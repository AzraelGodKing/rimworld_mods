using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Mental breaks can chase targets / wander through shafts across the colony.
    // Normal relays still block InMentalState; these paths opt in via CanRelayMentalBreak.
    public static class MentalBreakRelay
    {
        private static readonly List<Pawn> tmpPawns = new List<Pawn>();

        public static bool TryRelayForFoodStash(Pawn pawn, ref Job result)
        {
            if (result != null || !PawnRelay.CanRelayMentalBreak(pawn))
            {
                return false;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.foodRelayEnabled)
            {
                return false;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (!PawnRelay.HasFoodFor(pawn, link.map))
                {
                    continue;
                }
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Food, 3);
                if (job != null)
                {
                    result = job;
                    return true;
                }
            }
            return false;
        }

        public static bool TryRelayForHostiles(Pawn pawn, ref Job result)
        {
            if (result != null || !PawnRelay.CanRelayMentalBreak(pawn))
            {
                return false;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (!HasHostileTarget(pawn, link.map))
                {
                    continue;
                }
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Work, 2);
                if (job != null)
                {
                    result = job;
                    return true;
                }
            }
            return false;
        }

        public static bool TryRelayForMurderTarget(Pawn pawn, ref Job result)
        {
            if (result != null || !PawnRelay.CanRelayMentalBreak(pawn))
            {
                return false;
            }
            if (pawn.MentalState is not MentalState_MurderousRage rage || rage.target == null)
            {
                return false;
            }
            return TryRelayToThingMap(pawn, rage.target, ref result);
        }

        /// <summary>
        /// If the current mental state already has a Thing/Pawn target on another
        /// linked floor, commute there.
        /// </summary>
        public static bool TryRelayForMentalTarget(Pawn pawn, ref Job result)
        {
            if (result != null || !PawnRelay.CanRelayMentalBreak(pawn))
            {
                return false;
            }
            Thing target = GetMentalStateTarget(pawn);
            return TryRelayToThingMap(pawn, target, ref result);
        }

        /// <summary>
        /// Fallback when a break JobGiver finds nothing on this floor — go to
        /// another colony level so the break can play out anywhere downstairs/up.
        /// </summary>
        public static bool TryRelayAnywhere(Pawn pawn, ref Job result)
        {
            if (result != null || !PawnRelay.CanRelayMentalBreak(pawn))
            {
                return false;
            }
            if (pawn.MentalState == null)
            {
                return false;
            }

            Map best = null;
            int bestScore = 0;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                Map map = link.map;
                if (map == null || map.Disposed)
                {
                    continue;
                }
                int score = ScoreMapForMentalBreak(pawn, map);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = map;
                }
            }
            if (best == null || bestScore <= 0)
            {
                return false;
            }
            Job job = PawnRelay.TryRelayToMap(pawn, best, touchCooldown: true, RelayPurpose.ForcedOrder);
            if (job == null)
            {
                return false;
            }
            result = job;
            return true;
        }

        public static bool TryGiveMentalBreakJobOrRelay(Pawn pawn, ref Job result)
        {
            if (result != null)
            {
                return false;
            }
            return TryRelayForMentalTarget(pawn, ref result)
                || TryRelayAnywhere(pawn, ref result);
        }

        public static Thing GetMentalStateTarget(Pawn pawn)
        {
            MentalState state = pawn?.MentalState;
            if (state == null)
            {
                return null;
            }
            if (state is MentalState_Tantrum tantrum)
            {
                return tantrum.target;
            }
            if (state is MentalState_InsultingSpree insult)
            {
                return insult.target;
            }
            if (state is MentalState_MurderousRage rage)
            {
                return rage.target;
            }
            if (state is MentalState_SocialFighting fight)
            {
                return fight.otherPawn;
            }
            return null;
        }

        public static void AppendSmashablesFromLinkedMaps(
            Pawn pawn,
            List<Thing> outCandidates,
            System.Predicate<Thing> customValidator,
            int extraMinBuildingOrItemMarketValue)
        {
            if (pawn?.Map == null || outCandidates == null || !LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                Map map = link.map;
                if (map == null || map.Disposed)
                {
                    continue;
                }
                CollectSmashablesOnMap(
                    pawn,
                    map,
                    outCandidates,
                    customValidator,
                    extraMinBuildingOrItemMarketValue);
            }
        }

        public static void AppendInsultCandidatesFromLinkedMaps(
            Pawn bully,
            List<Pawn> outCandidates,
            bool allowPrisoners)
        {
            if (bully?.Map == null || outCandidates == null || !LevelGraph.AnyLinkFrom(bully.Map))
            {
                return;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(bully.Map))
            {
                Map map = link.map;
                if (map?.mapPawns == null)
                {
                    continue;
                }
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn other = pawns[i];
                    if (other == null || outCandidates.Contains(other))
                    {
                        continue;
                    }
                    // Distance check is same-map only in vanilla; skip it for
                    // cross-level (stair commute replaces InHorDistOf).
                    if (!other.RaceProps.Humanlike
                        || (other.Faction != bully.Faction
                            && !(allowPrisoners && other.HostFaction == bully.Faction))
                        || other == bully
                        || other.Dead
                        || other.Downed
                        || !other.Spawned
                        || !other.Awake()
                        || !SocialInteractionUtility.CanReceiveInteraction(other)
                        || other.HostileTo(bully))
                    {
                        continue;
                    }
                    outCandidates.Add(other);
                }
            }
        }

        public static Pawn FindMurderTargetOnLinkedMaps(Pawn pawn)
        {
            if (pawn?.Map == null || !LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return null;
            }
            tmpPawns.Clear();
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                Map map = link.map;
                if (map?.mapPawns == null)
                {
                    continue;
                }
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn other = pawns[i];
                    if (other == null
                        || other == pawn
                        || !other.RaceProps.Humanlike
                        || other.Dead
                        || other.Downed)
                    {
                        continue;
                    }
                    if (other.Faction != pawn.Faction
                        && !(other.IsPrisoner && other.HostFaction == pawn.Faction))
                    {
                        continue;
                    }
                    if (other.CurJob != null && other.CurJob.exitMapOnArrival)
                    {
                        continue;
                    }
                    tmpPawns.Add(other);
                }
            }
            if (tmpPawns.Count == 0)
            {
                return null;
            }
            Pawn pick = tmpPawns.RandomElement();
            tmpPawns.Clear();
            return pick;
        }

        private static bool TryRelayToThingMap(Pawn pawn, Thing target, ref Job result)
        {
            if (target == null)
            {
                return false;
            }
            Map dest = target.MapHeld;
            if (dest == null || dest == pawn.Map || dest.Disposed)
            {
                return false;
            }
            if (!ColonyBedUtility.MapsLinked(pawn.Map, dest))
            {
                return false;
            }
            Job job = PawnRelay.TryRelayToMap(
                pawn,
                dest,
                touchCooldown: true,
                RelayPurpose.ForcedOrder,
                preferArrivalNear: target.PositionHeld);
            if (job == null)
            {
                return false;
            }
            result = job;
            return true;
        }

        private static int ScoreMapForMentalBreak(Pawn pawn, Map map)
        {
            int score = 1;
            if (map.mapPawns != null)
            {
                score += map.mapPawns.FreeColonistsSpawnedCount * 3;
                score += map.mapPawns.AllPawnsSpawnedCount;
            }
            // Prefer floors with smashables / furniture for tantrum-style breaks.
            List<Thing> buildings = map.listerThings?.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            if (buildings != null)
            {
                score += System.Math.Min(buildings.Count, 40);
            }
            return score;
        }

        private static void CollectSmashablesOnMap(
            Pawn pawn,
            Map map,
            List<Thing> into,
            System.Predicate<Thing> customValidator,
            int extraMin)
        {
            void Consider(Thing t)
            {
                if (t == null || into.Contains(t))
                {
                    return;
                }
                if (TantrumMentalStateUtility.CanSmash(
                        pawn,
                        t,
                        skipReachabilityCheck: true,
                        customValidator,
                        extraMin))
                {
                    into.Add(t);
                }
            }

            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < buildings.Count; i++)
            {
                Consider(buildings[i]);
            }
            List<Thing> haulables = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
            for (int i = 0; i < haulables.Count; i++)
            {
                Consider(haulables[i]);
            }
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Consider(pawns[i]);
            }
        }

        private static bool HasHostileTarget(Pawn pawn, Map map)
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn other = pawns[i];
                if (other == null || other == pawn || other.Dead || other.Downed)
                {
                    continue;
                }
                if (other.HostileTo(pawn))
                {
                    return true;
                }
            }
            return false;
        }
    }

    // --- JobGiver patches: after local fail, chase target or wander to another floor ---

    [HarmonyPatch(typeof(JobGiver_Binge), "TryGiveJob")]
    public static class Patch_BingeAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (!MentalBreakRelay.TryRelayForFoodStash(pawn, ref __result))
            {
                MentalBreakRelay.TryGiveMentalBreakJobOrRelay(pawn, ref __result);
            }
        }
    }

    [HarmonyPatch(typeof(JobGiver_Berserk), "TryGiveJob")]
    public static class Patch_BerserkAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (!MentalBreakRelay.TryRelayForHostiles(pawn, ref __result))
            {
                MentalBreakRelay.TryGiveMentalBreakJobOrRelay(pawn, ref __result);
            }
        }
    }

    [HarmonyPatch(typeof(JobGiver_MurderousRage), "TryGiveJob")]
    public static class Patch_MurderousRageAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (!MentalBreakRelay.TryRelayForMurderTarget(pawn, ref __result))
            {
                MentalBreakRelay.TryGiveMentalBreakJobOrRelay(pawn, ref __result);
            }
        }
    }

    [HarmonyPatch(typeof(JobGiver_Tantrum), "TryGiveJob")]
    public static class Patch_TantrumAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            MentalBreakRelay.TryGiveMentalBreakJobOrRelay(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_InsultingSpree), "TryGiveJob")]
    public static class Patch_InsultingSpreeAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            MentalBreakRelay.TryGiveMentalBreakJobOrRelay(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_Manhunter), "TryGiveJob")]
    public static class Patch_ManhunterAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (!MentalBreakRelay.TryRelayForHostiles(pawn, ref __result))
            {
                MentalBreakRelay.TryGiveMentalBreakJobOrRelay(pawn, ref __result);
            }
        }
    }

    [HarmonyPatch]
    public static class Patch_FireStartingSpreeAcrossLevels
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(
                AccessTools.TypeByName("RimWorld.JobGiver_FireStartingSpree"),
                "TryGiveJob");
        }

        public static void Postfix(Pawn pawn, ref Job __result)
        {
            MentalBreakRelay.TryGiveMentalBreakJobOrRelay(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_SlaughterRandomAnimal), "TryGiveJob")]
    public static class Patch_SlaughterAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            MentalBreakRelay.TryGiveMentalBreakJobOrRelay(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_SocialFighting), "TryGiveJob")]
    public static class Patch_SocialFightingAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            MentalBreakRelay.TryGiveMentalBreakJobOrRelay(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_Wander), "TryGiveJob")]
    public static class Patch_WanderMentalAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (pawn?.InMentalState != true)
            {
                return;
            }
            // Local wander usually succeeds — sometimes take stairs so the break
            // can roam the whole column. Always escalate when empty-handed.
            if (__result != null && !Rand.Chance(0.12f))
            {
                return;
            }
            Job relay = null;
            if (MentalBreakRelay.TryRelayAnywhere(pawn, ref relay) && relay != null)
            {
                __result = relay;
            }
            else if (__result == null)
            {
                MentalBreakRelay.TryGiveMentalBreakJobOrRelay(pawn, ref __result);
            }
        }
    }

    // --- Target discovery: let breaks pick victims/smashables on linked floors ---

    [HarmonyPatch(typeof(TantrumMentalStateUtility), nameof(TantrumMentalStateUtility.GetSmashableThingsNear))]
    public static class Patch_TantrumSmashablesAcrossLevels
    {
        public static void Postfix(
            Pawn pawn,
            List<Thing> outCandidates,
            System.Predicate<Thing> customValidator,
            int extraMinBuildingOrItemMarketValue)
        {
            MentalBreakRelay.AppendSmashablesFromLinkedMaps(
                pawn,
                outCandidates,
                customValidator,
                extraMinBuildingOrItemMarketValue);
        }
    }

    [HarmonyPatch(typeof(InsultingSpreeMentalStateUtility), nameof(InsultingSpreeMentalStateUtility.GetInsultCandidatesFor))]
    public static class Patch_InsultCandidatesAcrossLevels
    {
        public static void Postfix(Pawn bully, List<Pawn> outCandidates, bool allowPrisoners)
        {
            MentalBreakRelay.AppendInsultCandidatesFromLinkedMaps(bully, outCandidates, allowPrisoners);
        }
    }

    [HarmonyPatch(typeof(MurderousRageMentalStateUtility), nameof(MurderousRageMentalStateUtility.FindPawnToKill))]
    public static class Patch_MurderTargetAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Pawn __result)
        {
            if (__result != null)
            {
                return;
            }
            __result = MentalBreakRelay.FindMurderTargetOnLinkedMaps(pawn);
        }
    }
}

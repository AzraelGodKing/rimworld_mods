using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Shared bed-seeking across linked levels for GetRest and ForceSleepNow
    // (exhaustion "pass out" bypasses GetRest entirely).
    public static class RestBedCommute
    {
        public static Job TryMakeJob(Pawn pawn, Job vanillaResult)
        {
            if (pawn == null || !pawn.Spawned
                || (StrataMod.Settings != null && !StrataMod.Settings.restRelayEnabled))
            {
                return null;
            }

            // Already commuting — caller should null out the floor-sleep job so
            // ForceSleepNow does not interrupt EnterPortal every think pass.
            if (PortalRelayChain.HasIntent(pawn) || IsMidRestCommute(pawn))
            {
                return null;
            }

            Building_Bed ownBed = pawn.ownership?.OwnedBed;
            bool ownBedElsewhere = ownBed != null && ownBed.Spawned && ownBed.Map != pawn.Map;
            bool homeless = ownBed == null || !ownBed.Spawned;

            if (ownBedElsewhere
                && ShouldCommute(pawn, vanillaResult)
                && PawnRelay.CanRelayBasics(pawn))
            {
                return PawnRelay.TryRelayToMap(
                    pawn,
                    ownBed.Map,
                    touchCooldown: false,
                    RelayPurpose.Rest,
                    ownBed);
            }

            if (ShouldCommute(pawn, vanillaResult)
                && !IsUsableBedRestJob(pawn, vanillaResult)
                && (vanillaResult == null
                    || vanillaResult.def == JobDefOf.LayDown
                    || vanillaResult.forceSleep)
                && StrataPawnUtility.CanUseLevelPortals(pawn)
                && !pawn.Drafted)
            {
                Building_Bed detourBed = FindBedNeedingDetour(pawn, ownBed);
                if (detourBed != null)
                {
                    Job detour = DraftedPortalPathing.TryMakeRestDetourJob(pawn, detourBed);
                    if (detour != null)
                    {
                        return detour;
                    }
                }
            }

            if (!StrataPawnUtility.CanUseLevelPortals(pawn)
                || pawn.Drafted
                || pawn.InMentalState
                || pawn.IsBurning()
                || !LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return null;
            }

            // Homeless skip cooldown; others still respect it.
            if (!homeless && PawnRelay.IsOnRelayCooldown(pawn))
            {
                return null;
            }

            if (IsUsableBedRestJob(pawn, vanillaResult))
            {
                return null;
            }

            if (vanillaResult != null
                && vanillaResult.def != JobDefOf.LayDown
                && !vanillaResult.forceSleep)
            {
                return null;
            }

            if (!ShouldCommute(pawn, vanillaResult))
            {
                return null;
            }

            Map bestMap = FindBestBedMap(pawn);
            if (bestMap == null)
            {
                return null;
            }

            return PawnRelay.TryRelayToMap(
                pawn,
                bestMap,
                touchCooldown: homeless,
                RelayPurpose.Rest);
        }

        // True when ForceSleepNow/GetRest should yield (return null job) so the
        // current stair commute is not overwritten by floor LayDown.
        public static bool ShouldYieldToCommute(Pawn pawn)
        {
            return pawn != null
                && (PortalRelayChain.HasIntent(pawn) || IsMidRestCommute(pawn));
        }

        private static bool IsMidRestCommute(Pawn pawn)
        {
            if (pawn?.CurJobDef == JobDefOf.EnterPortal)
            {
                return true;
            }

            // Walking toward a stairwell as part of EnterPortal pathing often
            // shows as Goto with the portal as target.
            if (pawn?.CurJobDef == JobDefOf.Goto
                && pawn.CurJob?.targetA.Thing is MapPortal)
            {
                return true;
            }

            return false;
        }

        private static Map FindBestBedMap(Pawn pawn)
        {
            Map best = null;
            int bestCount = 0;
            int bestDepth = int.MaxValue;

            // Snapshot — ReachableLevels reuses a buffer.
            var links = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(pawn.Map));
            for (int i = 0; i < links.Count; i++)
            {
                LevelGraph.LevelLink link = links[i];
                int count = CountClaimableBeds(pawn, link.map);
                if (count <= 0)
                {
                    continue;
                }

                if (count > bestCount
                    || (count == bestCount && link.depth < bestDepth))
                {
                    bestCount = count;
                    bestDepth = link.depth;
                    best = link.map;
                }
            }

            return best;
        }

        private static int CountClaimableBeds(Pawn pawn, Map map)
        {
            int count = 0;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is Building_Bed bed && IsNetworkClaimableBed(pawn, bed))
                {
                    count++;
                }
            }

            return count;
        }

        // Looser than sleep-now validation: free colonist/slave slots on player beds.
        private static bool IsNetworkClaimableBed(Pawn pawn, Building_Bed bed)
        {
            if (bed == null || !bed.Spawned || bed.Faction != Faction.OfPlayer
                || !bed.def.building.bed_humanlike || bed.Medical || bed.ForPrisoners
                || bed.IsForbidden(pawn) || bed.IsBurning())
            {
                return false;
            }

            if (!RestUtility.CanUseBedEver(pawn, bed.def))
            {
                return false;
            }

            if (bed.OwnersForReading.Contains(pawn))
            {
                return true;
            }

            CompAssignableToPawn comp = bed.CompAssignableToPawn;
            return comp != null && comp.HasFreeSlot;
        }

        private static bool ShouldCommute(Pawn pawn, Job vanillaResult)
        {
            if (vanillaResult != null
                && (vanillaResult.def == JobDefOf.LayDown || vanillaResult.forceSleep))
            {
                return true;
            }

            return PawnRelay.ShouldCommuteForRest(pawn, vanillaResult);
        }

        private static Building_Bed FindBedNeedingDetour(Pawn pawn, Building_Bed ownBed)
        {
            if (ownBed != null
                && ownBed.Spawned
                && ownBed.Map == pawn.Map
                && IsNetworkClaimableBed(pawn, ownBed)
                && !pawn.CanReach(ownBed, PathEndMode.OnCell, Danger.Deadly)
                && DraftedPortalPathing.HasDetour(pawn, ownBed.Position))
            {
                return ownBed;
            }

            Building_Bed best = null;
            float bestDist = float.MaxValue;
            foreach (Thing thing in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is not Building_Bed bed
                    || !IsNetworkClaimableBed(pawn, bed)
                    || pawn.CanReach(bed, PathEndMode.OnCell, Danger.Deadly)
                    || !DraftedPortalPathing.HasDetour(pawn, bed.Position))
                {
                    continue;
                }

                float dist = pawn.Position.DistanceToSquared(bed.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = bed;
                }
            }

            return best;
        }

        private static bool IsUsableBedRestJob(Pawn pawn, Job job)
        {
            return job?.def == JobDefOf.LayDown
                && job.targetA.Thing is Building_Bed bed
                && PawnRelay.IsUsableColonistBedJob(pawn, bed);
        }
    }
}

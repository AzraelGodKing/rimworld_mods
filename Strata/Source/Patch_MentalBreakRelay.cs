using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // A2: mental breaks can chase stash / victims through shafts.
    // Normal relays still block InMentalState; these JobGivers opt in.
    public static class MentalBreakRelay
    {
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
            Map dest = rage.target.MapHeld;
            if (dest == null || dest == pawn.Map || dest.Disposed)
            {
                return false;
            }
            Job job = PawnRelay.TryRelayToMap(pawn, dest, touchCooldown: true, RelayPurpose.Work);
            if (job == null)
            {
                return false;
            }
            result = job;
            return true;
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

    // Declared on abstract JobGiver_Binge — covers food and drug binges.
    [HarmonyPatch(typeof(JobGiver_Binge), "TryGiveJob")]
    public static class Patch_BingeAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            MentalBreakRelay.TryRelayForFoodStash(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_Berserk), "TryGiveJob")]
    public static class Patch_BerserkAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            MentalBreakRelay.TryRelayForHostiles(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_MurderousRage), "TryGiveJob")]
    public static class Patch_MurderousRageAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            MentalBreakRelay.TryRelayForMurderTarget(pawn, ref __result);
        }
    }
}

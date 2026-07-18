using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // A tired pawn on a floor without a usable bed walks to another level:
    // home to their assigned bed, or to any linked level with a free colonist
    // bed (vanilla auto-claims it once they arrive).
    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    public static class Patch_RestRelay
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.restRelayEnabled)
            {
                return;
            }

            Building_Bed ownBed = pawn.ownership?.OwnedBed;
            bool ownBedElsewhere = ownBed != null && ownBed.Spawned && ownBed.Map != pawn.Map;

            // Assigned bedroom on another level: always prefer commuting home over
            // floor sleep or a random free bed on this floor (apartment blocks).
            if (ownBedElsewhere
                && PawnRelay.ShouldCommuteForRest(pawn, __result)
                && PawnRelay.CanRelayBasics(pawn))
            {
                Job home = PawnRelay.TryRelayToMap(pawn, ownBed.Map, touchCooldown: false);
                if (home != null)
                {
                    __result = home;
                    return;
                }
            }

            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }

            // Already heading to a reachable bed on this floor.
            if (IsUsableBedRestJob(pawn, __result))
            {
                return;
            }

            // Some other rest job vanilla chose — keep it.
            if (__result != null && __result.def != JobDefOf.LayDown)
            {
                return;
            }

            // Usable bed exists here but vanilla picked the floor anyway; only
            // commute for free beds when this floor truly has nowhere to sleep.
            // (Owned-bed home is handled above and must not be blocked by this.)
            if (__result?.def == JobDefOf.LayDown
                && __result.targetA.Thing is not Building_Bed
                && PawnRelay.HasUsableBedOnMap(pawn, pawn.Map))
            {
                return;
            }

            if (!PawnRelay.ShouldCommuteForRest(pawn, __result))
            {
                return;
            }

            // Opportunistic free colonist beds on linked levels.
            var links = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(pawn.Map));
            LevelRoleUtility.SortLinksByRole(links, LevelRole.Barracks);
            foreach (LevelGraph.LevelLink link in links)
            {
                if (!PawnRelay.HasClaimableBedFor(pawn, link.map))
                {
                    continue;
                }

                int cap = PawnRelay.ClaimableBedCount(pawn, link.map);
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Rest, cap);
                if (job != null)
                {
                    __result = job;
                    return;
                }
            }
        }

        private static bool IsUsableBedRestJob(Pawn pawn, Job job)
        {
            return job?.def == JobDefOf.LayDown
                && job.targetA.Thing is Building_Bed bed
                && PawnRelay.IsUsableColonistBedJob(pawn, bed);
        }
    }
}

using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // A tired pawn who would otherwise sleep on the ground walks to another
    // level instead: to their own bed if they have one, or to any level with a
    // claimable free bed (vanilla auto-claims it once they arrive).
    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    public static class Patch_RestRelay
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }
            // Already found a real bed on this level (e.g. a medical bed)? Keep it.
            if (__result != null
                && (__result.def != JobDefOf.LayDown || __result.targetA.Thing is Building_Bed))
            {
                return;
            }
            Building_Bed ownBed = pawn.ownership?.OwnedBed;
            bool ownBedElsewhere = ownBed != null && ownBed.Spawned && ownBed.Map != pawn.Map;
            // Own bed on this level but vanilla still chose the floor: the bed
            // is unreachable or unusable right now. Not a level problem - stay out.
            if (ownBed != null && ownBed.Spawned && ownBed.Map == pawn.Map)
            {
                return;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                bool isTarget = ownBedElsewhere
                    ? link.map == ownBed.Map
                    : PawnRelay.HasClaimableBedFor(pawn, link.map);
                if (!isTarget)
                {
                    continue;
                }
                Job job = PawnRelay.MakeRelayJob(pawn, link.firstStep);
                if (job != null)
                {
                    __result = job;
                }
                return;
            }
        }
    }
}

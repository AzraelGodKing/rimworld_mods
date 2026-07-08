using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // A tired pawn whose own bed is on another level walks home to it instead
    // of sleeping on the floor by the stairs.
    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    public static class Patch_RestRelay
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }
            Building_Bed ownBed = pawn.ownership?.OwnedBed;
            if (ownBed == null || !ownBed.Spawned || ownBed.Map == pawn.Map)
            {
                return;
            }
            // Already found a real bed on this level (e.g. a medical bed)? Keep it.
            if (__result != null
                && (__result.def != JobDefOf.LayDown || __result.targetA.Thing is Building_Bed))
            {
                return;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (link.map != ownBed.Map)
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

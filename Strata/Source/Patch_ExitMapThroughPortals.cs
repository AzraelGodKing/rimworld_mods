using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // When vanilla exit-map AI finds no edge (underground pocket maps), send the
    // pawn through a Strata shaft toward the surface. Covers prison breaks, slave
    // escapes, guest departure, panic flee, and similar JobGiver_ExitMap* flows
    // without escorts and without opening work/food relays to those pawns.
    [HarmonyPatch(typeof(JobGiver_ExitMap), "TryGiveJob")]
    public static class Patch_ExitMapThroughPortals
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null || pawn == null || !pawn.Spawned)
            {
                return;
            }
            if (!EscapePortalUtility.NeedsPortalEscape(pawn.Map))
            {
                return;
            }
            Job job = EscapePortalUtility.MakeEscapeJob(pawn);
            if (job != null)
            {
                __result = job;
            }
        }
    }
}

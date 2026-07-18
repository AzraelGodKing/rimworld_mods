using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Let drafted "go here" accept cells that are only reachable via a linked
    // level (sealed bunker ↔ outside stair), then order the portal detour
    // instead of a doomed same-map Goto.
    [HarmonyPatch(typeof(FloatMenuOptionProvider_DraftedMove), "PawnCanGoto")]
    public static class Patch_DraftedMove_PawnCanGoto
    {
        public static void Postfix(Pawn pawn, IntVec3 gotoLoc, ref AcceptanceReport __result)
        {
            if (__result.Accepted || pawn == null)
            {
                return;
            }

            // Keep mechanitor range / other hard rejects; only upgrade no-path.
            if (__result.Reason != "CannotGoNoPath".Translate().Resolve())
            {
                return;
            }

            if (DraftedPortalPathing.HasDetour(pawn, gotoLoc))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(FloatMenuOptionProvider_DraftedMove), nameof(FloatMenuOptionProvider_DraftedMove.PawnGotoAction))]
    public static class Patch_DraftedMove_PawnGotoAction
    {
        public static bool Prefix(Pawn pawn, IntVec3 gotoLoc)
        {
            if (pawn == null || !pawn.Spawned || !pawn.Drafted)
            {
                return true;
            }

            // Vanilla Goto when the cell is already walkable on this map.
            if (pawn.CanReach(gotoLoc, PathEndMode.OnCell, Danger.Deadly))
            {
                return true;
            }

            if (DraftedPortalPathing.TryOrderDetour(pawn, gotoLoc))
            {
                FleckMaker.Static(gotoLoc, pawn.Map, FleckDefOf.FeedbackGoto);
                return false;
            }

            return true;
        }
    }
}

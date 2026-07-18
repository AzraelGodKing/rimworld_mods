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
        // clickCell = player's click; gotoLoc = BestOrderedGotoDestNear snap, which
        // for a sealed bunker is the wall (reachable) — that was making us fall
        // through to vanilla Goto. Prefer a stair detour to the click instead.
        public static bool Prefix(IntVec3 clickCell, Pawn pawn, IntVec3 gotoLoc)
        {
            if (pawn == null || !pawn.Spawned || !pawn.Drafted)
            {
                return true;
            }

            IntVec3 intended = DraftedPortalPathing.ResolveIntendedDest(pawn, clickCell, gotoLoc);
            if (!ReachabilityUtility.CanReach(pawn, intended, PathEndMode.OnCell, Danger.Deadly)
                && DraftedPortalPathing.TryOrderDetour(pawn, intended))
            {
                FleckMaker.Static(intended, pawn.Map, FleckDefOf.FeedbackGoto);
                return false;
            }

            return true;
        }
    }
}

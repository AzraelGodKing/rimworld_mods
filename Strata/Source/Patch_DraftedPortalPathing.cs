using System.Collections.Generic;
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

    // Right-click on a stair the pawn cannot walk to ("no path") uses the same
    // bunker detour as a drafted click on a cell, then enters that stair.
    [HarmonyPatch(typeof(MapPortal), nameof(MapPortal.GetFloatMenuOptions))]
    public static class Patch_MapPortal_NoPathDetour
    {
        public static void Postfix(MapPortal __instance, Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            if (__instance == null || selPawn == null || !selPawn.Spawned || selPawn.Map != __instance.Map)
            {
                return;
            }

            if (!StrataPawnUtility.CanUseLevelPortals(selPawn))
            {
                return;
            }

            if (selPawn.CanReach(__instance, PathEndMode.Touch, Danger.Deadly))
            {
                return;
            }

            if (!DraftedPortalPathing.HasDetour(selPawn, __instance.Position))
            {
                return;
            }

            string blocked = ("CannotEnterPortal".Translate(__instance.Label) + ": " + "NoPath".Translate()).Resolve();
            var options = new List<FloatMenuOption>();
            bool replaced = false;
            if (__result != null)
            {
                foreach (FloatMenuOption option in __result)
                {
                    if (!replaced && option != null && option.Label == blocked)
                    {
                        options.Add(EnterViaDetour(__instance, selPawn));
                        replaced = true;
                        continue;
                    }

                    options.Add(option);
                }
            }

            if (!replaced)
            {
                options.Add(EnterViaDetour(__instance, selPawn));
            }

            __result = options;
        }

        private static FloatMenuOption EnterViaDetour(MapPortal portal, Pawn pawn)
        {
            return new FloatMenuOption(portal.EnterString, delegate
            {
                Job job = DraftedPortalPathing.TryMakeEnterPortalDetourJob(pawn, portal);
                if (job != null)
                {
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            });
        }
    }
}

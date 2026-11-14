using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    public class CompProperties_StairPassage : CompProperties
    {
        public CompProperties_StairPassage()
        {
            compClass = typeof(CompStairPassage);
        }
    }

    // A one-way switch on one end of a stairwell or elevator. Closed, the
    // colony's own pawns don't step in from this side, on their own or when
    // ordered. The other end keeps its own switch, so the player can stop
    // people going down while those below still come up. Unlike the seal it
    // lets air, heat and raiders through: it is a rule for the colony, not a
    // hatch.
    public class CompStairPassage : ThingComp
    {
        private bool closedToColony;

        public bool ClosedToColony => closedToColony;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref closedToColony, "strataClosedToColony", defaultValue: false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != null && parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            yield return new Command_Toggle
            {
                defaultLabel = "Strata_StairPassage_Allow".Translate(),
                defaultDesc = "Strata_StairPassage_AllowDesc".Translate(),
                icon = TexCommand.ForbidOff,
                isActive = () => !closedToColony,
                toggleAction = () =>
                {
                    closedToColony = !closedToColony;
                    LevelGraph.InvalidateCache();
                },
            };
        }

        public override string CompInspectStringExtra()
        {
            return closedToColony ? "Strata_StairPassage_Closed".Translate().Resolve() : null;
        }
    }

    public static class StairPassage
    {
        // Whether this pawn must not step into the portal from its side.
        // Raiders, visitors and anyone in a mental state ignore it, the way
        // they ignore a forbidden door.
        public static bool ClosedTo(Thing portal, Pawn pawn)
        {
            if (portal == null || pawn == null)
            {
                return false;
            }
            CompStairPassage comp = portal.TryGetComp<CompStairPassage>();
            if (comp == null || !comp.ClosedToColony)
            {
                return false;
            }
            return pawn.Faction != null && pawn.Faction.IsPlayer && !pawn.InMentalState;
        }
    }

    // Last line: any EnterPortal job into a closed end fails, whoever queued it.
    [HarmonyPatch(typeof(JobDriver_EnterPortal), "MakeNewToils")]
    public static class Patch_EnterPortal_StairPassage
    {
        public static void Postfix(JobDriver_EnterPortal __instance)
        {
            JobDriver_EnterPortal driver = __instance;
            driver.FailOn(() => StairPassage.ClosedTo(driver.job?.targetA.Thing, driver.pawn));
        }
    }

    // The vanilla "Enter..." gizmo sends pawns in from this side.
    [HarmonyPatch(typeof(MapPortal), nameof(MapPortal.GetGizmos))]
    public static class Patch_MapPortalGizmos_StairPassage
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, MapPortal __instance)
        {
            CompStairPassage comp = __instance.TryGetComp<CompStairPassage>();
            string enter = comp != null && comp.ClosedToColony ? __instance.EnterString : null;
            foreach (Gizmo gizmo in gizmos)
            {
                if (enter != null
                    && gizmo is Command_Action action
                    && !action.Disabled
                    && action.defaultLabel != null
                    && action.defaultLabel.StartsWith(enter))
                {
                    action.Disable("Strata_StairPassage_Closed".Translate());
                }
                yield return gizmo;
            }
        }
    }

    [HarmonyPatch(typeof(FloatMenuOptionProvider_EnterMapPortal), "GetSingleOptionFor")]
    public static class Patch_EnterMapPortalOption_StairPassage
    {
        public static void Postfix(Thing clickedThing, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (__result == null || __result.Disabled || context == null)
            {
                return;
            }
            if (StairPassage.ClosedTo(clickedThing, context.FirstSelectedPawn))
            {
                __result = new FloatMenuOption(
                    ((clickedThing as MapPortal)?.EnterString ?? clickedThing.LabelCap.ToString())
                        + ": " + "Strata_StairPassage_Closed".Translate(),
                    null);
            }
        }
    }
}

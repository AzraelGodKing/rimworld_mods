using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Float-menu Rescue greys out when FindBedFor finds nothing on this map.
    // Offer a stair-commute rescue when a bed exists on a linked floor.
    [HarmonyPatch(typeof(FloatMenuOptionProvider_RescuePawn), "GetSingleOptionFor")]
    public static class Patch_RescuePawn_AcrossLevels
    {
        public static void Postfix(Pawn clickedPawn, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (clickedPawn == null || context.FirstSelectedPawn == null)
            {
                return;
            }

            Pawn rescuer = context.FirstSelectedPawn;
            bool needsCrossLevel = __result == null || __result.Disabled;
            if (!needsCrossLevel)
            {
                // Enabled local rescue already — leave it.
                return;
            }

            if (!HealthAIUtility.CanRescueNow(rescuer, clickedPawn, forced: true)
                && !HealthAIUtility.WantsToBeRescued(clickedPawn))
            {
                return;
            }

            if (!RescueBedAcrossLevels.HasAny(rescuer, clickedPawn))
            {
                return;
            }

            TaggedString label = "Rescue".Translate(clickedPawn.LabelCap, clickedPawn);
            Action action = delegate
            {
                if (!RescueBedAcrossLevels.TryFind(rescuer, clickedPawn, out Building_Bed dest, out MapPortal step)
                    && !RescueBedAcrossLevels.TryFind(
                        rescuer,
                        clickedPawn,
                        out dest,
                        out step,
                        ignoreReservations: true))
                {
                    Messages.Message(
                        "Strata_CannotRescueNoCrossLevelBed".Translate(),
                        clickedPawn,
                        MessageTypeDefOf.RejectInput,
                        historical: false);
                    return;
                }

                Job job = JobMaker.MakeJob(StrataDefOf.Strata_RescueToLevel, clickedPawn, step, dest);
                job.count = 1;
                rescuer.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            };

            __result = FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, action, MenuOptionPriority.RescueOrCapture, null, clickedPawn),
                rescuer,
                clickedPawn);
        }
    }
}

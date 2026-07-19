using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Capture entity float menu only lists Building_HoldingPlatform on the
    // selected pawn's map. When the crypt is underground, offer a stair commute.
    [HarmonyPatch(typeof(FloatMenuOptionProvider_CaptureEntity), nameof(FloatMenuOptionProvider_CaptureEntity.GetOptionsFor))]
    public static class Patch_CaptureEntity_AcrossLevels
    {
        public static IEnumerable<FloatMenuOption> Postfix(
            Thing clickedThing,
            FloatMenuContext context,
            IEnumerable<FloatMenuOption> __result)
        {
            bool anyEnabled = false;
            FloatMenuOption disabledNoPlatform = null;
            string noPlatformHint = "NoHoldingPlatformsAvailable".Translate().CapitalizeFirst();

            foreach (FloatMenuOption opt in __result)
            {
                if (opt.action != null)
                {
                    anyEnabled = true;
                    yield return opt;
                    continue;
                }

                if (opt.Label != null && opt.Label.Contains(noPlatformHint))
                {
                    disabledNoPlatform = opt;
                    continue;
                }

                yield return opt;
            }

            if (anyEnabled || clickedThing == null || context.FirstSelectedPawn == null)
            {
                if (!anyEnabled && disabledNoPlatform != null)
                {
                    yield return disabledNoPlatform;
                }

                yield break;
            }

            Pawn carrier = context.FirstSelectedPawn;
            CompHoldingPlatformTarget holdComp = clickedThing.TryGetComp<CompHoldingPlatformTarget>();
            if (holdComp == null || !holdComp.CanBeCaptured || !holdComp.StudiedAtHoldingPlatform)
            {
                if (disabledNoPlatform != null)
                {
                    yield return disabledNoPlatform;
                }

                yield break;
            }

            if (!HoldingPlatformAcrossLevels.HasAny(carrier, clickedThing))
            {
                if (disabledNoPlatform != null)
                {
                    yield return disabledNoPlatform;
                }

                yield break;
            }

            // Targeting UI is same-map only; auto-pick best linked platform.
            TaggedString label = "Capture".Translate(clickedThing.Label, clickedThing);
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    label,
                    () => StartBringJob(carrier, clickedThing),
                    MenuOptionPriority.RescueOrCapture,
                    null,
                    clickedThing),
                carrier,
                clickedThing);
        }

        private static void StartBringJob(Pawn carrier, Thing entity)
        {
            if (!HoldingPlatformAcrossLevels.TryFind(
                    carrier,
                    entity,
                    out Building_HoldingPlatform dest,
                    out MapPortal step)
                && !HoldingPlatformAcrossLevels.TryFind(
                    carrier,
                    entity,
                    out dest,
                    out step,
                    ignoreReservations: true))
            {
                Messages.Message(
                    "CannotGenericWorkCustom".Translate("CaptureLower".Translate(entity))
                        + ": " + "NoHoldingPlatformsAvailable".Translate().CapitalizeFirst(),
                    entity,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

            if (!ContainmentUtility.SafeContainerExistsFor(entity))
            {
                Messages.Message(
                    "MessageNoRoomWithMinimumContainmentStrength".Translate(entity.Label),
                    MessageTypeDefOf.ThreatSmall);
            }

            // Do not assign targetHolder here — CompTick clears cross-map holders.
            Job job = JobMaker.MakeJob(StrataDefOf.Strata_BringEntityToLevel, entity, step, dest);
            job.count = 1;
            carrier.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }

    [HarmonyPatch(typeof(StudyUtility), nameof(StudyUtility.HoldingPlatformAvailableOnCurrentMap))]
    public static class Patch_HoldingPlatformAvailable_LinkedMaps
    {
        public static void Postfix(ref bool __result)
        {
            if (__result || !ModsConfig.AnomalyActive)
            {
                return;
            }

            Map map = Find.CurrentMap;
            if (map != null && HoldingPlatformAcrossLevels.AnyAvailableOnLinkedMaps(map))
            {
                __result = true;
            }
        }
    }
}

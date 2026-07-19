using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Capture / arrest float menus call RestUtility.FindBedFor on the current
    // map, then ValidateTakeToBedOption greys Capture out. When the only prison
    // beds are on linked floors, offer a stair-commute capture instead.
    [HarmonyPatch(typeof(FloatMenuUtility), nameof(FloatMenuUtility.ValidateTakeToBedOption))]
    public static class Patch_ValidateTakeToBedOption_PrisonerAcrossLevels
    {
        public static void Postfix(Pawn pawn, Pawn target, FloatMenuOption option, GuestStatus? guestStatus)
        {
            if (option == null || !option.Disabled || guestStatus != GuestStatus.Prisoner)
            {
                return;
            }

            if (PrisonerBedAcrossLevels.HasAny(pawn, target))
            {
                option.Disabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(FloatMenuOptionProvider_CapturePawn), "GetSingleOptionFor")]
    public static class Patch_CapturePawn_AcrossLevels
    {
        public static void Postfix(Pawn clickedPawn, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (__result == null || clickedPawn == null || context.FirstSelectedPawn == null)
            {
                return;
            }

            Pawn warden = context.FirstSelectedPawn;
            Building_Bed local = RestUtility.FindBedFor(
                clickedPawn,
                warden,
                checkSocialProperness: false,
                ignoreOtherReservations: false,
                GuestStatus.Prisoner);
            if (local == null)
            {
                local = RestUtility.FindBedFor(
                    clickedPawn,
                    warden,
                    checkSocialProperness: false,
                    ignoreOtherReservations: true,
                    GuestStatus.Prisoner);
            }

            if (local != null)
            {
                return;
            }

            if (!PrisonerBedAcrossLevels.TryFind(warden, clickedPawn, out Building_Bed bed, out MapPortal portal)
                && !PrisonerBedAcrossLevels.TryFind(
                    warden,
                    clickedPawn,
                    out bed,
                    out portal,
                    ignoreReservations: true))
            {
                return;
            }

            TaggedString label = "Capture".Translate(clickedPawn.LabelCap, clickedPawn);
            if (!clickedPawn.guest.Recruitable)
            {
                label += " (" + "Unrecruitable".Translate() + ")";
            }

            if (clickedPawn.Faction != null && clickedPawn.Faction != Faction.OfPlayer
                && !clickedPawn.Faction.Hidden && !clickedPawn.Faction.HostileTo(Faction.OfPlayer)
                && !clickedPawn.IsPrisonerOfColony)
            {
                label += ": " + "AngersFaction".Translate().CapitalizeFirst();
            }

            Action action = delegate
            {
                if (!PrisonerBedAcrossLevels.TryFind(warden, clickedPawn, out Building_Bed dest, out MapPortal step)
                    && !PrisonerBedAcrossLevels.TryFind(
                        warden,
                        clickedPawn,
                        out dest,
                        out step,
                        ignoreReservations: true))
                {
                    Messages.Message(
                        "CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(),
                        clickedPawn,
                        MessageTypeDefOf.RejectInput,
                        historical: false);
                    return;
                }

                Job job = JobMaker.MakeJob(StrataDefOf.Strata_CaptureToLevel, clickedPawn, step, dest);
                job.count = 1;
                warden.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                if (clickedPawn.Faction != null && clickedPawn.Faction != Faction.OfPlayer
                    && !clickedPawn.Faction.Hidden && !clickedPawn.Faction.HostileTo(Faction.OfPlayer)
                    && !clickedPawn.IsPrisonerOfColony)
                {
                    Messages.Message(
                        "MessageCapturingWillAngerFaction".Translate(clickedPawn.Named("PAWN")).AdjustedFor(clickedPawn),
                        clickedPawn,
                        MessageTypeDefOf.CautionInput,
                        historical: false);
                }
            };

            __result = FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, action, MenuOptionPriority.RescueOrCapture, null, clickedPawn),
                warden,
                clickedPawn);
        }
    }

    [HarmonyPatch(typeof(FloatMenuOptionProvider_Arrest), "GetSingleOptionFor")]
    public static class Patch_Arrest_AcrossLevels
    {
        public static void Postfix(Pawn clickedPawn, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (__result == null || __result.action == null || clickedPawn == null
                || context.FirstSelectedPawn == null)
            {
                return;
            }

            Pawn warden = context.FirstSelectedPawn;
            Building_Bed local = RestUtility.FindBedFor(
                clickedPawn,
                warden,
                checkSocialProperness: false,
                ignoreOtherReservations: false,
                GuestStatus.Prisoner);
            if (local == null)
            {
                local = RestUtility.FindBedFor(
                    clickedPawn,
                    warden,
                    checkSocialProperness: false,
                    ignoreOtherReservations: true,
                    GuestStatus.Prisoner);
            }

            if (local != null)
            {
                return;
            }

            if (!PrisonerBedAcrossLevels.HasAny(warden, clickedPawn))
            {
                return;
            }

            Action original = __result.action;
            __result.action = delegate
            {
                Building_Bed stillLocal = RestUtility.FindBedFor(
                    clickedPawn,
                    warden,
                    checkSocialProperness: false,
                    ignoreOtherReservations: false,
                    GuestStatus.Prisoner)
                    ?? RestUtility.FindBedFor(
                        clickedPawn,
                        warden,
                        checkSocialProperness: false,
                        ignoreOtherReservations: true,
                        GuestStatus.Prisoner);
                if (stillLocal != null)
                {
                    original();
                    return;
                }

                if (!PrisonerBedAcrossLevels.TryFind(warden, clickedPawn, out Building_Bed dest, out MapPortal step)
                    && !PrisonerBedAcrossLevels.TryFind(
                        warden,
                        clickedPawn,
                        out dest,
                        out step,
                        ignoreReservations: true))
                {
                    Messages.Message(
                        "CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(),
                        clickedPawn,
                        MessageTypeDefOf.RejectInput,
                        historical: false);
                    return;
                }

                Job job = JobMaker.MakeJob(StrataDefOf.Strata_CaptureToLevel, clickedPawn, step, dest);
                job.count = 1;
                warden.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            };
        }
    }
}

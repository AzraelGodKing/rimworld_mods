using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace DateNight
{
    [StaticConstructorOnStartup]
    public static class DateNightDebug
    {
        private const string Cat = "Date Night";

        static DateNightDebug() { }

        [DebugAction(Cat, "Make selected pawns lovers (need 2)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void MakeSelectedLovers()
        {
            List<Pawn> pair = SelectedHumanlikes();
            if (pair.Count != 2)
            {
                Messages.Message("[Date Night] Select exactly two living humanlikes.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (TryMakeLovers(pair[0], pair[1], out string detail))
            {
                Messages.Message($"[Date Night] {pair[0].LabelShort} and {pair[1].LabelShort} are now lovers. {detail}",
                    MessageTypeDefOf.NeutralEvent, historical: false);
            }
            else
            {
                Messages.Message($"[Date Night] {detail}",
                    MessageTypeDefOf.RejectInput, historical: false);
            }
        }

        [DebugAction(Cat, "Make clicked pawn lover of selected",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMapForPawns)]
        private static void MakeClickedLoverOfSelected(Pawn clicked)
        {
            if (clicked == null || !clicked.RaceProps.Humanlike || clicked.Dead)
            {
                return;
            }

            Pawn selected = Find.Selector?.SingleSelectedThing as Pawn;
            if (selected == null || !selected.RaceProps.Humanlike || selected.Dead || selected == clicked)
            {
                Messages.Message("[Date Night] Select one humanlike, then click their partner.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (TryMakeLovers(selected, clicked, out string detail))
            {
                Messages.Message($"[Date Night] {selected.LabelShort} and {clicked.LabelShort} are now lovers. {detail}",
                    MessageTypeDefOf.NeutralEvent, historical: false);
            }
            else
            {
                Messages.Message($"[Date Night] {detail}",
                    MessageTypeDefOf.RejectInput, historical: false);
            }
        }

        [DebugAction(Cat, "Force lovin now (selected in shared bed)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceLovinNow()
        {
            int n = 0;
            foreach (Pawn p in SelectedHumanlikes())
            {
                Building_Bed bed = p.CurrentBed();
                Pawn partner = bed != null
                    ? LovePartnerRelationUtility.GetPartnerInMyBed(p)
                    : null;
                string why = bed == null ? "not in bed"
                    : bed.SleepingSlotsCount <= 1 ? "single bed"
                    : partner == null ? "no love partner in this bed"
                    : p.CurJobDef == JobDefOf.Lovin ? "already lovin"
                    : null;

                if (why == null && DateNightUtility.TryStartLovinNow(p))
                {
                    n++;
                }
                else if (why != null)
                {
                    Messages.Message($"[Date Night] {p.LabelShort}: {why}",
                        MessageTypeDefOf.RejectInput, historical: false);
                }
            }
            if (n > 0)
            {
                Messages.Message($"[Date Night] Started lovin on {n} pawn(s).",
                    MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        [DebugAction(Cat, "Force private time (selected)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForcePrivateTimeNow()
        {
            int n = 0;
            foreach (Pawn p in SelectedHumanlikes())
            {
                string why = !p.ageTracker.Adult || !p.DevelopmentalStage.Adult() ? "not an adult"
                    : p.CurJobDef == DateNightDefOf.DateNight_SelfLovin ? "already private time"
                    : p.CurJobDef == JobDefOf.Lovin ? "already lovin"
                    : LovePartnerRelationUtility.GetPartnerInMyBed(p) != null ? "love partner is in this bed — use Force lovin"
                    : null;

                if (why == null && DateNightUtility.TryStartSelfLovinNow(p, force: true))
                {
                    n++;
                }
                else if (why != null)
                {
                    Messages.Message($"[Date Night] {p.LabelShort}: {why}",
                        MessageTypeDefOf.RejectInput, historical: false);
                }
                else
                {
                    Messages.Message($"[Date Night] {p.LabelShort}: no usable bed",
                        MessageTypeDefOf.RejectInput, historical: false);
                }
            }
            if (n > 0)
            {
                Messages.Message($"[Date Night] Started private time on {n} pawn(s).",
                    MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        [DebugAction(Cat, "Force date now (selected)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceDateNow()
        {
            int n = 0;
            foreach (Pawn p in SelectedHumanlikes())
            {
                if (DateNightDateUtility.TryStartDateNow(p, force: true))
                {
                    n++;
                }
                else
                {
                    Messages.Message($"[Date Night] {p.LabelShort}: could not start a date (need a love partner on the map).",
                        MessageTypeDefOf.RejectInput, historical: false);
                }
            }
            if (n > 0)
            {
                Messages.Message($"[Date Night] Started a date on {n} pawn(s).",
                    MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        [DebugAction(Cat, "Force date with activity (selected)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceDateWithActivity()
        {
            List<Pawn> pawns = SelectedHumanlikes();
            if (pawns.Count == 0)
            {
                Messages.Message("[Date Night] Select at least one humanlike with a love partner.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (DateActivity activity in new[]
                     {
                         DateActivity.Hangout, DateActivity.Dinner, DateActivity.Picnic,
                         DateActivity.Walk, DateActivity.Stargaze, DateActivity.Dance,
                         DateActivity.Gift, DateActivity.Recreation,
                     })
            {
                DateActivity chosen = activity;
                options.Add(new FloatMenuOption(chosen.ToString(), () =>
                {
                    // Window instead of one-shot: the partner resolves their copy of
                    // the date a few ticks later and must land on the same activity.
                    DateNightActivities.ForcedActivity = chosen;
                    DateNightActivities.ForcedActivityExpireTick = Find.TickManager.TicksGame + 10000;

                    int n = 0;
                    foreach (Pawn p in pawns)
                    {
                        if (DateNightDateUtility.TryStartDateNow(p, force: true))
                        {
                            n++;
                        }
                    }
                    Messages.Message(
                        $"[Date Night] Forced {chosen} date on {n} pawn(s); override active ~4 in-game hours.",
                        n > 0 ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                        historical: false);
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        [DebugAction(Cat, "Clear forced date activity",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearForcedActivity()
        {
            DateNightActivities.ForcedActivity = DateActivity.Unresolved;
            DateNightActivities.ForcedActivityExpireTick = -1;
            Messages.Message("[Date Night] Forced date activity cleared.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Log rendezvous bed for selected",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogRendezvous()
        {
            foreach (Pawn p in SelectedHumanlikes())
            {
                Building_Bed bed = DateNightUtility.GetRendezvousBed(p);
                Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(p, allowDead: false);
                Messages.Message(
                    $"[Date Night] {p.LabelShort}: partner={partner?.LabelShort ?? "none"} rendezvous={bed?.LabelCap ?? "none"} (slots={bed?.SleepingSlotsCount ?? 0}) current={p.CurrentBed()?.LabelCap ?? "none"} job={p.CurJobDef?.defName ?? "none"}",
                    MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        [DebugAction(Cat, "Paint Lovin on selected (all hours)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void PaintLovinAllDay()
        {
            if (DateNightDefOf.DateNight_Lovin == null)
            {
                Messages.Message("[Date Night] Lovin TimeAssignmentDef missing.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            int n = 0;
            foreach (Pawn p in SelectedHumanlikes())
            {
                if (p.timetable == null)
                {
                    continue;
                }
                for (int h = 0; h < 24; h++)
                {
                    p.timetable.SetAssignment(h, DateNightDefOf.DateNight_Lovin);
                }
                n++;
            }

            Messages.Message(n > 0
                    ? $"[Date Night] Painted Lovin all day on {n} pawn(s)."
                    : "[Date Night] Select colonists with a timetable.",
                n > 0 ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                historical: false);
        }

        [DebugAction(Cat, "Paint Date on selected (all hours)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void PaintDateAllDay()
        {
            if (DateNightDefOf.DateNight_Date == null)
            {
                Messages.Message("[Date Night] Date TimeAssignmentDef missing.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            int n = 0;
            foreach (Pawn p in SelectedHumanlikes())
            {
                if (p.timetable == null)
                {
                    continue;
                }
                for (int h = 0; h < 24; h++)
                {
                    p.timetable.SetAssignment(h, DateNightDefOf.DateNight_Date);
                }
                n++;
            }

            Messages.Message(n > 0
                    ? $"[Date Night] Painted Date all day on {n} pawn(s)."
                    : "[Date Night] Select colonists with a timetable.",
                n > 0 ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                historical: false);
        }

        [DebugAction(Cat, "Force anniversary letter (selected pair)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceAnniversaryLetter()
        {
            List<Pawn> pair = SelectedHumanlikes();
            Pawn a;
            Pawn b;
            if (pair.Count == 2)
            {
                a = pair[0];
                b = pair[1];
            }
            else if (pair.Count == 1)
            {
                a = pair[0];
                b = LovePartnerRelationUtility.ExistingMostLikedLovePartner(a, allowDead: false);
            }
            else
            {
                a = null;
                b = null;
            }
            if (a == null || b == null)
            {
                Messages.Message("[Date Night] Select a couple (or one pawn with a love partner).",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            DateNightAnniversaries.DebugForceLetter(a, b);
            Messages.Message($"[Date Night] Anniversary letter for {a.LabelShort} and {b.LabelShort}.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Force missed-anniversary mood (selected pair)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceMissedAnniversary()
        {
            List<Pawn> pair = SelectedHumanlikes();
            Pawn a;
            Pawn b;
            if (pair.Count == 2)
            {
                a = pair[0];
                b = pair[1];
            }
            else if (pair.Count == 1)
            {
                a = pair[0];
                b = LovePartnerRelationUtility.ExistingMostLikedLovePartner(a, allowDead: false);
            }
            else
            {
                a = null;
                b = null;
            }
            if (a == null || b == null)
            {
                Messages.Message("[Date Night] Select a couple (or one pawn with a love partner).",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            DateNightAnniversaries.DebugForceMissed(a, b);
            Messages.Message($"[Date Night] Missed-anniversary mood for {a.LabelShort} and {b.LabelShort}.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Set bond age to 1 year (selected pair)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetBondAgeOneYear()
        {
            List<Pawn> pair = SelectedHumanlikes();
            Pawn a;
            Pawn b;
            if (pair.Count == 2)
            {
                a = pair[0];
                b = pair[1];
            }
            else if (pair.Count == 1)
            {
                a = pair[0];
                b = LovePartnerRelationUtility.ExistingMostLikedLovePartner(a, allowDead: false);
            }
            else
            {
                a = null;
                b = null;
            }
            if (a == null || b == null)
            {
                Messages.Message("[Date Night] Select a couple (or one pawn with a love partner).",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            DateNightAnniversaries.DebugSetBondAgeYears(a, b, 1);
            Messages.Message($"[Date Night] {a.LabelShort} and {b.LabelShort} bond set to 1 year ago (next matching day-of-year fires the letter).",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Log favourite venue for selected",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogFavoriteVenue()
        {
            foreach (Pawn p in SelectedHumanlikes())
            {
                Messages.Message($"[Date Night] {p.LabelShort} venue={DateNightVenues.DebugDescribe(p)}",
                    MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        private static List<Pawn> SelectedHumanlikes()
        {
            List<Pawn> list = new List<Pawn>();
            if (Find.Selector?.SelectedObjects == null)
            {
                return list;
            }
            foreach (object obj in Find.Selector.SelectedObjects)
            {
                if (obj is Pawn p && p.RaceProps.Humanlike && !p.Dead && !list.Contains(p))
                {
                    list.Add(p);
                }
            }
            return list;
        }

        private static bool TryMakeLovers(Pawn a, Pawn b, out string detail)
        {
            detail = null;
            if (a == null || b == null || a == b)
            {
                detail = "Need two different pawns.";
                return false;
            }
            if (!a.RaceProps.Humanlike || !b.RaceProps.Humanlike)
            {
                detail = "Both must be humanlike.";
                return false;
            }
            if (a.relations == null || b.relations == null)
            {
                detail = "Missing relations tracker.";
                return false;
            }

            if (LovePartnerRelationUtility.LovePartnerRelationExists(a, b))
            {
                detail = "Already love partners.";
                return true;
            }

            ClearLoveRelations(a, keep: b);
            ClearLoveRelations(b, keep: a);
            a.relations.AddDirectRelation(PawnRelationDefOf.Lover, b);
            detail = "Assign a shared double bed, then paint Lovin on both.";
            return true;
        }

        private static void ClearLoveRelations(Pawn pawn, Pawn keep)
        {
            List<DirectPawnRelation> relations = pawn.relations.DirectRelations;
            for (int i = relations.Count - 1; i >= 0; i--)
            {
                DirectPawnRelation rel = relations[i];
                if (rel.otherPawn == keep)
                {
                    continue;
                }
                if (rel.def == PawnRelationDefOf.Lover
                    || rel.def == PawnRelationDefOf.Fiance
                    || rel.def == PawnRelationDefOf.Spouse)
                {
                    pawn.relations.RemoveDirectRelation(rel);
                }
            }
        }
    }
}

using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public class Dialog_NemesisResolution : Window
    {
        private readonly NemesisData _data;
        private readonly Pawn _nemesis;

        public override Vector2 InitialSize => new Vector2(520f, 440f);

        public Dialog_NemesisResolution(NemesisData data, Pawn nemesis)
        {
            _data = data;
            _nemesis = nemesis;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("Nemesis_Dialog_Title".Translate(_data.nemesisName));
            Text.Font = GameFont.Small;
            listing.Gap(8f);

            string body = _data.trigger switch
            {
                NemesisTrigger.KilledAlly => "Nemesis_Dialog_Body_KilledAlly".Translate(),
                NemesisTrigger.PrisonerEscaped => "Nemesis_Dialog_Body_Prisoner".Translate(),
                NemesisTrigger.SlaveEscaped => "Nemesis_Dialog_Body_Slave".Translate(),
                NemesisTrigger.Fixation => "Nemesis_Dialog_Body_Fixation".Translate(),
                NemesisTrigger.WoundedAndEscaped => "Nemesis_Dialog_Body_Wounded".Translate(),
                _ => "Nemesis_Dialog_Body_Default".Translate(_data.nemesisName),
            };
            listing.Label(body);
            listing.Gap(18f);

            if (listing.ButtonText("Nemesis_Dialog_Execute".Translate()))
            {
                ApplyOutcome(NemesisOutcome.Execute);
                Close();
            }
            listing.Gap(6f);
            if (listing.ButtonText("Nemesis_Dialog_Release".Translate()))
            {
                ApplyOutcome(NemesisOutcome.Release);
                Close();
            }
            listing.Gap(6f);
            if (listing.ButtonText("Nemesis_Dialog_Keep".Translate()))
            {
                ApplyOutcome(NemesisOutcome.KeepPrisoner);
                Close();
            }
            listing.Gap(6f);
            int truceDays = NemesisMod.Settings?.truceDurationDays ?? 30;
            if (listing.ButtonText("Nemesis_Dialog_Truce".Translate(truceDays)))
            {
                ApplyOutcome(NemesisOutcome.Truce);
                Close();
            }

            listing.End();
        }

        private void ApplyOutcome(NemesisOutcome outcome)
        {
            Faction faction = (_data.faction != null && !_data.faction.IsPlayer)
                ? _data.faction
                : NemesisActions.FindFaction(_data);

            switch (outcome)
            {
                case NemesisOutcome.Execute:
                    if (_nemesis != null && !_nemesis.Dead)
                        _nemesis.Kill(null);

                    faction?.TryAffectGoodwillWith(Faction.OfPlayer, -30, canSendMessage: true, canSendHostilityLetter: true);

                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_ExecutedTitle".Translate(_data.nemesisName),
                        "Nemesis_Letter_ExecutedBody".Translate(_data.nemesisName, faction != null ? _data.factionName : ""),
                        LetterDefOf.NeutralEvent);
                    break;

                case NemesisOutcome.Release:
                    SendNemesisAway(PawnDiscardDecideMode.Decide);

                    faction?.TryAffectGoodwillWith(Faction.OfPlayer, 20, canSendMessage: true, canSendHostilityLetter: false);

                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_ReleasedTitle".Translate(_data.nemesisName),
                        "Nemesis_Letter_ReleasedBody".Translate(_data.nemesisName, faction != null ? _data.factionName : ""),
                        LetterDefOf.PositiveEvent);
                    break;

                case NemesisOutcome.KeepPrisoner:
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_KeptTitle".Translate(_data.nemesisName),
                        "Nemesis_Letter_KeptBody".Translate(_data.nemesisName),
                        LetterDefOf.NeutralEvent,
                        _nemesis);
                    break;

                case NemesisOutcome.Truce:
                    SendNemesisAway(PawnDiscardDecideMode.KeepForever);

                    int days = NemesisMod.Settings?.truceDurationDays ?? 30;
                    _data.truceUntilTick = Find.TickManager.TicksGame + days * 60000;

                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_TruceTitle".Translate(_data.nemesisName),
                        "Nemesis_Letter_TruceBody".Translate(_data.nemesisName, days),
                        LetterDefOf.NeutralEvent);
                    break;
            }

            NemesisRegistry.Clear();
        }

        private void SendNemesisAway(PawnDiscardDecideMode discardMode)
        {
            if (_nemesis == null || _nemesis.Dead) return;

            _nemesis.guest?.SetGuestStatus(null);

            if (_nemesis.Spawned)
                _nemesis.DeSpawn(DestroyMode.WillReplace);
            if (!_nemesis.IsWorldPawn())
                Find.WorldPawns.PassToWorld(_nemesis, discardMode);
        }
    }
}

using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public class Dialog_NemesisResolution : Window
    {
        private readonly NemesisData _data;

        public int HuntId => _data.huntId;

        public override Vector2 InitialSize => new Vector2(520f, 440f);

        public Dialog_NemesisResolution(NemesisData data, Pawn nemesis)
        {
            _data = data;
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
            listing.Label("Nemesis_Dialog_Title".Translate(_data.ColoredName));
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
                Choose(NemesisOutcome.Execute);
            listing.Gap(6f);
            if (listing.ButtonText("Nemesis_Dialog_Release".Translate()))
                Choose(NemesisOutcome.Release);
            listing.Gap(6f);
            if (listing.ButtonText("Nemesis_Dialog_Keep".Translate()))
                Choose(NemesisOutcome.KeepPrisoner);
            listing.Gap(6f);
            int truceDays = NemesisMod.Settings?.truceDurationDays ?? 30;
            if (listing.ButtonText("Nemesis_Dialog_Truce".Translate(truceDays)))
                Choose(NemesisOutcome.Truce);

            listing.End();
        }

        private void Choose(NemesisOutcome outcome)
        {
            NemesisPlayerCommand.Resolve(_data.huntId, outcome);
            Close();
        }

        /// <summary>Applies the player's choice; reached only through <see cref="NemesisPlayerCommand.Resolve"/>.</summary>
        internal static void ApplyOutcome(NemesisData data, NemesisOutcome outcome)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || data == null || !data.pendingResolution) return;
            Pawn nemesis = comp.FindNemesisPawnFor(data);
            if (nemesis == null || nemesis.Destroyed) return;

            Faction faction = (data.faction != null && !data.faction.IsPlayer)
                ? data.faction
                : NemesisActions.FindFaction(data);

            switch (outcome)
            {
                case NemesisOutcome.Execute:
                    if (!nemesis.Dead)
                        nemesis.Kill(null);

                    faction?.TryAffectGoodwillWith(Faction.OfPlayer, -30, canSendMessage: true, canSendHostilityLetter: true);

                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_ExecutedTitle".Translate(data.ColoredName),
                        "Nemesis_Letter_ExecutedBody".Translate(data.nemesisName, faction != null ? data.factionName : ""),
                        LetterDefOf.NeutralEvent);
                    break;

                case NemesisOutcome.Release:
                    SendNemesisAway(nemesis, PawnDiscardDecideMode.Decide);

                    faction?.TryAffectGoodwillWith(Faction.OfPlayer, 20, canSendMessage: true, canSendHostilityLetter: false);

                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_ReleasedTitle".Translate(data.ColoredName),
                        "Nemesis_Letter_ReleasedBody".Translate(data.nemesisName, faction != null ? data.factionName : ""),
                        LetterDefOf.PositiveEvent);
                    break;

                case NemesisOutcome.KeepPrisoner:
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_KeptTitle".Translate(data.ColoredName),
                        "Nemesis_Letter_KeptBody".Translate(data.nemesisName),
                        LetterDefOf.NeutralEvent,
                        nemesis);
                    break;

                case NemesisOutcome.Truce:
                    SendNemesisAway(nemesis, PawnDiscardDecideMode.KeepForever);

                    int days = NemesisMod.Settings?.truceDurationDays ?? 30;
                    data.truceUntilTick = Find.TickManager.TicksGame + days * 60000;

                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_TruceTitle".Translate(data.ColoredName),
                        "Nemesis_Letter_TruceBody".Translate(data.nemesisName, days),
                        LetterDefOf.NeutralEvent);
                    break;
            }

            string endKey = outcome switch
            {
                NemesisOutcome.Execute => "Nemesis_End_Executed",
                NemesisOutcome.Release => "Nemesis_End_Released",
                NemesisOutcome.KeepPrisoner => "Nemesis_End_Kept",
                NemesisOutcome.Truce => "Nemesis_End_Truce",
                _ => "Nemesis_End_Captured",
            };
            if (outcome != NemesisOutcome.Truce)
                comp.RecordEpitaph(data, endKey);

            data.pendingResolution = false;
            data.ClearCaches();
        }

        private static void SendNemesisAway(Pawn nemesis, PawnDiscardDecideMode discardMode)
        {
            if (nemesis == null || nemesis.Dead) return;

            nemesis.guest?.SetGuestStatus(null);

            if (nemesis.Spawned)
                nemesis.DeSpawn(DestroyMode.Vanish);
            if (!nemesis.IsWorldPawn())
                Find.WorldPawns.PassToWorld(nemesis, discardMode);
        }
    }
}

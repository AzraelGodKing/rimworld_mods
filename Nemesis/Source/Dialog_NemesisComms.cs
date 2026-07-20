using RimWorld;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>Player reply options after a comms taunt (or from the console float menu).</summary>
    public class Dialog_NemesisComms : Window
    {
        private readonly NemesisData _data;

        public override Vector2 InitialSize => new Vector2(480f, 320f);

        public Dialog_NemesisComms(NemesisData data)
        {
            _data = data;
            forcePause = true;
            absorbInputAroundWindow = true;
            doCloseX = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            Text.Font = GameFont.Medium;
            listing.Label("Nemesis_Comms_Title".Translate(_data.nemesisName));
            Text.Font = GameFont.Small;
            listing.Gap(6f);
            listing.Label("Nemesis_Comms_Body".Translate(_data.nemesisName, NemesisTaunts.TargetPhrase(_data)));
            listing.Gap(14f);

            if (listing.ButtonText("Nemesis_Comms_TauntBack".Translate()))
            {
                TauntBack();
                Close();
            }
            listing.Gap(4f);
            if (listing.ButtonText("Nemesis_Comms_OfferTruce".Translate()))
            {
                OfferTruce();
                Close();
            }
            listing.Gap(4f);
            if (listing.ButtonText("Nemesis_Comms_DemandSurrender".Translate()))
            {
                DemandSurrender();
                Close();
            }
            listing.Gap(8f);
            if (listing.ButtonText("Nemesis_Comms_Ignore".Translate()))
                Close();

            listing.End();
        }

        private void TauntBack()
        {
            _data.NotifyPlayerEngagedNemesis();
            _data.aggressionLevel = Mathf.Min(_data.aggressionLevel + 0.25f, 10f);
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_CommsReplyTitle".Translate(_data.nemesisName),
                "Nemesis_Letter_CommsTauntBackBody".Translate(_data.nemesisName, NemesisTaunts.TargetPhrase(_data)),
                LetterDefOf.ThreatSmall);
        }

        private void OfferTruce()
        {
            _data.NotifyPlayerEngagedNemesis();
            int days = Mathf.Clamp((NemesisMod.Settings?.truceDurationDays ?? 30) / 2, 5, 15);
            _data.active = false;
            _data.truceUntilTick = Find.TickManager.TicksGame + days * 60000;
            _data.nextTruceEventTick = Find.TickManager.TicksGame + 120000;
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_TruceTitle".Translate(_data.nemesisName),
                "Nemesis_Letter_TruceBody".Translate(_data.nemesisName, days),
                LetterDefOf.PositiveEvent);
        }

        private void DemandSurrender()
        {
            _data.NotifyPlayerEngagedNemesis();
            // Flavor — they refuse; slight aggression bump, next action sooner.
            _data.aggressionLevel = Mathf.Min(_data.aggressionLevel + 0.15f, 10f);
            _data.nextActionTick = Mathf.Min(_data.nextActionTick, Find.TickManager.TicksGame + 45000);
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_CommsReplyTitle".Translate(_data.nemesisName),
                "Nemesis_Letter_CommsSurrenderBody".Translate(_data.nemesisName, NemesisTaunts.TargetPhrase(_data)),
                LetterDefOf.NeutralEvent);
        }
    }
}

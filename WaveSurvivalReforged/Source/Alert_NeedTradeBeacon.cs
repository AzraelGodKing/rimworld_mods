using RimWorld;
using Verse;

namespace WaveSurvivalReforged
{
    public class Alert_NeedTradeBeacon : Alert
    {
        public Alert_NeedTradeBeacon()
        {
            defaultPriority = AlertPriority.High;
        }

        // Translated on use, not in the constructor, so the text follows a language change.
        public override string GetLabel() => "WSR_NeedBeaconLabel".Translate();

        public override TaggedString GetExplanation()
        {
            int days = WSR_DefOf.WSR_Config.noBeaconFallbackDays;
            TaggedString text = "WSR_NeedBeaconExplanation".Translate();
            if (days > 0)
                text += "\n\n" + "WSR_NeedBeaconFallback".Translate(days);
            return text;
        }

        public override AlertReport GetReport()
        {
            if (!WaveSurvivalComponent.ModeActive)
                return false;
            WaveSurvivalComponent waves = WaveSurvivalComponent.Instance;
            return waves != null && !waves.HasBeacon;
        }
    }
}

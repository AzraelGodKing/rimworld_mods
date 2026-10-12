using RimWorld;
using Verse;

namespace WaveSurvivalReforged
{
    public class Alert_NeedTradeBeacon : Alert
    {
        public Alert_NeedTradeBeacon()
        {
            defaultPriority = AlertPriority.High;
            defaultLabel = "WSR_NeedBeaconLabel".Translate();
            defaultExplanation = "WSR_NeedBeaconExplanation".Translate();
        }

        public override AlertReport GetReport()
        {
            return WaveSurvivalComponent.ModeActive && !WaveSurvivalComponent.AnyHomeHasBeacon();
        }
    }
}

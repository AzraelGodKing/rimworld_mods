using Verse;

namespace DateNight
{
    public class DateNightSettings : ModSettings
    {
        /// <summary>
        /// When true, only shortens GetLovinMtbHours while on Lovin schedule.
        /// Vanilla post-job canLovinTick cooldown stays intact (pregnancy-safer).
        /// </summary>
        public bool pregnancySafeCooldown = true;

        /// <summary>
        /// When pregnancySafeCooldown is false, also mirrors Always Do Lovin's ~100-tick cooldown.
        /// </summary>
        public bool eagerCooldown;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref pregnancySafeCooldown, "pregnancySafeCooldown", true);
            Scribe_Values.Look(ref eagerCooldown, "eagerCooldown", false);
        }

        public void ResetToDefaults()
        {
            pregnancySafeCooldown = true;
            eagerCooldown = false;
        }
    }
}

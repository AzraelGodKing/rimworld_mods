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

        /// <summary>
        /// Adults on Lovin hours can use a bed alone when a partner/double is not available.
        /// </summary>
        public bool allowSelfLovin = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref pregnancySafeCooldown, "pregnancySafeCooldown", true);
            Scribe_Values.Look(ref eagerCooldown, "eagerCooldown", false);
            Scribe_Values.Look(ref allowSelfLovin, "allowSelfLovin", true);
        }

        public void ResetToDefaults()
        {
            pregnancySafeCooldown = true;
            eagerCooldown = false;
            allowSelfLovin = true;
        }
    }
}

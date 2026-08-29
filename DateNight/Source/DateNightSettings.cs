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

        /// <summary>
        /// Claim the rendezvous double for the Lovin window, then restore previous beds.
        /// </summary>
        public bool allowWindowBedClaim = true;

        /// <summary>
        /// Dates pick a real activity (dinner, walk, stargazing, dancing, gift...)
        /// instead of always standing at a gather spot.
        /// </summary>
        public bool enableDateActivities = true;

        /// <summary>
        /// Date outcomes vary (wonderful / nice / awkward) with venue beauty and weather.
        /// </summary>
        public bool enableDateQuality = true;

        /// <summary>
        /// Gift dates may consume a small luxury item (beer, chocolate...).
        /// </summary>
        public bool allowGiftDates = true;

        /// <summary>
        /// A finished date improves lovin chance for the next in-game day.
        /// </summary>
        public bool postDateLovinBoost = true;

        /// <summary>
        /// Letter on the love-bond anniversary; a date that day is a peak mood,
        /// and skipping it leaves a missed-anniversary thought.
        /// </summary>
        public bool enableAnniversaries = true;

        /// <summary>
        /// Couples remember a favourite venue and prefer to go back; ruined dates can sour it.
        /// </summary>
        public bool rememberFavoriteSpot = true;

        /// <summary>
        /// Two couples with overlapping Date hours may share a venue.
        /// </summary>
        public bool allowDoubleDates = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref pregnancySafeCooldown, "pregnancySafeCooldown", true);
            Scribe_Values.Look(ref eagerCooldown, "eagerCooldown", false);
            Scribe_Values.Look(ref allowSelfLovin, "allowSelfLovin", true);
            Scribe_Values.Look(ref allowWindowBedClaim, "allowWindowBedClaim", true);
            Scribe_Values.Look(ref enableDateActivities, "enableDateActivities", true);
            Scribe_Values.Look(ref enableDateQuality, "enableDateQuality", true);
            Scribe_Values.Look(ref allowGiftDates, "allowGiftDates", true);
            Scribe_Values.Look(ref postDateLovinBoost, "postDateLovinBoost", true);
            Scribe_Values.Look(ref enableAnniversaries, "enableAnniversaries", true);
            Scribe_Values.Look(ref rememberFavoriteSpot, "rememberFavoriteSpot", true);
            Scribe_Values.Look(ref allowDoubleDates, "allowDoubleDates", true);
        }

        public void ResetToDefaults()
        {
            pregnancySafeCooldown = true;
            eagerCooldown = false;
            allowSelfLovin = true;
            allowWindowBedClaim = true;
            enableDateActivities = true;
            enableDateQuality = true;
            allowGiftDates = true;
            postDateLovinBoost = true;
            enableAnniversaries = true;
            rememberFavoriteSpot = true;
            allowDoubleDates = true;
        }
    }
}

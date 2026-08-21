using RimWorld;
using Verse;

namespace DateNight
{
    [DefOf]
    public static class DateNightDefOf
    {
        public static TimeAssignmentDef DateNight_Lovin;
        public static TimeAssignmentDef DateNight_Date;
        public static JobDef DateNight_SelfLovin;
        public static JobDef DateNight_GoOnDate;
        public static ThoughtDef DateNight_PrivateTime;
        public static ThoughtDef DateNight_HadADate;
        public static ThoughtDef DateNight_StoodUp;
        public static ThoughtDef DateNight_MadeIt;
        public static ThoughtDef DateNight_DateWonderful;
        public static ThoughtDef DateNight_DateAwkward;
        public static ThoughtDef DateNight_DateRuined;
        public static ThoughtDef DateNight_ReceivedGift;

        static DateNightDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DateNightDefOf));
        }
    }
}

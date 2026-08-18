using RimWorld;
using Verse;

namespace DateNight
{
    [DefOf]
    public static class DateNightDefOf
    {
        public static TimeAssignmentDef DateNight_Lovin;
        public static JobDef DateNight_SelfLovin;
        public static ThoughtDef DateNight_PrivateTime;

        static DateNightDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DateNightDefOf));
        }
    }
}

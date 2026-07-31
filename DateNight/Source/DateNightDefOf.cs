using RimWorld;
using Verse;

namespace DateNight
{
    [DefOf]
    public static class DateNightDefOf
    {
        public static TimeAssignmentDef DateNight_Lovin;

        static DateNightDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DateNightDefOf));
        }
    }
}

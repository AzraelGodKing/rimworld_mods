using RimWorld;
using Verse;

namespace Strata
{
    [DefOf]
    public static class SunkenRuinDefOf
    {
        public static SitePartDef Strata_SunkenRuin;

        public static ThingDef Strata_RuinStairsDown;

        public static IncidentDef Strata_SunkenRuinDiscovered;

        static SunkenRuinDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SunkenRuinDefOf));
        }
    }
}

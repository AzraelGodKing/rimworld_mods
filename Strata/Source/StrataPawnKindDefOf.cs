using RimWorld;
using Verse;

namespace Strata
{
    [DefOf]
    public static class StrataPawnKindDefOf
    {
        public static PawnKindDef Strata_Canary;

        static StrataPawnKindDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataPawnKindDefOf));
        }
    }
}

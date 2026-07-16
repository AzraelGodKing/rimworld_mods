using RimWorld;
using Verse;

namespace Strata
{
    public static class StrataCanaryBirdUtility
    {
        public static bool IsMineCanary(Pawn pawn) =>
            pawn?.kindDef == StrataPawnKindDefOf.Strata_Canary;

        public static bool IsAssignableBird(Pawn pawn) =>
            pawn != null
            && !pawn.Dead
            && pawn.RaceProps?.Animal == true
            && pawn.RaceProps.body?.defName == "Bird";
    }
}

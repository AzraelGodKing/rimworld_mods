using RimWorld;
using Verse;

namespace Strata
{
    [DefOf]
    public static class QuestSiteDefOf
    {
        public static SitePartDef Strata_CollapsedMine;

        public static SitePartDef Strata_SealedVault;

        public static SitePartDef Strata_GeothermalVent;

        public static ThingDef Strata_MineStairsDown;

        public static ThingDef Strata_VaultStairsDown;

        public static ThingDef Strata_VentStairsDown;

        public static IncidentDef Strata_CollapsedMineDiscovered;

        public static IncidentDef Strata_SealedVaultDiscovered;

        public static IncidentDef Strata_GeothermalVentDiscovered;

        static QuestSiteDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(QuestSiteDefOf));
        }
    }
}

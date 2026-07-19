using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    [DefOf]
    public static class StrataDefOf
    {
        public static WorkTypeDef Mining;

        public static WorkTypeDef PlantCutting;

        public static WorkTypeDef Cooking;

        public static WorkTypeDef Crafting;

        public static JobDef Strata_HaulToLevel;

        public static JobDef Strata_EscortToPortal;

        [MayRequire("Ludeon.RimWorld.Biotech")]
        public static JobDef Strata_BringBabyToLevel;

        public static JobDef Strata_BringPrisonerToLevel;

        public static JobDef Strata_CaptureToLevel;

        [MayRequire("Ludeon.RimWorld.Anomaly")]
        public static JobDef Strata_BringEntityToLevel;

        static StrataDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataDefOf));
        }
    }

    [StaticConstructorOnStartup]
    public static class StrataInit
    {
        static StrataInit()
        {
            ExhaustAutoPatch.Apply();
            GasNetAdapter.Inject();
            new Harmony("azraelgodking.strata").PatchAll();
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                StrataBuildInfo.LogStartup();
                StrataMultiFloorStairsUtility.ApplyFromSettings();
            });
        }
    }
}

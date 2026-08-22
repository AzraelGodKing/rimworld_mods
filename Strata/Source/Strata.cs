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

        public static JobDef Strata_RescueToLevel;

        [MayRequire("Ludeon.RimWorld.Anomaly")]
        public static JobDef Strata_BringEntityToLevel;

        public static MapMeshFlagDef Strata_BelowThings;

        public static JobDef Strata_CrossLevelAttack;

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
            Harmony harmony = new Harmony("azraelgodking.strata");
            harmony.PatchAll();
            StrataCombatExtendedSoftCompat.TryPatch(new Harmony("azraelgodking.strata.ce"));
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                StrataBuildInfo.LogStartup();
                StrataMultiFloorStairsUtility.ApplyFromSettings();
            });
        }
    }
}

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
            new Harmony("azraelgodking.strata").PatchAll();
        }
    }
}

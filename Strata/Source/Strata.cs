using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    [DefOf]
    public static class StrataDefOf
    {
        public static WorkTypeDef Mining;

        public static WorkTypeDef PlantCutting;

        public static WorkTypeDef Cooking;

        public static WorkTypeDef Crafting;

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
            new Harmony("azraelgodking.strata").PatchAll();
        }
    }
}

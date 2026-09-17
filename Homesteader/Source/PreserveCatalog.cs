using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Packed-lot and pantry "kind" list. Keep in sync with Homesteader_PackPreserveCrate ingredients.
    /// </summary>
    internal static class PreserveCatalog
    {
        internal static readonly string[] DefNames =
        {
            "Homesteader_Jam",
            "Homesteader_CannedJam",
            "Homesteader_Cheese",
            "Homesteader_WaxedCheese",
            "Homesteader_SmokedCheese",
            "Homesteader_Cider",
            "Homesteader_Jerky",
            "Homesteader_PickledVegetables",
            "Homesteader_SmokedMeat",
            "Homesteader_SaltedMeat",
            "Homesteader_SmokedFish",
            "Homesteader_SaltedFish",
            "Homesteader_DriedProduce",
            "Homesteader_FruitLeather",
            "Homesteader_DriedMushrooms",
            "Homesteader_Honey",
            "Homesteader_MapleSyrup",
            "Homesteader_CannedStew",
            "Homesteader_Hardtack",
            "Homesteader_Sausage"
        };

        internal const string PreserveCrateDefName = "Homesteader_PreserveCrate";
        internal const int PreserveCrateItemCount = 15;
        /// <summary>Mixed-lot variety credit so a crate is not "1 kind".</summary>
        internal const int PreserveCrateVarietyKinds = 8;

        private static HashSet<string> defNameSet;
        private static float cachedAvgNutrition = -1f;

        internal static bool IsPreserveCrate(ThingDef def)
        {
            return def != null && def.defName == PreserveCrateDefName;
        }

        internal static bool IsPreserveKind(ThingDef def)
        {
            if (def?.defName == null)
            {
                return false;
            }

            if (defNameSet == null)
            {
                defNameSet = new HashSet<string>(DefNames);
            }

            return defNameSet.Contains(def.defName);
        }

        internal static float AverageItemNutrition()
        {
            if (cachedAvgNutrition >= 0f)
            {
                return cachedAvgNutrition;
            }

            float sum = 0f;
            int count = 0;
            for (int i = 0; i < DefNames.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(DefNames[i]);
                if (def == null || !def.IsIngestible)
                {
                    continue;
                }

                sum += def.GetStatValueAbstract(StatDefOf.Nutrition);
                count++;
            }

            cachedAvgNutrition = count > 0 ? sum / count : 0.2f;
            return cachedAvgNutrition;
        }
    }
}

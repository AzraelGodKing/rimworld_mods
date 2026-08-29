using System.Collections.Generic;
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
            "Homesteader_Sausage",
            "Homesteader_PreserveCrate"
        };

        private static HashSet<string> defNameSet;

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
    }
}

using UnityEngine;
using Verse;

namespace NanoHiveReforged
{
    [StaticConstructorOnStartup]
    public static class NanoHiveTex
    {
        public static readonly Texture2D Range = ContentFinder<Texture2D>.Get("UI/NanoHive/Range");
        public static readonly Texture2D Rate = ContentFinder<Texture2D>.Get("UI/NanoHive/Rate");
        public static readonly Texture2D Default = ContentFinder<Texture2D>.Get("UI/NanoHive/Default");
        public static readonly Texture2D CopyAll = ContentFinder<Texture2D>.Get("UI/NanoHive/CopyAll");

        // Toggle icons borrow the uiIcon of a vanilla thing of that kind.
        public static readonly Texture2D Buildings = IconOf("Wall");
        public static readonly Texture2D Items = IconOf("ComponentIndustrial");
        public static readonly Texture2D Gear = IconOf("Apparel_FlakVest");
        public static readonly Texture2D Inventory = IconOf("MedicineIndustrial");

        public static Texture2D For(HiveToggle toggle)
        {
            switch (toggle)
            {
                case HiveToggle.Buildings: return Buildings;
                case HiveToggle.Items: return Items;
                case HiveToggle.Gear: return Gear;
                default: return Inventory;
            }
        }

        private static Texture2D IconOf(string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            return def?.uiIcon != null ? def.uiIcon : BaseContent.BadTex;
        }
    }
}

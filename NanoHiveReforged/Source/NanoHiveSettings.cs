using UnityEngine;
using Verse;

namespace NanoHiveReforged
{
    /// <summary>
    /// Per-player settings. Since 2.0.0 the gameplay values here are only the
    /// defaults for a save that has no NanoHive data yet (a new colony, or a 1.x
    /// save loaded for the first time). The class and field names are unchanged
    /// so the 1.x settings file still reads.
    /// </summary>
    public class NanoHiveSettings : ModSettings
    {
        public int repairPerTickRare = 1;
        public bool allowRepairApparelEquipment = true;
        public bool allowRepairInventoryItems = true;
        public bool useRangeOverride = false;
        public float repairRangeOverride = 12f;

        public HiveConfig NewColonyDefaults()
        {
            var config = new HiveConfig
            {
                range = useRangeOverride ? repairRangeOverride : DefaultRange,
                repairPerTarget = repairPerTickRare,
                repairGear = allowRepairApparelEquipment,
                repairInventory = allowRepairInventoryItems,
            };
            config.Clamp();
            return config;
        }

        public static float DefaultRange => NanoHiveDefOf.NanoHiveReforged != null
            ? NanoHiveProps.For(NanoHiveDefOf.NanoHiveReforged).defaultRange
            : 12f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref repairPerTickRare, "repairPerTickRare", 1);
            Scribe_Values.Look(ref allowRepairApparelEquipment, "allowRepairApparelEquipment", true);
            Scribe_Values.Look(ref allowRepairInventoryItems, "allowRepairInventoryItems", true);
            Scribe_Values.Look(ref useRangeOverride, "useRangeOverride", false);
            Scribe_Values.Look(ref repairRangeOverride, "repairRangeOverride", 12f);

            repairPerTickRare = Mathf.Clamp(repairPerTickRare, HiveConfig.MinRate, HiveConfig.MaxRate);
            repairRangeOverride = Mathf.Clamp(repairRangeOverride, HiveConfig.MinRange, HiveConfig.MaxRange);
        }
    }

    public class NanoHiveReforgedMod : Mod
    {
        public static NanoHiveSettings Settings;

        public NanoHiveReforgedMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<NanoHiveSettings>();
            NanoHiveReforgedBuildInfo.LogStartup(content);
        }

        public override string SettingsCategory()
        {
            return "NanoHive_SettingsCategory".Translate();
        }

        // Written once by WriteSettings() when the window closes, not every frame.
        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            list.Label("NanoHive_Settings_Intro".Translate());
            if (Current.ProgramState == ProgramState.Playing)
            {
                GUI.color = ColoredText.SubtleGrayColor;
                list.Label("NanoHive_Settings_InGame".Translate());
                GUI.color = Color.white;
            }
            list.GapLine();

            list.Label("NanoHive_Settings_Rate".Translate(Settings.repairPerTickRare));
            Settings.repairPerTickRare = Mathf.RoundToInt(list.Slider(Settings.repairPerTickRare, HiveConfig.MinRate, HiveConfig.MaxRate));

            list.GapLine();
            list.CheckboxLabeled("NanoHive_Settings_Gear".Translate(), ref Settings.allowRepairApparelEquipment);
            list.CheckboxLabeled("NanoHive_Settings_Inventory".Translate(), ref Settings.allowRepairInventoryItems);

            list.GapLine();
            list.CheckboxLabeled("NanoHive_Settings_RangeOverride".Translate(), ref Settings.useRangeOverride);
            if (Settings.useRangeOverride)
            {
                list.Label("NanoHive_Settings_Range".Translate(Settings.repairRangeOverride.ToString("F0")));
                Settings.repairRangeOverride = Mathf.Round(list.Slider(Settings.repairRangeOverride, HiveConfig.MinRange, HiveConfig.MaxRange));
            }
            else
            {
                list.Label("NanoHive_Settings_RangeDefault".Translate(NanoHiveSettings.DefaultRange.ToString("F0")));
            }

            list.End();
        }
    }
}

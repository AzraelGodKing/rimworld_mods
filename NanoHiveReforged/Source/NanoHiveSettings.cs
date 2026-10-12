using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NanoHiveReforged
{
    public class NanoHiveSettings : ModSettings
    {
        public int repairPerTickRare = 1;
        public bool allowRepairApparelEquipment = true;
        public bool allowRepairInventoryItems = true;
        public bool useRangeOverride = false;
        public float repairRangeOverride = 12f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref repairPerTickRare, "repairPerTickRare", 1);
            Scribe_Values.Look(ref allowRepairApparelEquipment, "allowRepairApparelEquipment", true);
            Scribe_Values.Look(ref allowRepairInventoryItems, "allowRepairInventoryItems", true);
            Scribe_Values.Look(ref useRangeOverride, "useRangeOverride", false);
            Scribe_Values.Look(ref repairRangeOverride, "repairRangeOverride", 12f);

            repairPerTickRare = repairPerTickRare < 1 ? 1 : repairPerTickRare;
            if (repairRangeOverride < 1f)
            {
                repairRangeOverride = 1f;
            }
        }
    }

    public class NanoHiveReforgedMod : Mod
    {
        public static NanoHiveSettings Settings;

        public NanoHiveReforgedMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<NanoHiveSettings>();
        }

        public override string SettingsCategory()
        {
            return "NanoHive Reforged";
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            list.Label("Repair points per tick (TickRare): " + Settings.repairPerTickRare);
            Settings.repairPerTickRare = (int)list.Slider(Settings.repairPerTickRare, 1f, 10f);

            list.GapLine();
            list.CheckboxLabeled("Repair colonist apparel + equipment", ref Settings.allowRepairApparelEquipment);
            list.CheckboxLabeled("Repair colonist inventory items", ref Settings.allowRepairInventoryItems);

            list.GapLine();
            list.CheckboxLabeled("Override repair radius", ref Settings.useRangeOverride);
            if (Settings.useRangeOverride)
            {
                list.Label("Repair radius: " + Settings.repairRangeOverride.ToString("F1"));
                Settings.repairRangeOverride = list.Slider(Settings.repairRangeOverride, 1f, 30f);
            }
            else
            {
                list.Label("Repair radius uses ThingDef specialDisplayRadius.");
            }

            list.End();
            Settings.Write();
        }
    }
}

#nullable enable
using Verse;

namespace Niceties
{
    public partial record struct NicetiesSimFlags
    {
        /// <summary>
        /// Scribes with the same keys as 1.x, so old Mod Options files and old save
        /// snapshots load unchanged. Missing keys take the default.
        /// </summary>
        public void Expose()
        {
            NicetiesSimFlags d = Defaults;
            Scribe_Values.Look(ref enableApparelCare, "enableApparelCare", d.enableApparelCare);
            Scribe_Values.Look(ref apparelQualityScaling, "apparelQualityScaling", d.apparelQualityScaling);
            Scribe_Values.Look(ref apparelCraftingBonus, "apparelCraftingBonus", d.apparelCraftingBonus);
            Scribe_Values.Look(ref protectCorpseApparel, "protectCorpseApparel", d.protectCorpseApparel);
            Scribe_Values.Look(ref allowThroneAltars, "allowThroneAltars", d.allowThroneAltars);
            Scribe_Values.Look(ref wearAnyGender, "wearAnyGender", d.wearAnyGender);
            Scribe_Values.Look(ref wearAnyGenderGenerated, "wearAnyGenderGenerated", d.wearAnyGenderGenerated);
            Scribe_Values.Look(ref hideCryptosleep, "hideCryptosleep", d.hideCryptosleep);
            Scribe_Values.Look(ref meleeHunting, "meleeHunting", d.meleeHunting);
            Scribe_Values.Look(ref unarmedHunting, "unarmedHunting", d.unarmedHunting);
            Scribe_Values.Look(ref meleeHuntMaxBodySize, "meleeHuntMaxBodySize", d.meleeHuntMaxBodySize);
            Scribe_Values.Look(ref meleeHuntMaxRevengeChance, "meleeHuntMaxRevengeChance", d.meleeHuntMaxRevengeChance);
            Scribe_Values.Look(ref enableSharedRooms, "enableSharedRooms", d.enableSharedRooms);
            Scribe_Values.Look(ref skipDisturbedSleepWhenSharing, "skipDisturbedSleepWhenSharing",
                d.skipDisturbedSleepWhenSharing);
            Scribe_Values.Look(ref enableLeaveAWayOut, "enableLeaveAWayOut", d.enableLeaveAWayOut);
            Scribe_Values.Look(ref leaveAWayOutMaxRoom, "leaveAWayOutMaxRoom", d.leaveAWayOutMaxRoom);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Clamp();
            }
        }

    }

    /// <summary>
    /// Every nicety has a master bool. Patches must no-op when that bool is off
    /// so players can mix features. Nested knobs only apply while the master is on.
    /// </summary>
    public class NicetiesSettings : ModSettings
    {
        public NicetiesSimFlags sim = NicetiesSimFlags.Defaults;
        public NicetiesUiPrefs ui = NicetiesUiPrefs.Defaults;

        public override void ExposeData()
        {
            sim.Expose();
            NicetiesUiPrefs d = NicetiesUiPrefs.Defaults;
            Scribe_Values.Look(ref ui.showCryptosleepCount, "showCryptosleepCount", d.showCryptosleepCount);
            Scribe_Values.Look(ref ui.cryptosleepRoster, "cryptosleepRoster", d.cryptosleepRoster);
            Scribe_Values.Look(ref ui.hideGrowthVats, "hideGrowthVats", d.hideGrowthVats);
            Scribe_Values.Look(ref ui.hidePods, "hidePods", d.hidePods);
            Scribe_Values.Look(ref ui.sharedRoomNobleAlert, "sharedRoomNobleAlert", d.sharedRoomNobleAlert);
            Scribe_Values.Look(ref ui.showEncloseOverlay, "showEncloseOverlay", d.showEncloseOverlay);
        }

        public void ApplyPreset(NicetiesPreset preset)
        {
            sim.ApplyKnobsFrom(NicetiesPresets.Knobs(preset));
        }

        /// <summary>The one full reset: every toggle, every knob and the UI preferences.</summary>
        public void ResetToDefaults()
        {
            sim = NicetiesSimFlags.Defaults;
            ui = NicetiesUiPrefs.Defaults;
        }

        /// <summary>Per-feature reset, including that feature's UI-only preferences (AZR-656).</summary>
        public void ResetFeature(NicetiesFeature feature)
        {
            sim.ResetFeature(feature);
            ui.ResetFeature(feature);
        }

        /// <summary>Soft / Default / Hard / Custom for the options label (AZR-293).</summary>
        public string ActivePresetLabelKey()
        {
            switch (NicetiesPresets.Active(sim))
            {
                case NicetiesPreset.Soft:
                    return "Niceties_Settings_PresetSoft";
                case NicetiesPreset.Default:
                    return "Niceties_Settings_PresetDefault";
                case NicetiesPreset.Hard:
                    return "Niceties_Settings_PresetHard";
                default:
                    return "Niceties_Settings_PresetCustom";
            }
        }
    }
}

#nullable enable
using Verse;

namespace Niceties
{
    /// <summary>
    /// Every setting that changes the simulation. A record struct, so copying is an
    /// assignment and equality covers every field: a new field cannot be missed by
    /// copy, compare or the save snapshot (AZR-533). Start from <see cref="Defaults"/>,
    /// never from <c>default</c>, which is all-off.
    /// </summary>
    public record struct NicetiesSimFlags
    {
        // Master toggles. Patches must no-op when their toggle is off.
        public bool enableApparelCare;
        public bool allowThroneAltars;
        public bool wearAnyGender;
        public bool hideCryptosleep;
        public bool meleeHunting;
        public bool enableSharedRooms;
        public bool enableLeaveAWayOut;

        // Nested knobs. Presets only touch these (AZR-700).
        public bool apparelQualityScaling;
        public bool apparelCraftingBonus;
        public bool protectCorpseApparel;
        public bool unarmedHunting;
        public float meleeHuntMaxBodySize;
        public bool skipDisturbedSleepWhenSharing;

        // Sub-toggle, not a preset knob: a choice about who the feature reaches.
        public bool wearAnyGenderGenerated;

        public static NicetiesSimFlags Defaults => new NicetiesSimFlags
        {
            enableApparelCare = true,
            allowThroneAltars = true,
            wearAnyGender = true,
            hideCryptosleep = true,
            meleeHunting = true,
            enableSharedRooms = true,
            enableLeaveAWayOut = true,
            apparelQualityScaling = true,
            apparelCraftingBonus = true,
            protectCorpseApparel = false,
            wearAnyGenderGenerated = false,
            unarmedHunting = false,
            meleeHuntMaxBodySize = NicetiesSettings.DefaultMeleeCap,
            skipDisturbedSleepWhenSharing = true,
        };

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
            Scribe_Values.Look(ref enableSharedRooms, "enableSharedRooms", d.enableSharedRooms);
            Scribe_Values.Look(ref skipDisturbedSleepWhenSharing, "skipDisturbedSleepWhenSharing",
                d.skipDisturbedSleepWhenSharing);
            Scribe_Values.Look(ref enableLeaveAWayOut, "enableLeaveAWayOut", d.enableLeaveAWayOut);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Clamp();
            }
        }

        public void Clamp()
        {
            meleeHuntMaxBodySize = meleeHuntMaxBodySize < NicetiesSettings.MinMeleeCap
                ? NicetiesSettings.MinMeleeCap
                : (meleeHuntMaxBodySize > NicetiesSettings.MaxMeleeCap ? NicetiesSettings.MaxMeleeCap : meleeHuntMaxBodySize);
        }

        /// <summary>Copies only the nested knobs of a preset; master toggles stay as they are.</summary>
        public void ApplyKnobsFrom(NicetiesSimFlags preset)
        {
            apparelQualityScaling = preset.apparelQualityScaling;
            apparelCraftingBonus = preset.apparelCraftingBonus;
            protectCorpseApparel = preset.protectCorpseApparel;
            unarmedHunting = preset.unarmedHunting;
            meleeHuntMaxBodySize = preset.meleeHuntMaxBodySize;
            skipDisturbedSleepWhenSharing = preset.skipDisturbedSleepWhenSharing;
        }

        public bool KnobsMatch(NicetiesSimFlags preset)
        {
            NicetiesSimFlags probe = this;
            probe.ApplyKnobsFrom(preset);
            probe.meleeHuntMaxBodySize = meleeHuntMaxBodySize;
            return probe == this && Approx(meleeHuntMaxBodySize, preset.meleeHuntMaxBodySize);
        }

        private static bool Approx(float a, float b)
        {
            float d = a - b;
            return d < 0.001f && d > -0.001f;
        }
    }

    public enum NicetiesPreset
    {
        Soft,
        Default,
        Hard,
    }

    /// <summary>
    /// Every nicety has a master bool. Patches must no-op when that bool is off
    /// so players can mix features. Nested knobs only apply while the master is on.
    /// </summary>
    public class NicetiesSettings : ModSettings
    {
        public const float DefaultMeleeCap = 1.5f;
        public const float MinMeleeCap = 0.2f;
        public const float MaxMeleeCap = 8f;

        public NicetiesSimFlags sim = NicetiesSimFlags.Defaults;

        // UI-only: never baked into the save snapshot.
        public bool showCryptosleepCount = true;

        public override void ExposeData()
        {
            sim.Expose();
            Scribe_Values.Look(ref showCryptosleepCount, "showCryptosleepCount", true);
        }

        /// <summary>Knob values for each preset. Master toggles are not part of a preset.</summary>
        public static NicetiesSimFlags PresetKnobs(NicetiesPreset preset)
        {
            NicetiesSimFlags f = NicetiesSimFlags.Defaults;
            switch (preset)
            {
                case NicetiesPreset.Soft:
                    f.apparelQualityScaling = false;
                    f.apparelCraftingBonus = true;
                    f.protectCorpseApparel = true;
                    f.unarmedHunting = true;
                    f.meleeHuntMaxBodySize = MaxMeleeCap;
                    break;
                case NicetiesPreset.Hard:
                    f.apparelCraftingBonus = false;
                    f.protectCorpseApparel = false;
                    f.unarmedHunting = false;
                    f.meleeHuntMaxBodySize = 0.8f;
                    break;
            }

            return f;
        }

        public void ApplyPreset(NicetiesPreset preset)
        {
            sim.ApplyKnobsFrom(PresetKnobs(preset));
        }

        /// <summary>The one full reset: every toggle, every knob and the UI preference.</summary>
        public void ResetToDefaults()
        {
            sim = NicetiesSimFlags.Defaults;
            showCryptosleepCount = true;
        }

        /// <summary>Soft / Default / Hard / Custom for the options label (AZR-293).</summary>
        public string ActivePresetLabelKey()
        {
            sim.Clamp();
            if (sim.KnobsMatch(PresetKnobs(NicetiesPreset.Soft)))
            {
                return "Niceties_Settings_PresetSoft";
            }

            if (sim.KnobsMatch(PresetKnobs(NicetiesPreset.Default)))
            {
                return "Niceties_Settings_PresetDefault";
            }

            if (sim.KnobsMatch(PresetKnobs(NicetiesPreset.Hard)))
            {
                return "Niceties_Settings_PresetHard";
            }

            return "Niceties_Settings_PresetCustom";
        }
    }
}

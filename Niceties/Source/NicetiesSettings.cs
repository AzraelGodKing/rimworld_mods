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
        public float meleeHuntMaxRevengeChance;
        public bool skipDisturbedSleepWhenSharing;

        // Sub-toggles and limits, not preset knobs: choices about who or what a nicety reaches.
        public bool wearAnyGenderGenerated;
        public int leaveAWayOutMaxRoom;

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
            unarmedHunting = false,
            meleeHuntMaxBodySize = NicetiesSettings.DefaultMeleeCap,
            meleeHuntMaxRevengeChance = 1f,
            skipDisturbedSleepWhenSharing = true,
            wearAnyGenderGenerated = false,
            leaveAWayOutMaxRoom = NicetiesSettings.DefaultMaxRoom,
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

        public void Clamp()
        {
            meleeHuntMaxBodySize = Clamp(meleeHuntMaxBodySize, NicetiesSettings.MinMeleeCap, NicetiesSettings.MaxMeleeCap);
            meleeHuntMaxRevengeChance = Clamp(meleeHuntMaxRevengeChance, 0f, 1f);
            leaveAWayOutMaxRoom = leaveAWayOutMaxRoom < NicetiesSettings.MinMaxRoom
                ? NicetiesSettings.MinMaxRoom
                : (leaveAWayOutMaxRoom > NicetiesSettings.MaxMaxRoom ? NicetiesSettings.MaxMaxRoom : leaveAWayOutMaxRoom);
        }

        /// <summary>Copies only the nested knobs of a preset; master toggles stay as they are.</summary>
        public void ApplyKnobsFrom(NicetiesSimFlags preset)
        {
            apparelQualityScaling = preset.apparelQualityScaling;
            apparelCraftingBonus = preset.apparelCraftingBonus;
            protectCorpseApparel = preset.protectCorpseApparel;
            unarmedHunting = preset.unarmedHunting;
            meleeHuntMaxBodySize = preset.meleeHuntMaxBodySize;
            meleeHuntMaxRevengeChance = preset.meleeHuntMaxRevengeChance;
            skipDisturbedSleepWhenSharing = preset.skipDisturbedSleepWhenSharing;
        }

        public bool KnobsMatch(NicetiesSimFlags preset)
        {
            NicetiesSimFlags probe = this;
            probe.ApplyKnobsFrom(preset);
            probe.meleeHuntMaxBodySize = meleeHuntMaxBodySize;
            probe.meleeHuntMaxRevengeChance = meleeHuntMaxRevengeChance;
            return probe == this
                && Approx(meleeHuntMaxBodySize, preset.meleeHuntMaxBodySize)
                && Approx(meleeHuntMaxRevengeChance, preset.meleeHuntMaxRevengeChance);
        }

        /// <summary>One nicety back to its defaults: its toggle, knobs and limits (AZR-656).</summary>
        public void ResetFeature(NicetiesFeature feature)
        {
            NicetiesSimFlags d = Defaults;
            switch (feature)
            {
                case NicetiesFeature.ApparelCare:
                    enableApparelCare = d.enableApparelCare;
                    apparelQualityScaling = d.apparelQualityScaling;
                    apparelCraftingBonus = d.apparelCraftingBonus;
                    protectCorpseApparel = d.protectCorpseApparel;
                    break;
                case NicetiesFeature.ThroneAltar:
                    allowThroneAltars = d.allowThroneAltars;
                    break;
                case NicetiesFeature.WearAnyGender:
                    wearAnyGender = d.wearAnyGender;
                    wearAnyGenderGenerated = d.wearAnyGenderGenerated;
                    break;
                case NicetiesFeature.HideCryptosleep:
                    hideCryptosleep = d.hideCryptosleep;
                    break;
                case NicetiesFeature.MeleeHunting:
                    meleeHunting = d.meleeHunting;
                    unarmedHunting = d.unarmedHunting;
                    meleeHuntMaxBodySize = d.meleeHuntMaxBodySize;
                    meleeHuntMaxRevengeChance = d.meleeHuntMaxRevengeChance;
                    break;
                case NicetiesFeature.SharedRooms:
                    enableSharedRooms = d.enableSharedRooms;
                    skipDisturbedSleepWhenSharing = d.skipDisturbedSleepWhenSharing;
                    break;
                case NicetiesFeature.LeaveAWayOut:
                    enableLeaveAWayOut = d.enableLeaveAWayOut;
                    leaveAWayOutMaxRoom = d.leaveAWayOutMaxRoom;
                    break;
            }
        }

        private static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
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

    public enum NicetiesFeature
    {
        ApparelCare,
        ThroneAltar,
        WearAnyGender,
        HideCryptosleep,
        MeleeHunting,
        SharedRooms,
        LeaveAWayOut,
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
        public const int DefaultMaxRoom = 80;
        public const int MinMaxRoom = 20;
        public const int MaxMaxRoom = 400;

        public NicetiesSimFlags sim = NicetiesSimFlags.Defaults;

        // UI-only: never baked into the save snapshot, and never synced in Multiplayer.
        public bool showCryptosleepCount = true;
        public bool cryptosleepRoster = true;
        public bool hideGrowthVats = false;
        public bool hidePods = false;
        public bool sharedRoomNobleAlert = true;
        public bool showEncloseOverlay = true;

        public override void ExposeData()
        {
            sim.Expose();
            Scribe_Values.Look(ref showCryptosleepCount, "showCryptosleepCount", true);
            Scribe_Values.Look(ref cryptosleepRoster, "cryptosleepRoster", true);
            Scribe_Values.Look(ref hideGrowthVats, "hideGrowthVats", false);
            Scribe_Values.Look(ref hidePods, "hidePods", false);
            Scribe_Values.Look(ref sharedRoomNobleAlert, "sharedRoomNobleAlert", true);
            Scribe_Values.Look(ref showEncloseOverlay, "showEncloseOverlay", true);
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
                    f.meleeHuntMaxRevengeChance = 0.5f;
                    break;
            }

            return f;
        }

        public void ApplyPreset(NicetiesPreset preset)
        {
            sim.ApplyKnobsFrom(PresetKnobs(preset));
        }

        /// <summary>The one full reset: every toggle, every knob and the UI preferences.</summary>
        public void ResetToDefaults()
        {
            sim = NicetiesSimFlags.Defaults;
            for (int i = 0; i <= (int)NicetiesFeature.LeaveAWayOut; i++)
            {
                ResetUiFor((NicetiesFeature)i);
            }
        }

        /// <summary>Per-feature reset, including that feature's UI-only preferences (AZR-656).</summary>
        public void ResetFeature(NicetiesFeature feature)
        {
            sim.ResetFeature(feature);
            ResetUiFor(feature);
        }

        private void ResetUiFor(NicetiesFeature feature)
        {
            switch (feature)
            {
                case NicetiesFeature.HideCryptosleep:
                    showCryptosleepCount = true;
                    cryptosleepRoster = true;
                    hideGrowthVats = false;
                    hidePods = false;
                    break;
                case NicetiesFeature.SharedRooms:
                    sharedRoomNobleAlert = true;
                    break;
                case NicetiesFeature.LeaveAWayOut:
                    showEncloseOverlay = true;
                    break;
            }
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

        /// <summary>Snapshot of the UI-only fields, for change detection in the options window.</summary>
        internal (bool, bool, bool, bool, bool, bool) UiState()
        {
            return (showCryptosleepCount, cryptosleepRoster, hideGrowthVats, hidePods, sharedRoomNobleAlert,
                showEncloseOverlay);
        }
    }
}

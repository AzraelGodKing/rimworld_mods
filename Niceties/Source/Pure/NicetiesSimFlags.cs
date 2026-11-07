#nullable enable

namespace Niceties
{
    /// <summary>Slider limits and defaults shared by the settings model and the options window.</summary>
    public static class NicetiesLimits
    {
        public const float DefaultMeleeCap = 1.5f;
        public const float MinMeleeCap = 0.2f;
        public const float MaxMeleeCap = 8f;
        public const int DefaultMaxRoom = 80;
        public const int MinMaxRoom = 20;
        public const int MaxMaxRoom = 400;
    }

    /// <summary>
    /// Every setting that changes the simulation. A record struct, so copying is an
    /// assignment and equality covers every field: a new field cannot be missed by
    /// copy, compare or the save snapshot (AZR-533). Start from <see cref="Defaults"/>,
    /// never from <c>default</c>, which is all-off.
    /// </summary>
    public partial record struct NicetiesSimFlags
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
            meleeHuntMaxBodySize = NicetiesLimits.DefaultMeleeCap,
            meleeHuntMaxRevengeChance = 1f,
            skipDisturbedSleepWhenSharing = true,
            wearAnyGenderGenerated = false,
            leaveAWayOutMaxRoom = NicetiesLimits.DefaultMaxRoom,
        };

        public void Clamp()
        {
            meleeHuntMaxBodySize = Clamp(meleeHuntMaxBodySize, NicetiesLimits.MinMeleeCap, NicetiesLimits.MaxMeleeCap);
            meleeHuntMaxRevengeChance = Clamp(meleeHuntMaxRevengeChance, 0f, 1f);
            leaveAWayOutMaxRoom = leaveAWayOutMaxRoom < NicetiesLimits.MinMaxRoom
                ? NicetiesLimits.MinMaxRoom
                : (leaveAWayOutMaxRoom > NicetiesLimits.MaxMaxRoom ? NicetiesLimits.MaxMaxRoom : leaveAWayOutMaxRoom);
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
    /// Local, UI-only preferences: never baked into the save snapshot and never synced in
    /// Multiplayer. A record struct for the same reason as <see cref="NicetiesSimFlags"/>.
    /// </summary>
    public record struct NicetiesUiPrefs
    {
        public bool showCryptosleepCount;
        public bool cryptosleepRoster;
        public bool hideGrowthVats;
        public bool hidePods;
        public bool sharedRoomNobleAlert;
        public bool showEncloseOverlay;

        public static NicetiesUiPrefs Defaults => new NicetiesUiPrefs
        {
            showCryptosleepCount = true,
            cryptosleepRoster = true,
            hideGrowthVats = false,
            hidePods = false,
            sharedRoomNobleAlert = true,
            showEncloseOverlay = true,
        };

        public void ResetFeature(NicetiesFeature feature)
        {
            NicetiesUiPrefs d = Defaults;
            switch (feature)
            {
                case NicetiesFeature.HideCryptosleep:
                    showCryptosleepCount = d.showCryptosleepCount;
                    cryptosleepRoster = d.cryptosleepRoster;
                    hideGrowthVats = d.hideGrowthVats;
                    hidePods = d.hidePods;
                    break;
                case NicetiesFeature.SharedRooms:
                    sharedRoomNobleAlert = d.sharedRoomNobleAlert;
                    break;
                case NicetiesFeature.LeaveAWayOut:
                    showEncloseOverlay = d.showEncloseOverlay;
                    break;
            }
        }
    }

    /// <summary>Preset knob values. Master toggles are never part of a preset (AZR-700).</summary>
    public static class NicetiesPresets
    {
        public static NicetiesSimFlags Knobs(NicetiesPreset preset)
        {
            NicetiesSimFlags f = NicetiesSimFlags.Defaults;
            switch (preset)
            {
                case NicetiesPreset.Soft:
                    f.apparelQualityScaling = false;
                    f.apparelCraftingBonus = true;
                    f.protectCorpseApparel = true;
                    f.unarmedHunting = true;
                    f.meleeHuntMaxBodySize = NicetiesLimits.MaxMeleeCap;
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

        /// <summary>The preset whose knobs match, or null for Custom.</summary>
        public static NicetiesPreset? Active(NicetiesSimFlags sim)
        {
            sim.Clamp();
            if (sim.KnobsMatch(Knobs(NicetiesPreset.Soft)))
            {
                return NicetiesPreset.Soft;
            }

            if (sim.KnobsMatch(Knobs(NicetiesPreset.Default)))
            {
                return NicetiesPreset.Default;
            }

            if (sim.KnobsMatch(Knobs(NicetiesPreset.Hard)))
            {
                return NicetiesPreset.Hard;
            }

            return null;
        }
    }
}

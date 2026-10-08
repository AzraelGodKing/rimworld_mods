using Verse;

namespace Niceties
{
    /// <summary>
    /// Every nicety has a master bool. Patches must no-op when that bool is off
    /// so players can mix features. Nested knobs only apply while the master is on.
    /// </summary>
    public class NicetiesSettings : ModSettings
    {
        public bool enableApparelCare = true;
        public bool apparelQualityScaling = true;
        public bool apparelCraftingBonus = true;
        public bool protectCorpseApparel = false;
        public bool enableWeaponCare = false;

        public bool allowThroneAltars = true;

        public bool wearAnyGender = true;

        public bool hideCryptosleep = true;

        public bool meleeHunting = true;
        public bool unarmedHunting = false;
        public float meleeHuntMaxBodySize = 1.5f;

        public bool enableSharedRooms = true;
        public bool skipDisturbedSleepWhenSharing = true;

        public bool enableLeaveAWayOut = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableApparelCare, "enableApparelCare", true);
            Scribe_Values.Look(ref apparelQualityScaling, "apparelQualityScaling", true);
            Scribe_Values.Look(ref apparelCraftingBonus, "apparelCraftingBonus", true);
            Scribe_Values.Look(ref protectCorpseApparel, "protectCorpseApparel", false);
            Scribe_Values.Look(ref enableWeaponCare, "enableWeaponCare", false);
            Scribe_Values.Look(ref allowThroneAltars, "allowThroneAltars", true);
            Scribe_Values.Look(ref wearAnyGender, "wearAnyGender", true);
            Scribe_Values.Look(ref hideCryptosleep, "hideCryptosleep", true);
            Scribe_Values.Look(ref meleeHunting, "meleeHunting", true);
            Scribe_Values.Look(ref unarmedHunting, "unarmedHunting", false);
            Scribe_Values.Look(ref meleeHuntMaxBodySize, "meleeHuntMaxBodySize", 1.5f);
            Scribe_Values.Look(ref enableSharedRooms, "enableSharedRooms", true);
            Scribe_Values.Look(ref skipDisturbedSleepWhenSharing, "skipDisturbedSleepWhenSharing", true);
            Scribe_Values.Look(ref enableLeaveAWayOut, "enableLeaveAWayOut", true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Clamp();
            }
        }

        public void Clamp()
        {
            meleeHuntMaxBodySize = meleeHuntMaxBodySize < 0.2f
                ? 0.2f
                : (meleeHuntMaxBodySize > 8f ? 8f : meleeHuntMaxBodySize);
        }

        public void CopyFrom(NicetiesSettings other)
        {
            if (other == null)
            {
                return;
            }

            enableApparelCare = other.enableApparelCare;
            apparelQualityScaling = other.apparelQualityScaling;
            apparelCraftingBonus = other.apparelCraftingBonus;
            protectCorpseApparel = other.protectCorpseApparel;
            enableWeaponCare = other.enableWeaponCare;
            allowThroneAltars = other.allowThroneAltars;
            wearAnyGender = other.wearAnyGender;
            hideCryptosleep = other.hideCryptosleep;
            meleeHunting = other.meleeHunting;
            unarmedHunting = other.unarmedHunting;
            meleeHuntMaxBodySize = other.meleeHuntMaxBodySize;
            enableSharedRooms = other.enableSharedRooms;
            skipDisturbedSleepWhenSharing = other.skipDisturbedSleepWhenSharing;
            enableLeaveAWayOut = other.enableLeaveAWayOut;
            Clamp();
        }

        public void ApplySoft()
        {
            ResetToDefaults();
            apparelQualityScaling = false;
            apparelCraftingBonus = true;
            protectCorpseApparel = true;
            enableWeaponCare = true;
            unarmedHunting = true;
            meleeHuntMaxBodySize = 8f;
        }

        public void ResetToDefaults()
        {
            enableApparelCare = true;
            apparelQualityScaling = true;
            apparelCraftingBonus = true;
            protectCorpseApparel = false;
            enableWeaponCare = false;
            allowThroneAltars = true;
            wearAnyGender = true;
            hideCryptosleep = true;
            meleeHunting = true;
            unarmedHunting = false;
            meleeHuntMaxBodySize = 1.5f;
            enableSharedRooms = true;
            skipDisturbedSleepWhenSharing = true;
            enableLeaveAWayOut = true;
        }

        public void ApplyHard()
        {
            ResetToDefaults();
            apparelCraftingBonus = false;
            protectCorpseApparel = false;
            enableWeaponCare = false;
            unarmedHunting = false;
            meleeHuntMaxBodySize = 0.8f;
        }

        public int ContentFingerprint()
        {
            unchecked
            {
                int h = enableApparelCare ? 1 : 0;
                h = (h * 397) ^ (apparelQualityScaling ? 1 : 0);
                h = (h * 397) ^ (apparelCraftingBonus ? 1 : 0);
                h = (h * 397) ^ (protectCorpseApparel ? 1 : 0);
                h = (h * 397) ^ (enableWeaponCare ? 1 : 0);
                h = (h * 397) ^ (allowThroneAltars ? 1 : 0);
                h = (h * 397) ^ (wearAnyGender ? 1 : 0);
                h = (h * 397) ^ (hideCryptosleep ? 1 : 0);
                h = (h * 397) ^ (meleeHunting ? 1 : 0);
                h = (h * 397) ^ (unarmedHunting ? 1 : 0);
                h = (h * 397) ^ meleeHuntMaxBodySize.GetHashCode();
                h = (h * 397) ^ (enableSharedRooms ? 1 : 0);
                h = (h * 397) ^ (skipDisturbedSleepWhenSharing ? 1 : 0);
                h = (h * 397) ^ (enableLeaveAWayOut ? 1 : 0);
                return h;
            }
        }

        public bool MatchesDefaults()
        {
            return enableApparelCare
                && apparelQualityScaling
                && apparelCraftingBonus
                && !protectCorpseApparel
                && !enableWeaponCare
                && allowThroneAltars
                && wearAnyGender
                && hideCryptosleep
                && meleeHunting
                && !unarmedHunting
                && Approx(meleeHuntMaxBodySize, 1.5f)
                && enableSharedRooms
                && skipDisturbedSleepWhenSharing
                && enableLeaveAWayOut;
        }

        public bool MatchesSoft()
        {
            return enableApparelCare
                && !apparelQualityScaling
                && apparelCraftingBonus
                && protectCorpseApparel
                && enableWeaponCare
                && allowThroneAltars
                && wearAnyGender
                && hideCryptosleep
                && meleeHunting
                && unarmedHunting
                && Approx(meleeHuntMaxBodySize, 8f)
                && enableSharedRooms
                && skipDisturbedSleepWhenSharing
                && enableLeaveAWayOut;
        }

        public bool MatchesHard()
        {
            return enableApparelCare
                && apparelQualityScaling
                && !apparelCraftingBonus
                && !protectCorpseApparel
                && !enableWeaponCare
                && allowThroneAltars
                && wearAnyGender
                && hideCryptosleep
                && meleeHunting
                && !unarmedHunting
                && Approx(meleeHuntMaxBodySize, 0.8f)
                && enableSharedRooms
                && skipDisturbedSleepWhenSharing
                && enableLeaveAWayOut;
        }

        /// <summary>Soft / Default / Hard / Custom for the options label (AZR-293).</summary>
        public string ActivePresetLabelKey()
        {
            Clamp();
            if (MatchesSoft())
            {
                return "Niceties_Settings_PresetSoft";
            }

            if (MatchesDefaults())
            {
                return "Niceties_Settings_PresetDefault";
            }

            if (MatchesHard())
            {
                return "Niceties_Settings_PresetHard";
            }

            return "Niceties_Settings_PresetCustom";
        }

        private static bool Approx(float a, float b)
        {
            float d = a - b;
            return d < 0.001f && d > -0.001f;
        }
    }
}

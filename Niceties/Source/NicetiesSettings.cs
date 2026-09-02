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

        public bool allowThroneAltars = true;

        public bool wearAnyGender = true;

        public bool hideCryptosleep = true;

        public bool meleeHunting = true;
        public bool unarmedHunting = false;
        public float meleeHuntMaxBodySize = 1.5f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableApparelCare, "enableApparelCare", true);
            Scribe_Values.Look(ref apparelQualityScaling, "apparelQualityScaling", true);
            Scribe_Values.Look(ref apparelCraftingBonus, "apparelCraftingBonus", true);
            Scribe_Values.Look(ref protectCorpseApparel, "protectCorpseApparel", false);
            Scribe_Values.Look(ref allowThroneAltars, "allowThroneAltars", true);
            Scribe_Values.Look(ref wearAnyGender, "wearAnyGender", true);
            Scribe_Values.Look(ref hideCryptosleep, "hideCryptosleep", true);
            Scribe_Values.Look(ref meleeHunting, "meleeHunting", true);
            Scribe_Values.Look(ref unarmedHunting, "unarmedHunting", false);
            Scribe_Values.Look(ref meleeHuntMaxBodySize, "meleeHuntMaxBodySize", 1.5f);
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

        public void ApplySoft()
        {
            ResetToDefaults();
            apparelQualityScaling = false;
            apparelCraftingBonus = true;
            protectCorpseApparel = true;
            unarmedHunting = true;
            meleeHuntMaxBodySize = 8f;
        }

        public void ResetToDefaults()
        {
            enableApparelCare = true;
            apparelQualityScaling = true;
            apparelCraftingBonus = true;
            protectCorpseApparel = false;
            allowThroneAltars = true;
            wearAnyGender = true;
            hideCryptosleep = true;
            meleeHunting = true;
            unarmedHunting = false;
            meleeHuntMaxBodySize = 1.5f;
        }

        public void ApplyHard()
        {
            ResetToDefaults();
            apparelCraftingBonus = false;
            protectCorpseApparel = false;
            unarmedHunting = false;
            meleeHuntMaxBodySize = 0.8f;
        }
    }
}

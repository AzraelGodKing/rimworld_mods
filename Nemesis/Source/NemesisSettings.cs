using Verse;

namespace Nemesis
{
    public enum NemesisDifficultyPreset
    {
        Custom,
        Subtle,
        Classic,
        Relentless,
    }

    public class NemesisSettings : ModSettings
    {
        public NemesisDifficultyPreset difficultyPreset = NemesisDifficultyPreset.Classic;

        public float killedAllyChance = 0.15f;
        public float prisonerEscapedChance = 0.10f;
        public float slaveEscapedChance = 0.12f;
        public float fixationChance = 0.10f;
        public float woundedEscapeChance = 0.12f;
        public int truceDurationDays = 30;
        public int maxEscapes = 4;

        public float maxAggressionCap = 0.6f;
        public float escalationRatePerDay = 0.06f;
        public int minActionCooldownTicks = 90000;

        public float actionWeightTaunt = 0.35f;
        public float actionWeightRaid = 0.15f;
        public float actionWeightAssault = 0.15f;
        public float actionWeightWaste = 0.08f;
        public float actionWeightFakeSignal = 0.10f;
        public float actionWeightCaravan = 0.07f;
        public float actionWeightSabotage = 0.05f;
        public float actionWeightFood = 0.05f;
        public float actionWeightKidnap = 0.08f;
        public float actionWeightSniper = 0.04f;
        public float actionWeightGrave = 0.05f;
        public float actionWeightFoodTamper = 0.06f;
        public float actionWeightInformant = 0.05f;
        public float actionWeightTrophy = 0.08f;
        public float actionWeightIntel = 0.07f;
        public float actionWeightBurrow = 0.04f;
        public float actionWeightAnomaly = 0.06f;

        /// <summary>When false, KidnapAttempt weight is forced to 0 in PickAction.</summary>
        public bool kidnapEnabled = true;

        /// <summary>When false, SniperTerror weight is forced to 0 in PickAction.</summary>
        public bool sniperEnabled = true;

        /// <summary>Chance a non-nemesis hostile raid gets a "I sold your layout" credit letter.</summary>
        public float raidCreditChance = 0.15f;

        /// <summary>Consecutive ignored actions before the "you're not answering" escalation.</summary>
        public int ignoredActionsThreshold = 3;

        /// <summary>Chance a foreign raid gets a "nobody kills you but me" cameo (Obsessed+).</summary>
        public float rivalCameoChance = 0.12f;

        /// <summary>Enable hunt-anniversary taunts every 60 days.</summary>
        public bool anniversaryEnabled = true;

        /// <summary>Nightmares on fixation target during Obsessed+.</summary>
        public bool nightmaresEnabled = true;

        /// <summary>Agency-safe mole event while nemesis is kept prisoner.</summary>
        public bool moleEnabled = true;

        /// <summary>Low chance to start a mole window after KeepPrisoner.</summary>
        public float moleChance = 0.08f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref difficultyPreset, "difficultyPreset", NemesisDifficultyPreset.Classic);
            Scribe_Values.Look(ref killedAllyChance, "killedAllyChance", 0.15f);
            Scribe_Values.Look(ref prisonerEscapedChance, "prisonerEscapedChance", 0.10f);
            Scribe_Values.Look(ref slaveEscapedChance, "slaveEscapedChance", 0.12f);
            Scribe_Values.Look(ref fixationChance, "fixationChance", 0.10f);
            Scribe_Values.Look(ref woundedEscapeChance, "woundedEscapeChance", 0.12f);
            Scribe_Values.Look(ref truceDurationDays, "truceDurationDays", 30);
            Scribe_Values.Look(ref maxEscapes, "maxEscapes", 4);
            Scribe_Values.Look(ref maxAggressionCap, "maxAggressionCap", 0.6f);
            Scribe_Values.Look(ref escalationRatePerDay, "escalationRatePerDay", 0.06f);
            Scribe_Values.Look(ref minActionCooldownTicks, "minActionCooldownTicks", 90000);
            Scribe_Values.Look(ref actionWeightTaunt, "actionWeightTaunt", 0.35f);
            Scribe_Values.Look(ref actionWeightRaid, "actionWeightRaid", 0.15f);
            Scribe_Values.Look(ref actionWeightAssault, "actionWeightAssault", 0.15f);
            Scribe_Values.Look(ref actionWeightWaste, "actionWeightWaste", 0.08f);
            Scribe_Values.Look(ref actionWeightFakeSignal, "actionWeightFakeSignal", 0.10f);
            Scribe_Values.Look(ref actionWeightCaravan, "actionWeightCaravan", 0.07f);
            Scribe_Values.Look(ref actionWeightSabotage, "actionWeightSabotage", 0.05f);
            Scribe_Values.Look(ref actionWeightFood, "actionWeightFood", 0.05f);
            Scribe_Values.Look(ref actionWeightKidnap, "actionWeightKidnap", 0.08f);
            Scribe_Values.Look(ref actionWeightSniper, "actionWeightSniper", 0.04f);
            Scribe_Values.Look(ref actionWeightGrave, "actionWeightGrave", 0.05f);
            Scribe_Values.Look(ref actionWeightFoodTamper, "actionWeightFoodTamper", 0.06f);
            Scribe_Values.Look(ref actionWeightInformant, "actionWeightInformant", 0.05f);
            Scribe_Values.Look(ref actionWeightTrophy, "actionWeightTrophy", 0.08f);
            Scribe_Values.Look(ref actionWeightIntel, "actionWeightIntel", 0.07f);
            Scribe_Values.Look(ref actionWeightBurrow, "actionWeightBurrow", 0.04f);
            Scribe_Values.Look(ref actionWeightAnomaly, "actionWeightAnomaly", 0.06f);
            Scribe_Values.Look(ref kidnapEnabled, "kidnapEnabled", true);
            Scribe_Values.Look(ref sniperEnabled, "sniperEnabled", true);
            Scribe_Values.Look(ref raidCreditChance, "raidCreditChance", 0.15f);
            Scribe_Values.Look(ref ignoredActionsThreshold, "ignoredActionsThreshold", 3);
            Scribe_Values.Look(ref rivalCameoChance, "rivalCameoChance", 0.12f);
            Scribe_Values.Look(ref anniversaryEnabled, "anniversaryEnabled", true);
            Scribe_Values.Look(ref nightmaresEnabled, "nightmaresEnabled", true);
            Scribe_Values.Look(ref moleEnabled, "moleEnabled", true);
            Scribe_Values.Look(ref moleChance, "moleChance", 0.08f);
        }

        public void ResetToDefaults()
        {
            ApplyPreset(NemesisDifficultyPreset.Classic);
        }

        public void ApplyPreset(NemesisDifficultyPreset preset)
        {
            difficultyPreset = preset;
            // Shared baseline, then diverge.
            killedAllyChance = 0.15f;
            prisonerEscapedChance = 0.10f;
            slaveEscapedChance = 0.12f;
            fixationChance = 0.10f;
            woundedEscapeChance = 0.12f;
            truceDurationDays = 30;
            maxEscapes = 4;
            maxAggressionCap = 0.6f;
            escalationRatePerDay = 0.06f;
            minActionCooldownTicks = 90000;
            actionWeightTaunt = 0.35f;
            actionWeightRaid = 0.15f;
            actionWeightAssault = 0.15f;
            actionWeightWaste = 0.08f;
            actionWeightFakeSignal = 0.10f;
            actionWeightCaravan = 0.07f;
            actionWeightSabotage = 0.05f;
            actionWeightFood = 0.05f;
            actionWeightKidnap = 0.08f;
            actionWeightSniper = 0.04f;
            actionWeightGrave = 0.05f;
            actionWeightFoodTamper = 0.06f;
            actionWeightInformant = 0.05f;
            // Trophy / intel / burrow / anomaly baselines are shared across all presets.
            actionWeightTrophy = 0.08f;
            actionWeightIntel = 0.07f;
            actionWeightBurrow = 0.04f;
            actionWeightAnomaly = 0.06f;
            kidnapEnabled = true;
            sniperEnabled = true;
            raidCreditChance = 0.15f;
            ignoredActionsThreshold = 3;
            rivalCameoChance = 0.12f;
            anniversaryEnabled = true;
            nightmaresEnabled = true;
            moleEnabled = true;
            moleChance = 0.08f;

            if (preset == NemesisDifficultyPreset.Subtle)
            {
                killedAllyChance = 0.08f;
                prisonerEscapedChance = 0.05f;
                slaveEscapedChance = 0.06f;
                fixationChance = 0.05f;
                woundedEscapeChance = 0.06f;
                maxAggressionCap = 0.45f;
                escalationRatePerDay = 0.03f;
                minActionCooldownTicks = 120000;
                actionWeightTaunt = 0.50f;
                actionWeightRaid = 0.08f;
                actionWeightAssault = 0.06f;
                actionWeightKidnap = 0.04f;
                actionWeightSniper = 0.06f;
                raidCreditChance = 0.08f;
                ignoredActionsThreshold = 4;
            }
            else if (preset == NemesisDifficultyPreset.Relentless)
            {
                killedAllyChance = 0.25f;
                prisonerEscapedChance = 0.18f;
                slaveEscapedChance = 0.20f;
                fixationChance = 0.18f;
                woundedEscapeChance = 0.20f;
                maxEscapes = 5;
                maxAggressionCap = 0.85f;
                escalationRatePerDay = 0.10f;
                minActionCooldownTicks = 60000;
                actionWeightTaunt = 0.20f;
                actionWeightRaid = 0.22f;
                actionWeightAssault = 0.22f;
                actionWeightKidnap = 0.12f;
                actionWeightSabotage = 0.10f;
                actionWeightFoodTamper = 0.10f;
                raidCreditChance = 0.25f;
                ignoredActionsThreshold = 2;
            }
            // Classic / Custom: baseline already applied.
            if (preset == NemesisDifficultyPreset.Custom)
                difficultyPreset = NemesisDifficultyPreset.Custom;
        }

        public void MarkCustom()
        {
            difficultyPreset = NemesisDifficultyPreset.Custom;
        }
    }
}

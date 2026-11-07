#nullable enable
using Verse;

namespace Nemesis
{
    public class NemesisSettings : ModSettings
    {
        public float killedAllyChance = 0.15f;
        public float prisonerEscapedChance = 0.10f;
        public float slaveEscapedChance = 0.12f;
        public float fixationChance = 0.10f;
        public float woundedEscapeChance = 0.12f;
        public int truceDurationDays = 30;
        public int maxEscapes = 4;
        /// <summary>Hunts that may run at once, at most one per hostile faction (AZR-537).</summary>
        public int maxConcurrentHunts = 2;
        /// <summary>Escapes before each further escape costs colony mood (AZR-707).</summary>
        public int escapeMoodThreshold = 2;
        /// <summary>Health fraction at which a cornered nemesis is subdued instead of fleeing (AZR-707).</summary>
        public float subdueHealth = 0.45f;
        /// <summary>Aggression taken off when a truce ends (AZR-661).</summary>
        public float truceAggressionDiscount = 2f;
        /// <summary>Days after a hunt starts before another may start (AZR-572).</summary>
        public float huntCooldownDays = 3f;

        public float maxAggressionCap = 0.6f;
        public float escalationRatePerDay = 0.06f;
        public int minActionCooldownTicks = 90000;

        public float actionWeightTaunt = 0.35f;
        public float actionWeightIdle = 0.22f;
        public float actionWeightRaid = 0.15f;
        public float actionWeightAssault = 0.15f;
        public float actionWeightWaste = 0.08f;
        public float actionWeightFakeSignal = 0.10f;
        public float actionWeightCaravan = 0.07f;
        public float actionWeightSabotage = 0.05f;
        public float actionWeightFood = 0.05f;
        public float actionWeightAnomaly = 0.06f;
        public float actionWeightKidnap = 0.05f;

        // Hybrid captain progression
        public bool enableCaptainProgression = true;
        public int maxProgressionLevel = 8;
        public float postEscapeSabotageWeightMul = 0.35f;
        public bool enableSoftMounts = true;
        public bool enableSoftMechs = true;
        public bool enableShuttleDrops = true;
        public bool enableNemesisVisuals = true;

        // Sister mods (AZR-556)
        public bool livingWorldCollapseEscalates = true;
        public bool enableIonStormBaiting = true;

        public bool enableInformants = true;
        public int informantLeadCost = 180;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref killedAllyChance, "killedAllyChance", 0.15f);
            Scribe_Values.Look(ref prisonerEscapedChance, "prisonerEscapedChance", 0.10f);
            Scribe_Values.Look(ref slaveEscapedChance, "slaveEscapedChance", 0.12f);
            Scribe_Values.Look(ref fixationChance, "fixationChance", 0.10f);
            Scribe_Values.Look(ref woundedEscapeChance, "woundedEscapeChance", 0.12f);
            Scribe_Values.Look(ref truceDurationDays, "truceDurationDays", 30);
            Scribe_Values.Look(ref maxEscapes, "maxEscapes", 4);
            Scribe_Values.Look(ref maxConcurrentHunts, "maxConcurrentHunts", 2);
            Scribe_Values.Look(ref escapeMoodThreshold, "escapeMoodThreshold", 2);
            Scribe_Values.Look(ref subdueHealth, "subdueHealth", 0.45f);
            Scribe_Values.Look(ref truceAggressionDiscount, "truceAggressionDiscount", 2f);
            Scribe_Values.Look(ref huntCooldownDays, "huntCooldownDays", 3f);
            Scribe_Values.Look(ref maxAggressionCap, "maxAggressionCap", 0.6f);
            Scribe_Values.Look(ref escalationRatePerDay, "escalationRatePerDay", 0.06f);
            Scribe_Values.Look(ref minActionCooldownTicks, "minActionCooldownTicks", 90000);
            Scribe_Values.Look(ref actionWeightTaunt, "actionWeightTaunt", 0.35f);
            Scribe_Values.Look(ref actionWeightIdle, "actionWeightIdle", 0.22f);
            Scribe_Values.Look(ref actionWeightRaid, "actionWeightRaid", 0.15f);
            Scribe_Values.Look(ref actionWeightAssault, "actionWeightAssault", 0.15f);
            Scribe_Values.Look(ref actionWeightWaste, "actionWeightWaste", 0.08f);
            Scribe_Values.Look(ref actionWeightFakeSignal, "actionWeightFakeSignal", 0.10f);
            Scribe_Values.Look(ref actionWeightCaravan, "actionWeightCaravan", 0.07f);
            Scribe_Values.Look(ref actionWeightSabotage, "actionWeightSabotage", 0.05f);
            Scribe_Values.Look(ref actionWeightFood, "actionWeightFood", 0.05f);
            Scribe_Values.Look(ref actionWeightAnomaly, "actionWeightAnomaly", 0.06f);
            Scribe_Values.Look(ref actionWeightKidnap, "actionWeightKidnap", 0.05f);
            Scribe_Values.Look(ref enableCaptainProgression, "enableCaptainProgression", true);
            Scribe_Values.Look(ref maxProgressionLevel, "maxProgressionLevel", 8);
            Scribe_Values.Look(ref postEscapeSabotageWeightMul, "postEscapeSabotageWeightMul", 0.35f);
            Scribe_Values.Look(ref enableSoftMounts, "enableSoftMounts", true);
            Scribe_Values.Look(ref enableSoftMechs, "enableSoftMechs", true);
            Scribe_Values.Look(ref enableShuttleDrops, "enableShuttleDrops", true);
            Scribe_Values.Look(ref enableNemesisVisuals, "enableNemesisVisuals", true);
            Scribe_Values.Look(ref livingWorldCollapseEscalates, "livingWorldCollapseEscalates", true);
            Scribe_Values.Look(ref enableIonStormBaiting, "enableIonStormBaiting", true);
            Scribe_Values.Look(ref enableInformants, "enableInformants", true);
            Scribe_Values.Look(ref informantLeadCost, "informantLeadCost", 180);
        }

        /// <summary>
        /// Relentless and Slow-burn presets (AZR-572): trigger odds, hunt caps, pacing and the
        /// threat-heavy action weights. Everything else keeps its current value. Default is
        /// <see cref="ResetToDefaults"/>.
        /// </summary>
        public void ApplyPreset(bool relentless)
        {
            ResetToDefaults();
            float odds = relentless ? 1.6f : 0.5f;
            killedAllyChance *= odds;
            prisonerEscapedChance *= odds;
            slaveEscapedChance *= odds;
            fixationChance *= odds;
            woundedEscapeChance *= odds;
            if (relentless)
            {
                maxConcurrentHunts = 3;
                huntCooldownDays = 1.5f;
                truceDurationDays = 15;
                maxAggressionCap = 0.8f;
                escalationRatePerDay = 0.10f;
                minActionCooldownTicks = 60000;
                actionWeightIdle = 0.12f;
                actionWeightRaid = 0.20f;
                actionWeightAssault = 0.22f;
                actionWeightKidnap = 0.08f;
            }
            else
            {
                maxConcurrentHunts = 1;
                huntCooldownDays = 8f;
                truceDurationDays = 45;
                maxAggressionCap = 0.45f;
                escalationRatePerDay = 0.03f;
                minActionCooldownTicks = 100000;
                actionWeightIdle = 0.35f;
                actionWeightRaid = 0.10f;
                actionWeightAssault = 0.10f;
                actionWeightKidnap = 0.03f;
            }
        }

        public void ResetToDefaults()
        {
            killedAllyChance = 0.15f;
            prisonerEscapedChance = 0.10f;
            slaveEscapedChance = 0.12f;
            fixationChance = 0.10f;
            woundedEscapeChance = 0.12f;
            truceDurationDays = 30;
            maxEscapes = 4;
            maxConcurrentHunts = 2;
            escapeMoodThreshold = 2;
            subdueHealth = 0.45f;
            truceAggressionDiscount = 2f;
            huntCooldownDays = 3f;
            maxAggressionCap = 0.6f;
            escalationRatePerDay = 0.06f;
            minActionCooldownTicks = 90000;
            actionWeightTaunt = 0.35f;
            actionWeightIdle = 0.22f;
            actionWeightRaid = 0.15f;
            actionWeightAssault = 0.15f;
            actionWeightWaste = 0.08f;
            actionWeightFakeSignal = 0.10f;
            actionWeightCaravan = 0.07f;
            actionWeightSabotage = 0.05f;
            actionWeightFood = 0.05f;
            actionWeightAnomaly = 0.06f;
            actionWeightKidnap = 0.05f;
            enableCaptainProgression = true;
            maxProgressionLevel = 8;
            postEscapeSabotageWeightMul = 0.35f;
            enableSoftMounts = true;
            enableSoftMechs = true;
            enableShuttleDrops = true;
            enableNemesisVisuals = true;
            livingWorldCollapseEscalates = true;
            enableIonStormBaiting = true;
            enableInformants = true;
            informantLeadCost = 180;
        }
    }
}

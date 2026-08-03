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

        // Hybrid captain progression
        public bool enableCaptainProgression = true;
        public int maxProgressionLevel = 8;
        public float postEscapeSabotageWeightMul = 0.35f;
        public bool enableSoftMounts = true;
        public bool enableSoftMechs = true;

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
            Scribe_Values.Look(ref enableCaptainProgression, "enableCaptainProgression", true);
            Scribe_Values.Look(ref maxProgressionLevel, "maxProgressionLevel", 8);
            Scribe_Values.Look(ref postEscapeSabotageWeightMul, "postEscapeSabotageWeightMul", 0.35f);
            Scribe_Values.Look(ref enableSoftMounts, "enableSoftMounts", true);
            Scribe_Values.Look(ref enableSoftMechs, "enableSoftMechs", true);
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
            enableCaptainProgression = true;
            maxProgressionLevel = 8;
            postEscapeSabotageWeightMul = 0.35f;
            enableSoftMounts = true;
            enableSoftMechs = true;
        }
    }
}

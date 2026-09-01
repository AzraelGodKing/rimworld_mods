using System.Collections.Generic;
using Verse;

namespace DeepColony
{
    public class DeepColonySettings : ModSettings
    {
        public enum Preset
        {
            Soft = 0,
            Default = 1,
            Hard = 2
        }

        public bool enablePerks = true;
        public bool showPerkHediffs = false;
        public bool announceVisitorPerkUnlocks = false;
        public bool enableTrauma = true;
        public bool enableMentoring = true;
        public bool enableInheritance = true;
        public bool enableFactionRep = true;

        public float combatShockChance = 0.40f;
        public int minSkillLead = 3;
        public float passiveMentorMultiplier = 1.25f;
        public float activeMentorMultiplier = 1.40f;
        public float allyDriftMtbDays = 5f;
        public float enemyDriftMtbDays = 4f;
        public int massacreDeathThreshold = 3;
        public float traitInheritChance = 0.35f;
        public float therapyHealScale = 1f;
        public bool enableTraumaPenalties = false;
        public bool enableAttitudeConsequences = false;

        // Phase 5 — power systems (mostly default off)
        public bool enableSkill20Capstones = true;
        public bool enableBranchingPerks = false;
        public bool enablePerkRespec = true;
        public float respecCooldownDays = 15f;
        public bool enableCrossSkillArchetypes = false;
        public bool enableRecruitPrePerks = false;
        public bool enableHeirlooms = false;
        public bool enableChronicTrauma = false;

        // Batch C
        public float childRaidWitnessChance = 0.55f;
        public bool enablePrisonerCounsel = false;
        public bool enableEnvoyVisits = false;
        public bool enableApologyTribute = true;

        // Family join / ex-lover reconcile (D17 / D18)
        public bool enableFamilyJoin = true;
        public float familyRaidDefectChance = 0.28f;
        public float familyVisitJoinChance = 0.18f;
        public bool enableExLoverReconcile = true;
        public float exLoverReconcileMtbDays = 8f;
        public int familyUnwaveringMinOpinion = 20;
        public float familyUnwaveringBreakChance = 0.55f;

        // Family tree display (not part of Soft/Default/Hard)
        public bool familyTreePedigreeStyle = false;

        // Fail-open Despicable 2 / RimPacts (not part of Soft/Default/Hard)
        public bool enableDiplomacyCompat = true;

        // F01 touch-averse (not a Hard-only power system)
        public bool enableTouchAverse = true;
        public float touchComfortDays = 4f;
        public float touchComfortThreshold = 0.65f;

        // AZR-65 Quiet Hours (not Hard-only)
        public bool enableQuietHours = true;
        public float quietHoursIntensity = 1f;

        // AZR-73 mentor retraining (off on Soft/Default)
        public bool enablePerkRetrain = false;

        // AZR-70 estate / wills
        public bool enableEstate = true;

        public static DeepColonySettings Get =>
            DeepColonyMod.Settings ?? new DeepColonySettings();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enablePerks, "enablePerks", true);
            Scribe_Values.Look(ref showPerkHediffs, "showPerkHediffs", false);
            Scribe_Values.Look(ref announceVisitorPerkUnlocks, "announceVisitorPerkUnlocks", false);
            Scribe_Values.Look(ref enableTrauma, "enableTrauma", true);
            Scribe_Values.Look(ref enableMentoring, "enableMentoring", true);
            Scribe_Values.Look(ref enableInheritance, "enableInheritance", true);
            Scribe_Values.Look(ref enableFactionRep, "enableFactionRep", true);
            Scribe_Values.Look(ref combatShockChance, "combatShockChance", 0.40f);
            Scribe_Values.Look(ref minSkillLead, "minSkillLead", 3);
            Scribe_Values.Look(ref passiveMentorMultiplier, "passiveMentorMultiplier", 1.25f);
            Scribe_Values.Look(ref activeMentorMultiplier, "activeMentorMultiplier", 1.40f);
            Scribe_Values.Look(ref allyDriftMtbDays, "allyDriftMtbDays", 5f);
            Scribe_Values.Look(ref enemyDriftMtbDays, "enemyDriftMtbDays", 4f);
            Scribe_Values.Look(ref massacreDeathThreshold, "massacreDeathThreshold", 3);
            Scribe_Values.Look(ref traitInheritChance, "traitInheritChance", 0.35f);
            Scribe_Values.Look(ref therapyHealScale, "therapyHealScale", 1f);
            Scribe_Values.Look(ref enableTraumaPenalties, "enableTraumaPenalties", false);
            Scribe_Values.Look(ref enableAttitudeConsequences, "enableAttitudeConsequences", false);
            Scribe_Values.Look(ref enableSkill20Capstones, "enableSkill20Capstones", true);
            Scribe_Values.Look(ref enableBranchingPerks, "enableBranchingPerks", false);
            Scribe_Values.Look(ref enablePerkRespec, "enablePerkRespec", true);
            Scribe_Values.Look(ref respecCooldownDays, "respecCooldownDays", 15f);
            Scribe_Values.Look(ref enableCrossSkillArchetypes, "enableCrossSkillArchetypes", false);
            Scribe_Values.Look(ref enableRecruitPrePerks, "enableRecruitPrePerks", false);
            Scribe_Values.Look(ref enableHeirlooms, "enableHeirlooms", false);
            Scribe_Values.Look(ref enableChronicTrauma, "enableChronicTrauma", false);
            Scribe_Values.Look(ref childRaidWitnessChance, "childRaidWitnessChance", 0.55f);
            Scribe_Values.Look(ref enablePrisonerCounsel, "enablePrisonerCounsel", false);
            Scribe_Values.Look(ref enableEnvoyVisits, "enableEnvoyVisits", false);
            Scribe_Values.Look(ref enableApologyTribute, "enableApologyTribute", true);
            Scribe_Values.Look(ref enableFamilyJoin, "enableFamilyJoin", true);
            Scribe_Values.Look(ref familyRaidDefectChance, "familyRaidDefectChance", 0.28f);
            Scribe_Values.Look(ref familyVisitJoinChance, "familyVisitJoinChance", 0.18f);
            Scribe_Values.Look(ref enableExLoverReconcile, "enableExLoverReconcile", true);
            Scribe_Values.Look(ref exLoverReconcileMtbDays, "exLoverReconcileMtbDays", 8f);
            Scribe_Values.Look(ref familyUnwaveringMinOpinion, "familyUnwaveringMinOpinion", 20);
            Scribe_Values.Look(ref familyUnwaveringBreakChance, "familyUnwaveringBreakChance", 0.55f);
            Scribe_Values.Look(ref familyTreePedigreeStyle, "familyTreePedigreeStyle", false);
            Scribe_Values.Look(ref enableDiplomacyCompat, "enableDiplomacyCompat", true);
            Scribe_Values.Look(ref enableTouchAverse, "enableTouchAverse", true);
            Scribe_Values.Look(ref touchComfortDays, "touchComfortDays", 4f);
            Scribe_Values.Look(ref touchComfortThreshold, "touchComfortThreshold", 0.65f);
            Scribe_Values.Look(ref enableQuietHours, "enableQuietHours", true);
            Scribe_Values.Look(ref quietHoursIntensity, "quietHoursIntensity", 1f);
            Scribe_Values.Look(ref enablePerkRetrain, "enablePerkRetrain", false);
            Scribe_Values.Look(ref enableEstate, "enableEstate", true);
        }

        public void ApplyPreset(Preset preset)
        {
            enablePerks = true;
            enableTrauma = true;
            enableMentoring = true;
            enableInheritance = true;
            enableFactionRep = true;
            enableTraumaPenalties = false;
            enableAttitudeConsequences = false;
            enableSkill20Capstones = true;
            enableBranchingPerks = false;
            enablePerkRespec = true;
            respecCooldownDays = 15f;
            enableCrossSkillArchetypes = false;
            enableRecruitPrePerks = false;
            enableHeirlooms = false;
            enableChronicTrauma = false;
            childRaidWitnessChance = 0.55f;
            enablePrisonerCounsel = false;
            enableEnvoyVisits = false;
            enableApologyTribute = true;
            enableFamilyJoin = true;
            familyRaidDefectChance = 0.28f;
            familyVisitJoinChance = 0.18f;
            enableExLoverReconcile = true;
            exLoverReconcileMtbDays = 8f;
            familyUnwaveringMinOpinion = 20;
            familyUnwaveringBreakChance = 0.55f;
            enableTouchAverse = true;
            touchComfortDays = 4f;
            touchComfortThreshold = 0.65f;
            enableQuietHours = true;
            quietHoursIntensity = 1f;
            enablePerkRetrain = false;
            enableEstate = true;

            switch (preset)
            {
                case Preset.Soft:
                    combatShockChance = 0.20f;
                    minSkillLead = 2;
                    passiveMentorMultiplier = 1.35f;
                    activeMentorMultiplier = 1.55f;
                    allyDriftMtbDays = 8f;
                    enemyDriftMtbDays = 6f;
                    massacreDeathThreshold = 4;
                    traitInheritChance = 0.45f;
                    therapyHealScale = 1.35f;
                    respecCooldownDays = 10f;
                    familyRaidDefectChance = 0.40f;
                    familyVisitJoinChance = 0.28f;
                    exLoverReconcileMtbDays = 12f;
                    familyUnwaveringMinOpinion = 10;
                    familyUnwaveringBreakChance = 0.75f;
                    touchComfortDays = 2.5f;
                    touchComfortThreshold = 0.55f;
                    quietHoursIntensity = 0.6f;
                    enablePerkRetrain = false;
                    break;
                case Preset.Hard:
                    combatShockChance = 0.55f;
                    minSkillLead = 4;
                    passiveMentorMultiplier = 1.15f;
                    activeMentorMultiplier = 1.25f;
                    allyDriftMtbDays = 3f;
                    enemyDriftMtbDays = 2.5f;
                    massacreDeathThreshold = 2;
                    traitInheritChance = 0.22f;
                    therapyHealScale = 0.75f;
                    enableTraumaPenalties = true;
                    enableAttitudeConsequences = true;
                    enableSkill20Capstones = true;
                    enableBranchingPerks = true;
                    enableCrossSkillArchetypes = true;
                    enableHeirlooms = true;
                    enableChronicTrauma = true;
                    respecCooldownDays = 20f;
                    childRaidWitnessChance = 0.70f;
                    enableEnvoyVisits = true;
                    familyRaidDefectChance = 0.18f;
                    familyVisitJoinChance = 0.10f;
                    exLoverReconcileMtbDays = 5f;
                    familyUnwaveringMinOpinion = 40;
                    familyUnwaveringBreakChance = 0.35f;
                    touchComfortDays = 6.5f;
                    touchComfortThreshold = 0.75f;
                    quietHoursIntensity = 1.35f;
                    enablePerkRetrain = true;
                    break;
                default:
                    ResetToDefaults();
                    break;
            }
        }

        public void ResetToDefaults()
        {
            enablePerks = true;
            showPerkHediffs = false;
            announceVisitorPerkUnlocks = false;
            enableTrauma = true;
            enableMentoring = true;
            enableInheritance = true;
            enableFactionRep = true;
            combatShockChance = 0.40f;
            minSkillLead = 3;
            passiveMentorMultiplier = 1.25f;
            activeMentorMultiplier = 1.40f;
            allyDriftMtbDays = 5f;
            enemyDriftMtbDays = 4f;
            massacreDeathThreshold = 3;
            traitInheritChance = 0.35f;
            therapyHealScale = 1f;
            enableTraumaPenalties = false;
            enableAttitudeConsequences = false;
            enableSkill20Capstones = true;
            enableBranchingPerks = false;
            enablePerkRespec = true;
            respecCooldownDays = 15f;
            enableCrossSkillArchetypes = false;
            enableRecruitPrePerks = false;
            enableHeirlooms = false;
            enableChronicTrauma = false;
            childRaidWitnessChance = 0.55f;
            enablePrisonerCounsel = false;
            enableEnvoyVisits = false;
            enableApologyTribute = true;
            enableFamilyJoin = true;
            familyRaidDefectChance = 0.28f;
            familyVisitJoinChance = 0.18f;
            enableExLoverReconcile = true;
            exLoverReconcileMtbDays = 8f;
            familyUnwaveringMinOpinion = 20;
            familyUnwaveringBreakChance = 0.55f;
            familyTreePedigreeStyle = false;
            enableDiplomacyCompat = true;
            enableTouchAverse = true;
            touchComfortDays = 4f;
            touchComfortThreshold = 0.65f;
            enableQuietHours = true;
            quietHoursIntensity = 1f;
            enablePerkRetrain = false;
            enableEstate = true;
        }
    }
}

using RimWorld;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public enum NemesisTargetMode
    {
        Colony,
        Pawn
    }

    public enum NemesisTrigger
    {
        KilledAlly,
        WoundedAndEscaped,
        PrisonerEscaped,
        SlaveEscaped,
        FactionRetaliation,
        Fixation,
        TrophyHunt,
    }

    public enum NemesisAction
    {
        CommsTaunt,
        DirectRaid,
        NemesisAssault,
        WastePackDrop,
        FakeSignalAmbush,
        CaravanHarass,
        PowerSabotage,
        FoodStoreRaid,
        AnomalyBait,
    }

    public enum NemesisOutcome
    {
        Execute,
        Release,
        KeepPrisoner,
        Truce
    }

    public enum NemesisEndReason
    {
        Captured,
        Killed,
        TargetDied,
        TargetHandedOver,
        Cleared
    }

    /// <summary>Captain combat specialty — rolled once at hunt create; steers gear/skills/escorts.</summary>
    public enum NemesisCombatFocus
    {
        Destroyer,
        Berserker,
        Sniper,
        Psycho,
        Survivor,
        Mechanitor,
    }

    /// <summary>
    /// Persistent hunt state. Foundation by Dredd (Misakabob); fields extended for new actions / end reasons.
    /// </summary>
    public class NemesisData : IExposable
    {
        public bool active;
        public int nemesisPawnId = -1;
        public string nemesisName;
        public string factionName;
        public Faction faction;
        public NemesisTargetMode targetMode;
        public NemesisTrigger trigger;
        public int targetPawnId = -1;
        public string targetPawnName;
        public float aggressionLevel = 1f;
        public int nextActionTick;
        public int escapeCount;
        public int lastEscapeTick;
        public int truceUntilTick = -1;
        public bool rogue;
        public bool corneredAnnounced;
        public int lastActionKind = -1;
        public int harassmentCount;
        public bool pendingFakeAmbush;
        public int fakeAmbushTick = -1;

        /// <summary>Captain tier — rises on each escape (capped by settings).</summary>
        public int progressionLevel;
        /// <summary>Last level fully applied to the pawn (idempotent Apply).</summary>
        public int appliedProgressionLevel = -1;
        public NemesisCombatFocus combatFocus = NemesisCombatFocus.Survivor;
        /// <summary>Soft Giddy-Up mount animal kind defName, if assigned.</summary>
        public string mountKindDefName;

        public float EffectiveAggression
        {
            get
            {
                float fraction = NemesisMod.Settings?.maxAggressionCap ?? 0.6f;
                float capValue = Mathf.Lerp(1f, 10f, fraction);
                return Mathf.Min(aggressionLevel, capValue);
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref active, "active", false);
            Scribe_Values.Look(ref nemesisPawnId, "nemesisPawnId", -1);
            Scribe_Values.Look(ref nemesisName, "nemesisName", null);
            Scribe_Values.Look(ref factionName, "factionName", null);
            Scribe_References.Look(ref faction, "nemesisFaction");
            Scribe_Values.Look(ref targetMode, "targetMode", NemesisTargetMode.Colony);
            Scribe_Values.Look(ref trigger, "trigger", NemesisTrigger.KilledAlly);
            Scribe_Values.Look(ref targetPawnId, "targetPawnId", -1);
            Scribe_Values.Look(ref targetPawnName, "targetPawnName", null);
            Scribe_Values.Look(ref aggressionLevel, "aggressionLevel", 1f);
            Scribe_Values.Look(ref nextActionTick, "nextActionTick", 0);
            Scribe_Values.Look(ref escapeCount, "escapeCount", 0);
            Scribe_Values.Look(ref lastEscapeTick, "lastEscapeTick", 0);
            Scribe_Values.Look(ref truceUntilTick, "truceUntilTick", -1);
            Scribe_Values.Look(ref rogue, "rogue", false);
            Scribe_Values.Look(ref corneredAnnounced, "corneredAnnounced", false);
            Scribe_Values.Look(ref lastActionKind, "lastActionKind", -1);
            Scribe_Values.Look(ref harassmentCount, "harassmentCount", 0);
            Scribe_Values.Look(ref pendingFakeAmbush, "pendingFakeAmbush", false);
            Scribe_Values.Look(ref fakeAmbushTick, "fakeAmbushTick", -1);
            Scribe_Values.Look(ref progressionLevel, "progressionLevel", 0);
            Scribe_Values.Look(ref appliedProgressionLevel, "appliedProgressionLevel", -1);
            Scribe_Values.Look(ref combatFocus, "combatFocus", NemesisCombatFocus.Survivor);
            Scribe_Values.Look(ref mountKindDefName, "mountKindDefName", null);
        }

        public string FocusLabelKey => combatFocus switch
        {
            NemesisCombatFocus.Destroyer => "Nemesis_Focus_Destroyer",
            NemesisCombatFocus.Berserker => "Nemesis_Focus_Berserker",
            NemesisCombatFocus.Sniper => "Nemesis_Focus_Sniper",
            NemesisCombatFocus.Psycho => "Nemesis_Focus_Psycho",
            NemesisCombatFocus.Survivor => "Nemesis_Focus_Survivor",
            NemesisCombatFocus.Mechanitor => "Nemesis_Focus_Mechanitor",
            _ => "Nemesis_Focus_Survivor",
        };
    }
}

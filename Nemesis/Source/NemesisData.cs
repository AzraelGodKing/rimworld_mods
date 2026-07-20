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
        }
    }
}

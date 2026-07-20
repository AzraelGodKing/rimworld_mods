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
        // Append-only: new actions below so scribed lastActionKind ints stay valid.
        KidnapAttempt,
        SniperTerror,
        GraveDesecration,
        FoodTampering,
        InformantReveal,
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

    /// <summary>Named hunt phases mapped from aggression (thresholds match prior float gates).</summary>
    public enum NemesisHuntPhase
    {
        Watching,   // under 2
        Testing,    // 2+ (sabotage)
        Obsessed,   // 3+ (assault / kidnap)
        Reckoning,  // 5+ or escape cap / finale
    }

    public class NemesisTrophyEntry : IExposable
    {
        public string nemesisName;
        public string factionName;
        public NemesisTrigger trigger;
        public NemesisEndReason endReason;
        public int startTick;
        public int endTick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref nemesisName, "nemesisName", null);
            Scribe_Values.Look(ref factionName, "factionName", null);
            Scribe_Values.Look(ref trigger, "trigger", NemesisTrigger.KilledAlly);
            Scribe_Values.Look(ref endReason, "endReason", NemesisEndReason.Cleared);
            Scribe_Values.Look(ref startTick, "startTick", 0);
            Scribe_Values.Look(ref endTick, "endTick", 0);
        }
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

        /// <summary>TicksGame when the current hunt started. 0 = legacy save (treat as unknown).</summary>
        public int huntStartTick;

        /// <summary>Consecutive actions with no player engagement against the nemesis pawn.</summary>
        public int ignoredActionsCount;

        /// <summary>True if a colonist damaged/arrested the nemesis since the last harassment action.</summary>
        public bool engagedSinceLastAction;

        // --- SniperTerror runtime (despawn via FireEscape-like pattern) ---
        public bool sniperActive;
        public int sniperUntilTick = -1;
        public int sniperShotsLeft;

        // --- FoodTampering (scribed stack id + windows) ---
        public int taintedFoodThingId = -1;
        public int taintedUntilTick = -1;
        public int taintedRevealTick = -1;
        public bool taintedRevealed;

        // --- InformantReveal timing (set when traders/visitors leave) ---
        public int lastVisitorLeaveTick = -1;

        // --- Deliberate silence window ---
        public int silenceUntilTick = -1;
        public int silenceLetterTick = -1;
        public bool silenceLetterSent;

        // --- Staged finale ---
        public bool finaleOffered;
        public bool finaleDuelActive;

        public float EffectiveAggression
        {
            get
            {
                float fraction = NemesisMod.Settings?.maxAggressionCap ?? 0.6f;
                float capValue = Mathf.Lerp(1f, 10f, fraction);
                return Mathf.Min(aggressionLevel, capValue);
            }
        }

        /// <summary>
        /// Watching under 2, Testing 2+, Obsessed 3+ (assault), Reckoning 5+ or escape cap.
        /// </summary>
        public NemesisHuntPhase Phase
        {
            get
            {
                float a = EffectiveAggression;
                int maxEsc = NemesisMod.Settings?.maxEscapes ?? 4;
                if (finaleOffered || finaleDuelActive || escapeCount >= maxEsc || a >= 5f)
                    return NemesisHuntPhase.Reckoning;
                if (a >= 3f) return NemesisHuntPhase.Obsessed;
                if (a >= 2f) return NemesisHuntPhase.Testing;
                return NemesisHuntPhase.Watching;
            }
        }

        public string PhaseLabelKeyed()
        {
            return Phase switch
            {
                NemesisHuntPhase.Watching => "Nemesis_Phase_Watching".Translate(),
                NemesisHuntPhase.Testing => "Nemesis_Phase_Testing".Translate(),
                NemesisHuntPhase.Obsessed => "Nemesis_Phase_Obsessed".Translate(),
                _ => "Nemesis_Phase_Reckoning".Translate(),
            };
        }

        public int HuntDays
        {
            get
            {
                if (huntStartTick <= 0) return harassmentCount; // rough legacy fallback
                return Mathf.Max(0, (Find.TickManager.TicksGame - huntStartTick) / 60000);
            }
        }

        public void NotifyPlayerEngagedNemesis()
        {
            ignoredActionsCount = 0;
            engagedSinceLastAction = true;
        }

        public void ScheduleSilence(int normalInterval)
        {
            int quiet = (int)(normalInterval * Rand.Range(1.5f, 2f));
            silenceUntilTick = Find.TickManager.TicksGame + quiet;
            silenceLetterTick = Find.TickManager.TicksGame + quiet / 2;
            silenceLetterSent = false;
            nextActionTick = Mathf.Max(nextActionTick, silenceUntilTick);
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
            Scribe_Values.Look(ref huntStartTick, "huntStartTick", 0);
            Scribe_Values.Look(ref ignoredActionsCount, "ignoredActionsCount", 0);
            Scribe_Values.Look(ref engagedSinceLastAction, "engagedSinceLastAction", false);
            Scribe_Values.Look(ref sniperActive, "sniperActive", false);
            Scribe_Values.Look(ref sniperUntilTick, "sniperUntilTick", -1);
            Scribe_Values.Look(ref sniperShotsLeft, "sniperShotsLeft", 0);
            Scribe_Values.Look(ref taintedFoodThingId, "taintedFoodThingId", -1);
            Scribe_Values.Look(ref taintedUntilTick, "taintedUntilTick", -1);
            Scribe_Values.Look(ref taintedRevealTick, "taintedRevealTick", -1);
            Scribe_Values.Look(ref taintedRevealed, "taintedRevealed", false);
            Scribe_Values.Look(ref lastVisitorLeaveTick, "lastVisitorLeaveTick", -1);
            Scribe_Values.Look(ref silenceUntilTick, "silenceUntilTick", -1);
            Scribe_Values.Look(ref silenceLetterTick, "silenceLetterTick", -1);
            Scribe_Values.Look(ref silenceLetterSent, "silenceLetterSent", false);
            Scribe_Values.Look(ref finaleOffered, "finaleOffered", false);
            Scribe_Values.Look(ref finaleDuelActive, "finaleDuelActive", false);
        }
    }
}

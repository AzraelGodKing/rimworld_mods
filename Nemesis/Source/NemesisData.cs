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
        StrataBurrow, // SoftCompat only — never fires if Strata probe fails
        // Append-only
        TrophyTheft,
        CampIntel,
    }

    public enum NemesisOutcome
    {
        Execute,
        Release,
        KeepPrisoner,
        Truce,
        // Append-only
        Recruit,
        Ransom,
        HandToEnemies,
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

    /// <summary>Personality rolled at hunt creation. Append-only; default Stalker for old saves.</summary>
    public enum NemesisArchetype
    {
        Stalker,
        Butcher,
        Saboteur,
        Trickster,
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

        // --- Living truce pacing ---
        public int nextTruceEventTick = -1;

        // --- TargetDied grave visit (robbed nemesis) ---
        public bool graveVisitPending;
        public int graveVisitTick = -1;
        public bool graveVisitDone;

        // --- Personality (Phase 1) ---
        public NemesisArchetype archetype = NemesisArchetype.Stalker;

        // --- Rival cameo (Phase 2) — dedicated bools; do NOT reuse lastActionKind=-2 ---
        public bool rivalCameoActive;
        public int rivalCameoUntilTick = -1;
        /// <summary>Faction before cameo SetFaction; restored on cameo end / hunt end. Null = legacy / no change.</summary>
        public Faction rivalCameoOriginalFaction;

        // --- Physical evidence (Phase 3) ---
        public int stolenTrophyThingId = -1;
        public string stolenTrophyDefName;
        public string stolenTrophyLabel;
        public int stolenTrophyQuality = -1;

        // --- Timing (Phase 4) ---
        public int lastAnniversaryDay = -1;
        public int nextNightmareCheckTick = -1;

        // --- Captivity arc (Phase 5) ---
        public bool captivityActive;
        public int captivityStartTick = -1;
        public int nextJailbreakCheckTick = -1;
        public bool moleLetterSent;
        public int moleColonistId = -1;
        public int moleSabotageTick = -1;

        // --- Endings (Phase 6) ---
        public bool targetKilledByNemesis;
        public bool endgameFinaleScheduled;
        public bool royaltyDuelFormalized;
        public bool ideologyRelicStolen;

        // --- Hunt base / intel ---
        public int intelLevel;
        public int lastKnownTile = -1;
        public int campWorldObjectId = -1;
        public bool campIsReal;
        public bool campIsTrap;
        public bool campResolved;
        public int nextCaravanTrackTick = -1;

        // --- Identity tint (RGB; -1 = unset) ---
        public float tintR = -1f;
        public float tintG = -1f;
        public float tintB = -1f;
        public bool socialSeeded;

        // --- Balance: suppress storyteller raids briefly after nemesis threat ---
        public int lastNemesisThreatTick = -1;

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

        public string ArchetypeLabelKeyed()
        {
            return archetype switch
            {
                NemesisArchetype.Butcher => "Nemesis_Archetype_Butcher".Translate(),
                NemesisArchetype.Saboteur => "Nemesis_Archetype_Saboteur".Translate(),
                NemesisArchetype.Trickster => "Nemesis_Archetype_Trickster".Translate(),
                _ => "Nemesis_Archetype_Stalker".Translate(),
            };
        }

        /// <summary>Roll a personality at hunt creation. Equal weight; fail-open Stalker.</summary>
        public static NemesisArchetype RollArchetype()
        {
            int roll = Rand.RangeInclusive(0, 3);
            if (roll == 1) return NemesisArchetype.Butcher;
            if (roll == 2) return NemesisArchetype.Saboteur;
            if (roll == 3) return NemesisArchetype.Trickster;
            return NemesisArchetype.Stalker;
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
            Scribe_Values.Look(ref nextTruceEventTick, "nextTruceEventTick", -1);
            Scribe_Values.Look(ref graveVisitPending, "graveVisitPending", false);
            Scribe_Values.Look(ref graveVisitTick, "graveVisitTick", -1);
            Scribe_Values.Look(ref graveVisitDone, "graveVisitDone", false);
            Scribe_Values.Look(ref archetype, "archetype", NemesisArchetype.Stalker);
            Scribe_Values.Look(ref rivalCameoActive, "rivalCameoActive", false);
            Scribe_Values.Look(ref rivalCameoUntilTick, "rivalCameoUntilTick", -1);
            Scribe_References.Look(ref rivalCameoOriginalFaction, "rivalCameoOriginalFaction");
            Scribe_Values.Look(ref stolenTrophyThingId, "stolenTrophyThingId", -1);
            Scribe_Values.Look(ref stolenTrophyDefName, "stolenTrophyDefName", null);
            Scribe_Values.Look(ref stolenTrophyLabel, "stolenTrophyLabel", null);
            Scribe_Values.Look(ref stolenTrophyQuality, "stolenTrophyQuality", -1);
            Scribe_Values.Look(ref lastAnniversaryDay, "lastAnniversaryDay", -1);
            Scribe_Values.Look(ref nextNightmareCheckTick, "nextNightmareCheckTick", -1);
            Scribe_Values.Look(ref captivityActive, "captivityActive", false);
            Scribe_Values.Look(ref captivityStartTick, "captivityStartTick", -1);
            Scribe_Values.Look(ref nextJailbreakCheckTick, "nextJailbreakCheckTick", -1);
            Scribe_Values.Look(ref moleLetterSent, "moleLetterSent", false);
            Scribe_Values.Look(ref moleColonistId, "moleColonistId", -1);
            Scribe_Values.Look(ref moleSabotageTick, "moleSabotageTick", -1);
            Scribe_Values.Look(ref targetKilledByNemesis, "targetKilledByNemesis", false);
            Scribe_Values.Look(ref endgameFinaleScheduled, "endgameFinaleScheduled", false);
            Scribe_Values.Look(ref royaltyDuelFormalized, "royaltyDuelFormalized", false);
            Scribe_Values.Look(ref ideologyRelicStolen, "ideologyRelicStolen", false);
            Scribe_Values.Look(ref intelLevel, "intelLevel", 0);
            Scribe_Values.Look(ref lastKnownTile, "lastKnownTile", -1);
            Scribe_Values.Look(ref campWorldObjectId, "campWorldObjectId", -1);
            Scribe_Values.Look(ref campIsReal, "campIsReal", false);
            Scribe_Values.Look(ref campIsTrap, "campIsTrap", false);
            Scribe_Values.Look(ref campResolved, "campResolved", false);
            Scribe_Values.Look(ref nextCaravanTrackTick, "nextCaravanTrackTick", -1);
            Scribe_Values.Look(ref tintR, "tintR", -1f);
            Scribe_Values.Look(ref tintG, "tintG", -1f);
            Scribe_Values.Look(ref tintB, "tintB", -1f);
            Scribe_Values.Look(ref socialSeeded, "socialSeeded", false);
            Scribe_Values.Look(ref lastNemesisThreatTick, "lastNemesisThreatTick", -1);
        }
    }
}

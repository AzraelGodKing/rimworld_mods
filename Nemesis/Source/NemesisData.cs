#nullable enable
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
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
        DoNothing,
        DirectRaid,
        NemesisAssault,
        WastePackDrop,
        FakeSignalAmbush,
        CaravanHarass,
        PowerSabotage,
        FoodStoreRaid,
        AnomalyBait,
        KidnapAttempt,
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
        Cleared,
        Vanished
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
        /// <summary>Unique per save; commands, the dossier and the raid inject address hunts by it.</summary>
        public int huntId = -1;
        public bool active;
        public int nemesisPawnId = -1;
        /// <summary>Saved reference next to the ID; the ID stays the source of truth.</summary>
        public Pawn? nemesisPawn;
        public Pawn? targetPawn;
        public string? nemesisName;
        public string? factionName;
        public Faction? faction;
        public NemesisTargetMode targetMode;
        public NemesisTrigger trigger;
        public int targetPawnId = -1;
        public string? targetPawnName;
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
        /// <summary>Capture resolution dialog awaiting player choice; survives save/reload.</summary>
        public bool pendingResolution;

        /// <summary>Captain tier — rises on each escape (capped by settings).</summary>
        public int progressionLevel;
        /// <summary>Last level fully applied to the pawn (idempotent Apply).</summary>
        public int appliedProgressionLevel = -1;
        public NemesisCombatFocus combatFocus = NemesisCombatFocus.Survivor;
        /// <summary>Soft Giddy-Up mount animal kind defName, if assigned.</summary>
        public string? mountKindDefName;

        public bool tellsRolled;
        public NemesisVoice voice = NemesisVoice.Cold;
        public NemesisMark mark = NemesisMark.CallingCard;
        public NemesisHabit habit = NemesisHabit.PowerFirst;
        public string? weaponDefName;
        public int huntStartTick;
        public PlanetTile lastKnownTile = PlanetTile.Invalid;
        public string? lastKnownTileLabel;

        // Hunt camp from a bought lead (AZR-555). Real camps hold the nemesis; false ones don't.
        public Site? campSite;
        public bool campIsReal;
        public bool campVisited;

        /// <summary>Last tick an ion storm pulled this hunt's next move forward (AZR-556).</summary>
        public int lastIonBaitTick = -GenDate.TicksPerDay;
        /// <summary>Silver the player posted against this hunt (AZR-657).</summary>
        public int bountySilver;
        public string? lastGearSeen;
        public List<NemesisNote> notes = new List<NemesisNote>();

        // Lookup caches; not saved.
        [Unsaved] public Pawn? cachedNemesis;
        [Unsaved] public Pawn? cachedTarget;

        // Tick of the last full pawn scan that found nobody, so a missing pawn costs at most
        // one scan of every map and world pawn per MissRescanTicks (AZR-535).
        [Unsaved] public int nemesisMissTick = int.MinValue / 2;
        [Unsaved] public int targetMissTick = int.MinValue / 2;

        /// <summary>Active, in truce, or waiting on the capture dialog.</summary>
        public bool Engaged => active || truceUntilTick > 0 || pendingResolution;

        public void ClearCaches()
        {
            cachedNemesis = null;
            cachedTarget = null;
            nemesisMissTick = targetMissTick = int.MinValue / 2;
        }

        /// <summary>Name in the hunt faction's colour, for letter titles.</summary>
        public string ColoredName
        {
            get
            {
                string n = nemesisName ?? "Nemesis_Phrase_Someone".Translate();
                return faction != null ? n.Colorize(faction.Color) : n;
            }
        }

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
            Scribe_Values.Look(ref huntId, "huntId", -1);
            Scribe_Values.Look(ref active, "active", false);
            Scribe_Values.Look(ref nemesisPawnId, "nemesisPawnId", -1);
            Scribe_References.Look(ref nemesisPawn, "nemesisPawn");
            Scribe_References.Look(ref targetPawn, "targetPawn");
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
            Scribe_Values.Look(ref pendingResolution, "pendingResolution", false);
            Scribe_Values.Look(ref progressionLevel, "progressionLevel", 0);
            Scribe_Values.Look(ref appliedProgressionLevel, "appliedProgressionLevel", -1);
            Scribe_Values.Look(ref combatFocus, "combatFocus", NemesisCombatFocus.Survivor);
            Scribe_Values.Look(ref mountKindDefName, "mountKindDefName", null);
            Scribe_Values.Look(ref tellsRolled, "tellsRolled", false);
            Scribe_Values.Look(ref voice, "voice", NemesisVoice.Cold);
            Scribe_Values.Look(ref mark, "mark", NemesisMark.CallingCard);
            Scribe_Values.Look(ref habit, "habit", NemesisHabit.PowerFirst);
            Scribe_Values.Look(ref weaponDefName, "weaponDefName", null);
            Scribe_Values.Look(ref huntStartTick, "huntStartTick", 0);
            Scribe_Values.Look(ref lastKnownTile, "lastKnownPlanetTile", PlanetTile.Invalid);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // 1.x saved a bare int, which drops the planet layer; it was always the surface.
                int legacyTile = -1;
                Scribe_Values.Look(ref legacyTile, "lastKnownTile", -1);
                if (!lastKnownTile.Valid && legacyTile >= 0)
                    lastKnownTile = new PlanetTile(legacyTile);
            }
            Scribe_Values.Look(ref lastKnownTileLabel, "lastKnownTileLabel", null);
            Scribe_References.Look(ref campSite, "campSite");
            Scribe_Values.Look(ref campIsReal, "campIsReal");
            Scribe_Values.Look(ref campVisited, "campVisited");
            Scribe_Values.Look(ref lastIonBaitTick, "lastIonBaitTick", -GenDate.TicksPerDay);
            Scribe_Values.Look(ref lastGearSeen, "lastGearSeen", null);
            Scribe_Values.Look(ref bountySilver, "bountySilver", 0);
            Scribe_Collections.Look(ref notes, "notes", LookMode.Deep);
            if (notes == null)
                notes = new List<NemesisNote>();
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

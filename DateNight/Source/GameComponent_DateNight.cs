#nullable enable
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DateNight
{
    public class GameComponent_DateNight : GameComponent
    {
        /// <summary>
        /// Save layout version (AZR-539). 0 = a 1.x save (key missing), 2 = 2.0.0.
        /// Bump only when a saved key changes meaning; new keys need no bump.
        /// Saved type names, Scribe keys and the DateActivity enum are a contract:
        /// rename nothing, append enum values only.
        /// </summary>
        public const int CurrentSaveVersion = 2;

        private int saveVersion = CurrentSaveVersion;
        private Dictionary<int, bool> wasOnLovin = new Dictionary<int, bool>();
        private Dictionary<int, bool> wasOnDate = new Dictionary<int, bool>();
        private string? lastNewsVersion;

        public GameComponent_DateNight(Game game)
        {
            // Persisted statics must not leak thing IDs across colonies/sessions.
            // A new game never runs ExposeData, so reset everything here (AZR-522).
            ResetAllStaticState();
        }

        public override void FinalizeInit()
        {
            // Reload: ExposeData already restored scribed collections. Only clear
            // ephemeral windows; Claims were rebuilt from the save.
            DateNightUtility.ClearEphemeral();
            // Seed flags only for pawns the save did not know, so a schedule change
            // across the save is still seen as a transition (AZR-523, AZR-539).
            SeedScheduleFlags();
            UpdateNewsLetter.TrySend(ref lastNewsVersion);
        }

        private static void ResetAllStaticState()
        {
            DateNightPawns.Reset();
            DateNightUtility.ClearEphemeral();
            DateNightWindows.Reset();
            DateNightDateUtility.Reset();
            DateNightAnniversaries.Reset();
            DateNightVenues.Reset();
            DateNightDoubleDates.Reset();
            DateNightReunions.Reset();
            DateNightRomance.Reset();
            DateNightHistory.Reset();
            DateNightAskOut.Reset();
        }

        private void SeedScheduleFlags()
        {
            List<Pawn> colonists = DateNightPawns.Colonists;
            if (colonists == null)
            {
                return;
            }

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn == null)
                {
                    continue;
                }
                int id = pawn.thingIDNumber;
                if (!wasOnLovin.ContainsKey(id))
                {
                    wasOnLovin[id] = DateNightUtility.IsLovinSchedule(pawn);
                }
                if (!wasOnDate.ContainsKey(id))
                {
                    wasOnDate[id] = DateNightUtility.IsDateSchedule(pawn);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Missing key = a save from before 2.0.0.
                saveVersion = 0;
            }
            Scribe_Values.Look(ref saveVersion, "dateNightSaveVersion", 0);
            Scribe_Values.Look(ref lastNewsVersion, "lastNewsVersion");
            Scribe_Collections.Look(ref wasOnLovin, "dateNightWasOnLovin", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref wasOnDate, "dateNightWasOnDate", LookMode.Value, LookMode.Value);
            DateNightWindows.ExposeData();
            DateNightDateUtility.ExposeData();
            DateNightAnniversaries.ExposeData();
            DateNightVenues.ExposeData();
            DateNightDoubleDates.ExposeData();
            DateNightReunions.ExposeData();
            DateNightRomance.ExposeData();
            DateNightHistory.ExposeData();
            DateNightAskOut.ExposeData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                wasOnLovin ??= new Dictionary<int, bool>();
                wasOnDate ??= new Dictionary<int, bool>();
                if (saveVersion < CurrentSaveVersion)
                {
                    Log.Message("[DateNight] Upgraded a save from Date Night "
                        + (saveVersion == 0 ? "1.x" : saveVersion.ToString()) + " to save version "
                        + CurrentSaveVersion + ".");
                    saveVersion = CurrentSaveVersion;
                }
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 60 != 0)
            {
                return;
            }

            List<Pawn> colonists = DateNightPawns.Colonists;
            if (colonists == null)
            {
                return;
            }

            for (int i = 0; i < colonists.Count; i++)
            {
                TickPawn(colonists[i]);
            }

            DateNightAnniversaries.Tick();
            DateNightDoubleDates.Tick();
            DateNightAskOut.Tick();

            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                DateNightDateUtility.PruneDeadPawns();
                DateNightVenues.PruneDeadPawns();
                DateNightVenues.TickDestroyedVenues();
                DateNightVenues.TickBreakups();
                DateNightDoubleDates.PruneDeadPawns();
                DateNightReunions.PruneDeadPawns();
                DateNightReunions.Tick();
                DateNightAnniversaries.PruneDeadPawns();
                DateNightRomance.PruneDeadPawns();
                DateNightRomance.TickBonds();
                DateNightHistory.PruneDeadPawns();
                DateNightHistory.TickBabyPlans();
                PruneScheduleFlags(wasOnLovin);
                PruneScheduleFlags(wasOnDate);
            }
        }

        private static readonly List<int> pruneScratch = new List<int>();

        private static void PruneScheduleFlags(Dictionary<int, bool> flags)
        {
            pruneScratch.Clear();
            foreach (int id in flags.Keys)
            {
                if (!DateNightDateUtility.PawnStillAlive(id))
                {
                    pruneScratch.Add(id);
                }
            }
            for (int i = 0; i < pruneScratch.Count; i++)
            {
                flags.Remove(pruneScratch[i]);
            }
        }

        private void TickPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            bool onLovin = DateNightUtility.IsLovinSchedule(pawn);
            bool onDate = DateNightUtility.IsDateSchedule(pawn);
            int id = pawn.thingIDNumber;
            wasOnLovin.TryGetValue(id, out bool wasLovin);
            wasOnDate.TryGetValue(id, out bool wasDate);

            if (onLovin && !wasLovin)
            {
                DateNightUtility.NotifyEnteredLovinSchedule(pawn);
            }
            else if (!onLovin && wasLovin)
            {
                DateNightUtility.NotifyLeftLovinSchedule(pawn);
                DateNightWindows.NotifyLeftSharedSchedule(pawn);
            }

            if (!onDate && wasDate)
            {
                DateNightWindows.NotifyLeftSharedSchedule(pawn);
            }

            wasOnLovin[id] = onLovin;
            wasOnDate[id] = onDate;

            DateNightWindows.NotifyScheduleTick(pawn, onLovin, onDate);

            if (!pawn.Spawned || pawn.Map == null)
            {
                return;
            }

            if (onLovin)
            {
                DateNightUtility.TickScheduledPawn(pawn);
            }
            if (onDate)
            {
                DateNightDateUtility.TickScheduledDate(pawn);
            }
        }
    }
}

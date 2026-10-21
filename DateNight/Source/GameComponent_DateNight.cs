using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DateNight
{
    public class GameComponent_DateNight : GameComponent
    {
        private readonly Dictionary<int, bool> wasOnLovin = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> wasOnDate = new Dictionary<int, bool>();
        private string lastNewsVersion;

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
            DateNightWindows.ClearWindowsOnly();
            // Seed schedule flags without firing enter/leave transitions (AZR-523).
            SeedScheduleFlags();
            UpdateNewsLetter.TrySend(ref lastNewsVersion);
        }

        private static void ResetAllStaticState()
        {
            DateNightUtility.ClearEphemeral();
            DateNightWindows.Reset();
            DateNightDateUtility.Reset();
            DateNightAnniversaries.Reset();
            DateNightVenues.Reset();
            DateNightDoubleDates.Reset();
            DateNightReunions.Reset();
            DateNightRomance.Reset();
        }

        private void SeedScheduleFlags()
        {
            wasOnLovin.Clear();
            wasOnDate.Clear();
            List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
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
                wasOnLovin[id] = DateNightUtility.IsLovinSchedule(pawn);
                wasOnDate[id] = DateNightUtility.IsDateSchedule(pawn);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastNewsVersion, "lastNewsVersion");
            DateNightWindows.ExposeData();
            DateNightDateUtility.ExposeData();
            DateNightAnniversaries.ExposeData();
            DateNightVenues.ExposeData();
            DateNightDoubleDates.ExposeData();
            DateNightReunions.ExposeData();
            DateNightRomance.ExposeData();
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 60 != 0)
            {
                return;
            }

            List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
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

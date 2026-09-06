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
        }

        public override void FinalizeInit()
        {
            UpdateNewsLetter.TrySend(ref lastNewsVersion);
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

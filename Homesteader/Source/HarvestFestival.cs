using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Once a year, the first autumn day a home map has a harvest maypole, colonists there hold a
    /// harvest festival. Mood scales with how many distinct preserves the pantry holds.
    /// </summary>
    public class GameComponent_HarvestFestival : GameComponent
    {
        private const int CheckIntervalTicks = GenDate.TicksPerHour;
        private const string MaypoleDefName = "Homesteader_HarvestMaypole";

        private Dictionary<int, int> lastFestivalYearByMap = new Dictionary<int, int>();

        public GameComponent_HarvestFestival(Game game)
        {
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % CheckIntervalTicks != 0)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map != null && map.IsPlayerHome && IsFestivalTime(map))
                {
                    TryHold(map);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref lastFestivalYearByMap, "hsLastFestivalYearByMap", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && lastFestivalYearByMap == null)
            {
                lastFestivalYearByMap = new Dictionary<int, int>();
            }
        }

        private static bool IsFestivalTime(Map map)
        {
            Season season = GenLocalDate.Season(map);
            if (season == Season.PermanentSummer || season == Season.PermanentWinter)
            {
                float longitude = Find.WorldGrid.LongLatOf(map.Tile).x;
                return GenDate.Quadrum(Find.TickManager.TicksAbs, longitude) == Quadrum.Septober;
            }

            return season == Season.Fall;
        }

        private void TryHold(Map map)
        {
            int year = GenLocalDate.Year(map);
            if (lastFestivalYearByMap.TryGetValue(map.uniqueID, out int last) && last >= year)
            {
                return;
            }

            ThingDef maypoleDef = DefDatabase<ThingDef>.GetNamedSilentFail(MaypoleDefName);
            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_HarvestFestival");
            if (maypoleDef == null || thought == null)
            {
                return;
            }

            List<Thing> maypoles = map.listerThings.ThingsOfDef(maypoleDef);
            Thing maypole = null;
            for (int i = 0; i < maypoles.Count; i++)
            {
                if (maypoles[i].Faction == Faction.OfPlayer)
                {
                    maypole = maypoles[i];
                    break;
                }
            }

            if (maypole == null)
            {
                return;
            }

            lastFestivalYearByMap[map.uniqueID] = year;

            PantryUtility.Invalidate();
            int kinds = PantryUtility.Snapshot().preserveKinds;
            int stage = kinds >= 6 ? 2 : kinds >= 3 ? 1 : 0;

            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                colonists[i].needs?.mood?.thoughts?.memories?.TryGainMemory(
                    ThoughtMaker.MakeThought(thought, stage));
            }

            Find.LetterStack.ReceiveLetter(
                "Homesteader_HarvestFestivalLetterLabel".Translate(),
                "Homesteader_HarvestFestivalLetterText".Translate(kinds, thought.stages[stage].label),
                LetterDefOf.PositiveEvent,
                new LookTargets(maypole));
        }
    }
}

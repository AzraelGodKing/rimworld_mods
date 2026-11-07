#nullable enable
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
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                PruneLostMaps();
            }

            Scribe_Collections.Look(ref lastFestivalYearByMap, "hsLastFestivalYearByMap", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && lastFestivalYearByMap == null)
            {
                lastFestivalYearByMap = new Dictionary<int, int>();
            }
        }

        /// <summary>Abandoned or destroyed colonies drop out of the saved list (AZR-680).</summary>
        private void PruneLostMaps()
        {
            if (lastFestivalYearByMap == null || lastFestivalYearByMap.Count == 0 || Find.Maps == null)
            {
                return;
            }

            var live = new HashSet<int>();
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                live.Add(Find.Maps[i].uniqueID);
            }

            List<int>? drop = null;
            foreach (int id in lastFestivalYearByMap.Keys)
            {
                if (!live.Contains(id))
                {
                    (drop ??= new List<int>()).Add(id);
                }
            }

            if (drop != null)
            {
                for (int i = 0; i < drop.Count; i++)
                {
                    lastFestivalYearByMap.Remove(drop[i]);
                }
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

            ThingDef? maypoleDef = DefDatabase<ThingDef>.GetNamedSilentFail(MaypoleDefName);
            ThoughtDef? thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_HarvestFestival");
            if (maypoleDef == null || thought == null)
            {
                return;
            }

            List<Thing> maypoles = map.listerThings.ThingsOfDef(maypoleDef);
            Thing? maypole = null;
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

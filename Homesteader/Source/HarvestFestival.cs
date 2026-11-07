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
            int now = Find.TickManager.TicksGame;
            if (now % GenTicks.TickRareInterval == 0)
            {
                RefreshPantries();
            }

            if (now % CheckIntervalTicks != 0)
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

        /// <summary>Per-map pantry for the larder mood, only when that mood is on.</summary>
        private static void RefreshPantries()
        {
            if (!HomesteaderGameplay.Active.larderMood)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].IsPlayerHome)
                {
                    PantryUtility.RefreshMap(maps[i]);
                }
            }
        }

        internal static int StageFor(int preserveKinds, bool seasonalFood)
        {
            int stage = preserveKinds >= 6 ? 2 : preserveKinds >= 3 ? 1 : 0;
            return seasonalFood && stage < 2 ? stage + 1 : stage;
        }

        /// <summary>Seasonal food (HS-A01): pumpkin pie or cider on the shelves lifts the festival a step.</summary>
        private static bool HasSeasonalFood(PantryReport pantry)
        {
            for (int i = 0; i < pantry.kinds.Count; i++)
            {
                string? defName = pantry.kinds[i].def?.defName;
                if (defName == "Homesteader_PumpkinPie" || defName == "Homesteader_Cider")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Trade pull (HS-A01): word of the festival brings a trader caravan one to
        /// three days later, if one can reach the colony by then.
        /// </summary>
        private static bool QueueFestivalTrader(Map map)
        {
            IncidentDef def = IncidentDefOf.TraderCaravanArrival;
            if (def == null)
            {
                return false;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            parms.forced = true;
            int fireTick = Find.TickManager.TicksGame + Rand.RangeInclusive(GenDate.TicksPerDay, 3 * GenDate.TicksPerDay);
            return Find.Storyteller.incidentQueue.Add(def, fireTick, parms, retryDurationTicks: GenDate.TicksPerDay);
        }

        private static string LetterText(PantryReport pantry, string stageLabel, bool feast, bool traderQueued)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Homesteader_HarvestFestivalLetterText".Translate(pantry.preserveKinds, stageLabel));
            if (pantry.preserveDefs.Count > 0)
            {
                // Which preserves earned the festival (AZR-682).
                var names = new List<string>(pantry.preserveDefs.Count);
                for (int i = 0; i < pantry.preserveDefs.Count; i++)
                {
                    names.Add(pantry.preserveDefs[i].label);
                }

                sb.Append("\n\n").Append("Homesteader_HarvestFestivalLetterKinds".Translate(names.ToCommaList(useAnd: true)));
            }

            if (feast)
            {
                sb.Append("\n\n").Append("Homesteader_HarvestFestivalLetterFeast".Translate());
            }

            if (traderQueued)
            {
                sb.Append("\n\n").Append("Homesteader_HarvestFestivalLetterTrader".Translate());
            }

            return sb.ToString();
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

            // This map's own pantry only, counted now (AZR-581).
            PantryReport pantry = PantryUtility.RefreshMap(map);
            int kinds = pantry.preserveKinds;
            bool feast = HasSeasonalFood(pantry);
            int stage = StageFor(kinds, feast);

            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                colonists[i].needs?.mood?.thoughts?.memories?.TryGainMemory(
                    ThoughtMaker.MakeThought(thought, stage));
            }

            bool traderQueued = QueueFestivalTrader(map);

            // The letter's own "Jump to location" button targets the maypole (AZR-697).
            Find.LetterStack.ReceiveLetter(
                "Homesteader_HarvestFestivalLetterLabel".Translate(),
                LetterText(pantry, thought.stages[stage].label, feast, traderQueued),
                LetterDefOf.PositiveEvent,
                new LookTargets(maypole));
        }
    }
}

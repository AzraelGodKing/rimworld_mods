using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Continuous seepage on levels below the water table (AZR-139).
    // Reuses FloodMapComponent; off by default via settings.
    public class MapComponent_WaterSeep : MapComponent
    {
        private const int SeepInterval = 2500;
        private int lastWarnTick = -999999;

        public MapComponent_WaterSeep(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (!map.IsHashIntervalTick(SeepInterval))
            {
                return;
            }
            StrataSettings settings = StrataMod.Settings;
            if (settings == null || !settings.waterTableSeepageEnabled)
            {
                return;
            }
            if (!StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            int below = WorldComponent_StrataStratum.Get?.LevelsBelowTable(map) ?? 0;
            if (below <= 0)
            {
                return;
            }

            FloodMapComponent flood = map.GetComponent<FloodMapComponent>();
            if (flood == null)
            {
                return;
            }

            if (HasActiveSumpCoveringMostOfMap())
            {
                return;
            }

            float rate = Mathf.Clamp(settings.waterTableSeepRate, 0.25f, 2f);
            int cells = Mathf.Clamp(Mathf.RoundToInt(below * 2f * rate), 1, 12);
            for (int i = 0; i < cells; i++)
            {
                if (!CellFinder.TryFindRandomCell(map,
                        c => c.Standable(map) && !c.Fogged(map)
                            && !FloodUtility.IsFlooded(map.terrainGrid.TerrainAt(c)),
                        out IntVec3 cell))
                {
                    break;
                }
                flood.FloodCell(cell);
            }

            if (Find.TickManager.TicksGame - lastWarnTick > 60000
                && flood.AnyFlooded
                && map.mapPawns.FreeColonistsSpawnedCount > 0)
            {
                lastWarnTick = Find.TickManager.TicksGame;
                Find.LetterStack.ReceiveLetter(
                    "Strata_Seep_LetterLabel".Translate(),
                    "Strata_Seep_LetterText".Translate(below),
                    LetterDefOf.NegativeEvent,
                    new TargetInfo(map.Center, map));
            }
        }

        private bool HasActiveSumpCoveringMostOfMap()
        {
            List<Thing> pumps = map.listerThings.ThingsOfDef(
                DefDatabase<ThingDef>.GetNamedSilentFail("Strata_SumpPump"));
            if (pumps == null || pumps.Count == 0)
            {
                return false;
            }
            int active = 0;
            for (int i = 0; i < pumps.Count; i++)
            {
                if (pumps[i].TryGetComp<CompSumpPump>() is CompSumpPump pump && pump.Active)
                {
                    active++;
                }
            }
            // One working pump per ~80 cells of map keeps seepage at bay.
            int needed = Mathf.Max(1, map.Area / 6400);
            return active >= needed;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastWarnTick, "strataSeepWarnTick", -999999);
        }
    }
}

using System.Collections.Generic;
using Verse;

namespace Homesteader
{
    // Tracks map cells chilled by root cellars (the cellar footprint + adjacent cells
    // in the same enclosed room). Spoilables on those cells use the cellar temp cap.
    public class RootCellarCoolingMapComponent : MapComponent
    {
        private readonly HashSet<IntVec3> cooledCells = new HashSet<IntVec3>();

        private int lastRebuildTick = -99999;

        public RootCellarCoolingMapComponent(Map map) : base(map)
        {
        }

        public bool IsCooledCell(IntVec3 cell) => cooledCells.Contains(cell);

        public override void MapComponentTick()
        {
            // Rebuild periodically; cheap vs AmbientTemperature spam scanning rooms.
            if (Find.TickManager.TicksGame - lastRebuildTick < 250)
            {
                return;
            }

            Rebuild();
        }

        public void Rebuild()
        {
            lastRebuildTick = Find.TickManager.TicksGame;
            cooledCells.Clear();

            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < buildings.Count; i++)
            {
                Thing t = buildings[i];
                CompRootCellarCooling comp = t.TryGetComp<CompRootCellarCooling>();
                if (comp == null)
                {
                    continue;
                }

                Room room = t.GetRoom();
                bool indoor = room != null && !room.PsychologicallyOutdoors;

                foreach (IntVec3 cell in t.OccupiedRect())
                {
                    cooledCells.Add(cell);
                    if (!indoor)
                    {
                        continue;
                    }

                    foreach (IntVec3 adj in GenAdj.CellsAdjacent8Way(new TargetInfo(cell, map)))
                    {
                        if (!adj.InBounds(map))
                        {
                            continue;
                        }

                        Room adjRoom = adj.GetRoom(map);
                        if (adjRoom == room)
                        {
                            cooledCells.Add(adj);
                        }
                    }
                }
            }
        }
    }
}

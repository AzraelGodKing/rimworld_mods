using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    [DefOf]
    public static class StrataSmokeDefOf
    {
        public static HediffDef Strata_SmokeInhalation;

        static StrataSmokeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataSmokeDefOf));
        }
    }

    // A lightweight, room-based combustion-smoke simulation. Burners add smoke
    // to the room they sit in; it lingers in enclosed rooms and disperses where
    // the room is open to the sky, has an open roof, an exterior door, a vent,
    // or an unsealed stairwell shaft (smoke rises). Colonists breathing thick
    // smoke take on a worsening inhalation hediff.
    public class SmokeMapComponent : MapComponent, ICellBoolGiver
    {
        private const int CycleTicks = 60;
        private const float BaseLeak = 0.02f;      // slow seepage from any enclosed room
        private const float OpenRoofVent = 0.06f;  // per open-roof cell (capped)
        private const float OutdoorDisperse = 0.6f; // fraction cleared per cycle in open air
        private const float ExteriorDoorDrain = 0.4f; // per open exterior door - a visible flush
        private const float InteriorDoorFlow = 0.25f; // fraction of the density gap that crosses an open interior door
        private const float HarmThreshold = 0.15f;
        // Burners and inflows can never push a properly ventilated room past
        // this light haze, safely under HarmThreshold - ventilation is a
        // guarantee, not a race between the emission rate and the vent rate.
        // Pre-existing thick smoke still drains through the outlets visibly
        // instead of vanishing to the cap.
        private const float VentilatedCap = 0.12f;
        // Tuned so a pawn in 100% smoke reaches "coughing" in roughly an
        // in-game hour and dies only after several - a hazard you can react
        // to, not an instant kill.
        private const float SeverityGain = 0.006f;
        private const float SeverityDecay = 0.03f;
        private const float MoteThreshold = 0.2f;

        public readonly HashSet<CompExhaust> Emitters = new HashSet<CompExhaust>();
        public readonly HashSet<CompExhaustVent> Vents = new HashSet<CompExhaustVent>();
        public readonly HashSet<CompSmokeUpdraft> Updrafts = new HashSet<CompSmokeUpdraft>();

        private struct Cloud
        {
            public float density;
            public IntVec3 sample;
        }

        private readonly Dictionary<int, Cloud> clouds = new Dictionary<int, Cloud>();

        // Per-cell density mirror for the drawn smog overlay (lazy-allocated).
        private float[] cellDensity;
        private CellBoolDrawer drawer;

        public SmokeMapComponent(Map map) : base(map)
        {
        }

        public Color Color => Color.white;

        public bool GetCellBool(int index)
        {
            return cellDensity != null && cellDensity[index] > 0.05f;
        }

        public Color GetCellExtraColor(int index)
        {
            // Black smog that thickens with density.
            return new Color(0.04f, 0.04f, 0.05f, Mathf.Clamp01(cellDensity[index]));
        }

        public float DensityInRoom(Room room)
        {
            return room != null && clouds.TryGetValue(room.ID, out Cloud c) ? c.density : 0f;
        }

        // Dev helpers.
        public string DebugSummary()
        {
            if (clouds.Count == 0)
            {
                return "  (no active smoke)";
            }
            var sb = new System.Text.StringBuilder();
            foreach (KeyValuePair<int, Cloud> kv in clouds)
            {
                sb.AppendLine($"  room {kv.Key}: density {kv.Value.density:F2} @ {kv.Value.sample}");
            }
            return sb.ToString().TrimEnd();
        }

        public void ClearAll()
        {
            clouds.Clear();
            if (cellDensity != null)
            {
                System.Array.Clear(cellDensity, 0, cellDensity.Length);
            }
            drawer?.SetDirty();
        }

        // Dev: instantly fill the room containing a cell with smoke.
        public void DebugSaturate(IntVec3 cell)
        {
            Room room = cell.InBounds(map) ? cell.GetRoom(map) : null;
            if (room == null)
            {
                return;
            }
            clouds[room.ID] = new Cloud { density = 1f, sample = cell };
            RebuildCellDensity();
        }

        public override void MapComponentTick()
        {
            if ((Find.TickManager.TicksGame + map.uniqueID) % CycleTicks != 0)
            {
                return;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.smokeEnabled)
            {
                if (clouds.Count > 0)
                {
                    ClearAll();
                }
                return;
            }

            // 1. One-way wall vents (powered fans and passive louvers).
            ProcessDirectionalVents();

            // 2. Smoke rises through unsealed stairwell / elevator shafts.
            SmokeRiseUtility.ProcessMap(this);

            // 3. Open doors move smoke: exterior doors flush it outside fast
            // and visibly, interior doors spill it into the next room so it
            // spreads toward an exit.
            ProcessDoorFlow();

            // 4. Disperse existing smoke (open roof, slow leak).
            foreach (int id in clouds.Keys.ToList())
            {
                Cloud c = clouds[id];
                Room r = c.sample.IsValid && c.sample.InBounds(map) ? c.sample.GetRoom(map) : null;
                if (r == null || r.UsesOutdoorTemperature)
                {
                    c.density *= 1f - OutdoorDisperse;
                }
                else
                {
                    float vent = BaseLeak + OpenRoofVent * Mathf.Min(r.OpenRoofCount, 5);
                    c.density *= 1f - Mathf.Clamp01(vent);
                }
                if (c.density < 0.01f)
                {
                    clouds.Remove(id);
                }
                else
                {
                    clouds[id] = c;
                }
            }

            // 5. Emit from active burners in enclosed rooms. Ventilation is a
            // guarantee: burners can never push a properly ventilated room
            // past a light haze - but thick pre-existing smoke drains
            // naturally (and visibly) rather than vanishing.
            foreach (CompExhaust emitter in Emitters)
            {
                if (!emitter.parent.Spawned || !emitter.Active)
                {
                    continue;
                }
                Room r = emitter.parent.GetRoom();
                if (r == null || r.UsesOutdoorTemperature)
                {
                    continue; // vents straight to open air
                }
                float add = emitter.Props.emissionPerCycle / Mathf.Max(r.CellCount, 1);
                Cloud c = clouds.TryGetValue(r.ID, out Cloud existing) ? existing : new Cloud();
                float limit = RoomIsProperlyVentilated(r) ? Mathf.Max(VentilatedCap, c.density) : 1f;
                c.density = Mathf.Min(limit, c.density + add);
                c.sample = emitter.parent.Position;
                clouds[r.ID] = c;
            }

            RebuildCellDensity();
            AffectPawns();
            ThrowMotes();
        }

        // Move smoke through open doors. Runs on a snapshot of the smoky
        // rooms; smoke pushed into a fresh room joins the simulation next
        // cycle.
        private void ProcessDoorFlow()
        {
            if (clouds.Count == 0)
            {
                return;
            }
            var countedDoors = new HashSet<Building>();
            foreach (int id in clouds.Keys.ToList())
            {
                if (!clouds.TryGetValue(id, out Cloud c))
                {
                    continue;
                }
                Room room = c.sample.IsValid && c.sample.InBounds(map) ? c.sample.GetRoom(map) : null;
                if (room == null || room.UsesOutdoorTemperature)
                {
                    continue;
                }
                countedDoors.Clear();
                float density = c.density;
                foreach (Region region in room.Regions)
                {
                    Building_Door door = region.door;
                    if (door == null || !door.Open || !countedDoors.Add(door))
                    {
                        continue;
                    }
                    Room neighbor = null;
                    bool exterior = false;
                    foreach (RegionLink link in region.links)
                    {
                        Room other = link.GetOtherRegion(region)?.Room;
                        if (other == null || other == room)
                        {
                            continue;
                        }
                        if (other.PsychologicallyOutdoors || other.UsesOutdoorTemperature)
                        {
                            exterior = true;
                        }
                        else
                        {
                            neighbor = other;
                        }
                    }
                    if (exterior)
                    {
                        // Exits take priority: outdoors is a pure drain.
                        density *= 1f - ExteriorDoorDrain;
                    }
                    else if (neighbor != null && neighbor.CellCount > 0)
                    {
                        // Equalize toward the volume-weighted mix of the two
                        // rooms, conserving smoke mass: what a small room
                        // loses in density, a big hall gains as a thin haze.
                        float cellsHere = Mathf.Max(room.CellCount, 1);
                        float cellsThere = neighbor.CellCount;
                        float there = DensityInRoom(neighbor);
                        float equilibrium = (density * cellsHere + there * cellsThere) / (cellsHere + cellsThere);
                        float drop = (density - equilibrium) * InteriorDoorFlow;
                        if (drop > 0.005f)
                        {
                            density -= drop;
                            AddSmokeToRoom(neighbor, drop * cellsHere / cellsThere, door.Position);
                        }
                    }
                }
                if (density != c.density)
                {
                    if (density < 0.01f)
                    {
                        clouds.Remove(id);
                    }
                    else
                    {
                        c.density = density;
                        clouds[id] = c;
                    }
                }
            }
        }

        // A working smoke outlet: open sky, an open exterior door, a fan or
        // louver whose exhaust side (or duct run) reaches outdoors, or a
        // powered updraft filter beside an unsealed shaft.
        private bool RoomIsProperlyVentilated(Room room)
        {
            if (room.OpenRoofCount > 0 || SmokeVentUtility.RoomHasOpenExteriorDoor(room))
            {
                return true;
            }
            foreach (CompExhaustVent vent in Vents)
            {
                if (!vent.parent.Spawned || !vent.Active || vent.IntakeRoom != room)
                {
                    continue;
                }
                if (SmokeVentUtility.ExhaustOpensIntoDuct(vent.parent, out HashSet<IntVec3> network))
                {
                    if (SmokeVentUtility.DuctNetworkReachesOutdoor(map, network))
                    {
                        return true;
                    }
                    continue;
                }
                Room exhaust = vent.ExhaustRoom;
                if ((exhaust != null && exhaust.UsesOutdoorTemperature)
                    || SmokeVentUtility.CellIsOutdoor(map, SmokeVentUtility.ExhaustCell(vent.parent)))
                {
                    return true;
                }
            }
            foreach (CompSmokeUpdraft updraft in Updrafts)
            {
                if (updraft.parent.Spawned && updraft.Active
                    && updraft.parent.GetRoom() == room
                    && RoomHasOpenShaftUp(room))
                {
                    return true;
                }
            }
            return false;
        }

        private bool RoomHasOpenShaftUp(Room room)
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (!(thing is PocketMapExit exit) || thing is Building_StairsDown)
                {
                    continue;
                }
                if (exit.Position.GetRoom(map) != room)
                {
                    continue;
                }
                if (StrataPortalUtility.IsSealedPortal(exit.entrance ?? (Thing)exit))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        // Draw the smog overlay every frame while any smoke exists.
        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            if (cellDensity == null || clouds.Count == 0)
            {
                return;
            }
            drawer ??= new CellBoolDrawer(this, map.Size.x, map.Size.z, 0.5f);
            drawer.MarkForDraw();
            drawer.CellBoolDrawerUpdate();
        }

        // Percentage readout under the cursor while the smoke toggle is on.
        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (!Patch_SmokeOverlay.ShowReadout || Find.CurrentMap != map)
            {
                return;
            }
            IntVec3 cell = UI.MouseCell();
            if (!cell.InBounds(map))
            {
                return;
            }
            float density = DensityInRoom(cell.GetRoom(map));
            if (density <= 0.001f)
            {
                return;
            }
            Text.Font = GameFont.Small;
            Vector2 mouse = Event.current.mousePosition;
            var rect = new Rect(mouse.x + 12f, mouse.y + 12f, 110f, 24f);
            Widgets.Label(rect, $"Smoke {Mathf.RoundToInt(density * 100f)}%");
        }

        private void ProcessDirectionalVents()
        {
            foreach (CompExhaustVent vent in Vents)
            {
                if (!vent.parent.Spawned || !vent.Active)
                {
                    continue;
                }
                Room intake = vent.IntakeRoom;
                if (intake == null || intake.UsesOutdoorTemperature)
                {
                    continue;
                }
                if (!clouds.TryGetValue(intake.ID, out Cloud cloud))
                {
                    continue;
                }
                float rate = Mathf.Clamp01(vent.Props.ventPower);
                if (rate <= 0f)
                {
                    continue;
                }

                if (SmokeVentUtility.ExhaustOpensIntoDuct(vent.parent, out HashSet<IntVec3> network))
                {
                    if (SmokeVentUtility.DuctNetworkReachesOutdoor(map, network))
                    {
                        cloud.density *= 1f - rate;
                    }
                }
                else
                {
                    Room exhaust = vent.ExhaustRoom;
                    if (exhaust != null && exhaust.UsesOutdoorTemperature)
                    {
                        cloud.density *= 1f - rate;
                    }
                    else if (SmokeVentUtility.CellIsOutdoor(map, SmokeVentUtility.ExhaustCell(vent.parent)))
                    {
                        cloud.density *= 1f - rate;
                    }
                    else if (exhaust != null)
                    {
                        float moved = cloud.density * rate;
                        cloud.density -= moved;
                        AddSmokeToRoom(exhaust, moved, vent.parent.Position);
                    }
                }

                if (cloud.density < 0.01f)
                {
                    clouds.Remove(intake.ID);
                }
                else
                {
                    clouds[intake.ID] = cloud;
                }
            }
        }

        internal void TransferSmokeUp(Room sourceRoom, Room upperRoom, Map upperMap, float rate, IntVec3 sample)
        {
            if (sourceRoom == null || rate <= 0f || !clouds.TryGetValue(sourceRoom.ID, out Cloud cloud))
            {
                return;
            }
            float moved = cloud.density * Mathf.Clamp01(rate);
            cloud.density -= moved;
            if (cloud.density < 0.01f)
            {
                clouds.Remove(sourceRoom.ID);
            }
            else
            {
                clouds[sourceRoom.ID] = cloud;
            }
            if (moved <= 0f)
            {
                return;
            }
            if (upperRoom == null || upperRoom.UsesOutdoorTemperature)
            {
                return;
            }
            SmokeMapComponent upperSmoke = upperMap.GetComponent<SmokeMapComponent>();
            upperSmoke?.AddSmokeToRoom(upperRoom, moved, sample);
        }

        private void AddSmokeToRoom(Room room, float amount, IntVec3 sample)
        {
            if (room == null || amount <= 0f)
            {
                return;
            }
            // The sample cell must sit INSIDE the room: it is how the cloud
            // resolves its room each cycle and where the overlay paints.
            // Callers often pass a door or vent cell, which belongs to its own
            // one-cell room and would make the smoke invisible.
            if (!sample.IsValid || !sample.InBounds(map) || sample.GetRoom(map) != room)
            {
                if (room.RegionCount == 0)
                {
                    return;
                }
                sample = room.Regions[0].AnyCell;
            }
            Cloud c = clouds.TryGetValue(room.ID, out Cloud existing) ? existing : new Cloud();
            // Inflows respect the ventilation guarantee the same way burners do.
            float limit = RoomIsProperlyVentilated(room) ? Mathf.Max(VentilatedCap, c.density) : 1f;
            c.density = Mathf.Min(limit, c.density + amount);
            c.sample = sample;
            clouds[room.ID] = c;
        }

        private void RebuildCellDensity()
        {
            if (clouds.Count == 0)
            {
                if (cellDensity != null)
                {
                    System.Array.Clear(cellDensity, 0, cellDensity.Length);
                }
                drawer?.SetDirty();
                return;
            }
            cellDensity ??= new float[map.cellIndices.NumGridCells];
            System.Array.Clear(cellDensity, 0, cellDensity.Length);
            foreach (Cloud c in clouds.Values)
            {
                Room room = c.sample.IsValid && c.sample.InBounds(map) ? c.sample.GetRoom(map) : null;
                if (room == null || room.UsesOutdoorTemperature)
                {
                    continue;
                }
                foreach (IntVec3 cell in room.Cells)
                {
                    if (cell.InBounds(map))
                    {
                        cellDensity[map.cellIndices.CellToIndex(cell)] = c.density;
                    }
                }
            }
            drawer?.SetDirty();
        }

        private void AffectPawns()
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.RaceProps == null || !pawn.RaceProps.IsFlesh || pawn.Dead)
                {
                    continue;
                }
                Room room = pawn.GetRoom();
                float density = DensityInRoom(room);
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(StrataSmokeDefOf.Strata_SmokeInhalation);
                if (density > HarmThreshold)
                {
                    float scale = StrataMod.Settings?.smokeSeverityScale ?? 1f;
                    if (scale <= 0f)
                    {
                        continue;
                    }
                    hediff ??= pawn.health.GetOrAddHediff(StrataSmokeDefOf.Strata_SmokeInhalation);
                    hediff.Severity += (density - HarmThreshold) * SeverityGain * scale;
                }
                else if (hediff != null)
                {
                    hediff.Severity -= SeverityDecay;
                    if (hediff.Severity <= 0f)
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                }
            }
        }

        private void ThrowMotes()
        {
            foreach (Cloud c in clouds.Values)
            {
                if (c.density <= MoteThreshold || !c.sample.InBounds(map))
                {
                    continue;
                }
                Room room = c.sample.GetRoom(map);
                // Denser smoke: more, bigger puffs scattered across the room.
                int puffs = Mathf.Clamp(Mathf.RoundToInt(c.density * 4f), 1, 5);
                for (int i = 0; i < puffs; i++)
                {
                    IntVec3 cell = room != null && room.CellCount > 1 ? room.Cells.RandomElement() : c.sample;
                    if (cell.InBounds(map) && Rand.Value < 0.6f)
                    {
                        FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), map, 1f + c.density * 2f);
                    }
                }
            }
        }
    }
}

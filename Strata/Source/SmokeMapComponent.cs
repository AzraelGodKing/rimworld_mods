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
    // to the room they sit in; it lingers in enclosed rooms and disperses fast
    // where the room is open to the sky, has an open roof, or a powered exhaust
    // fan. Colonists breathing thick smoke take on a worsening inhalation
    // hediff. Deliberately room-scale (not per-cell) so a tall base stays cheap.
    public class SmokeMapComponent : MapComponent, ICellBoolGiver
    {
        private const int CycleTicks = 60;
        private const float BaseLeak = 0.02f;      // slow seepage from any enclosed room
        private const float OpenRoofVent = 0.06f;  // per open-roof cell (capped)
        private const float OutdoorDisperse = 0.6f; // fraction cleared per cycle in open air
        private const float HarmThreshold = 0.15f;
        private const float SeverityGain = 0.02f;
        private const float SeverityDecay = 0.03f;
        private const float MoteThreshold = 0.2f;

        public readonly HashSet<CompExhaust> Emitters = new HashSet<CompExhaust>();
        public readonly HashSet<CompExhaustVent> Vents = new HashSet<CompExhaustVent>();

        private struct Cloud
        {
            public float density;
            public IntVec3 sample;
        }

        private readonly Dictionary<int, Cloud> clouds = new Dictionary<int, Cloud>();
        private readonly Dictionary<int, float> ventPower = new Dictionary<int, float>();

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

            // 1. Tally powered vents by room.
            ventPower.Clear();
            foreach (CompExhaustVent vent in Vents)
            {
                if (!vent.parent.Spawned || !vent.Active)
                {
                    continue;
                }
                Room r = vent.parent.GetRoom();
                if (r != null)
                {
                    ventPower[r.ID] = ventPower.TryGetValue(r.ID, out float v) ? v + vent.Props.ventPower : vent.Props.ventPower;
                }
            }

            // 2. Disperse / vent existing smoke.
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
                    float vent = BaseLeak
                        + OpenRoofVent * Mathf.Min(r.OpenRoofCount, 5)
                        + (ventPower.TryGetValue(r.ID, out float vp) ? vp : 0f);
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

            // 3. Emit from active burners in enclosed rooms.
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
                c.density = Mathf.Min(1f, c.density + add);
                c.sample = emitter.parent.Position;
                clouds[r.ID] = c;
            }

            RebuildCellDensity();
            AffectPawns();
            ThrowMotes();
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
                    hediff ??= pawn.health.GetOrAddHediff(StrataSmokeDefOf.Strata_SmokeInhalation);
                    hediff.Severity += (density - HarmThreshold) * SeverityGain;
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

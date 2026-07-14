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

        public static HediffDef Strata_ToxGasExposure;

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
        private const float VentExteriorDrain = 0.3f; // per vanilla wall vent facing open air - near-complete emptying
        private const float VentInteriorFlow = 0.15f; // room-to-room equalization through a vanilla wall vent
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

        internal struct Cloud
        {
            public float density;
            public IntVec3 sample;
        }

        private readonly Dictionary<int, Cloud> clouds = new Dictionary<int, Cloud>();

        private readonly Dictionary<int, Cloud> toxicClouds = new Dictionary<int, Cloud>();

        private readonly Dictionary<int, Cloud> naturalGasClouds = new Dictionary<int, Cloud>();

        // Per-cell density mirror for the drawn smog overlay (lazy-allocated).
        private float[] cellDensity;
        private Color[] cellColors;
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
            if (cellColors != null && index >= 0 && index < cellColors.Length && cellColors[index].a > 0.01f)
            {
                return cellColors[index];
            }
            return new Color(0.04f, 0.04f, 0.05f, Mathf.Clamp01(cellDensity[index]));
        }

        public float DensityInRoom(Room room, AtmosphereChannel channel = AtmosphereChannel.Smoke)
        {
            Dictionary<int, Cloud> store = CloudStore(channel);
            return room != null && store.TryGetValue(room.ID, out Cloud c) ? c.density : 0f;
        }

        public void AddGasToRoom(AtmosphereChannel channel, Room room, float amount, IntVec3 sample)
        {
            if (channel == AtmosphereChannel.Smoke)
            {
                AddSmokeToRoom(room, amount, sample);
                return;
            }
            AddGasToRoomInternal(CloudStore(channel), room, amount, sample, channel);
        }

        internal void TransferGasUp(AtmosphereChannel channel, Room sourceRoom, Room upperRoom, Map upperMap, float rate, IntVec3 sample)
        {
            Dictionary<int, Cloud> store = CloudStore(channel);
            if (sourceRoom == null || rate <= 0f || !store.TryGetValue(sourceRoom.ID, out Cloud cloud))
            {
                return;
            }
            float moved = cloud.density * Mathf.Clamp01(rate);
            cloud.density -= moved;
            if (cloud.density < 0.01f)
            {
                store.Remove(sourceRoom.ID);
            }
            else
            {
                store[sourceRoom.ID] = cloud;
            }
            if (moved <= 0f || upperRoom == null || upperRoom.UsesOutdoorTemperature)
            {
                return;
            }
            SmokeMapComponent upperSmoke = upperMap.GetComponent<SmokeMapComponent>();
            upperSmoke?.AddGasToRoom(channel, upperRoom, moved, sample);
        }

        internal Dictionary<int, Cloud> CloudStore(AtmosphereChannel channel)
        {
            switch (channel)
            {
                case AtmosphereChannel.Toxic: return toxicClouds;
                case AtmosphereChannel.NaturalGas: return naturalGasClouds;
                default: return clouds;
            }
        }

        public float DensityInRoom(Room room)
        {
            return room != null && clouds.TryGetValue(room.ID, out Cloud c) ? c.density : 0f;
        }

        // The densest smoke cloud on this map, for alerts and dev tools.
        public bool TryGetWorstCloud(out IntVec3 cell, out float density)
        {
            cell = IntVec3.Invalid;
            density = 0f;
            foreach (Cloud c in clouds.Values)
            {
                if (c.density > density && c.sample.IsValid)
                {
                    density = c.density;
                    cell = c.sample;
                }
            }
            return density > 0f;
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
            toxicClouds.Clear();
            naturalGasClouds.Clear();
            if (cellDensity != null)
            {
                System.Array.Clear(cellDensity, 0, cellDensity.Length);
            }
            if (cellColors != null)
            {
                System.Array.Clear(cellColors, 0, cellColors.Length);
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
                if (clouds.Count > 0 || toxicClouds.Count > 0 || naturalGasClouds.Count > 0)
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
            ProcessAlternativeGasChannels();
            CheckNaturalGasIgnition();
        }

        // Toxic and natural gas reuse the same ventilation model as smoke but
        // have no burner emitters — pockets release them in one burst.
        private void ProcessAlternativeGasChannels()
        {
            ProcessGasChannel(AtmosphereChannel.Toxic, toxicClouds);
            ProcessGasChannel(AtmosphereChannel.NaturalGas, naturalGasClouds);
            if (toxicClouds.Count > 0 || naturalGasClouds.Count > 0)
            {
                RebuildCellDensity();
                AffectAlternativeGases();
            }
        }

        private void ProcessGasChannel(AtmosphereChannel channel, Dictionary<int, Cloud> store)
        {
            if (store.Count == 0)
            {
                return;
            }
            ProcessDirectionalVentsFor(channel, store);
            ProcessShaftRiseFor(channel, store);
            ProcessDoorFlowFor(channel, store);
            DisperseClouds(store);
        }

        private void ProcessShaftRiseFor(AtmosphereChannel channel, Dictionary<int, Cloud> store)
        {
            if (store.Count == 0)
            {
                return;
            }
            float bonusRate = 0f;
            foreach (CompSmokeUpdraft updraft in Updrafts)
            {
                if (updraft.parent.Spawned && updraft.Active)
                {
                    bonusRate += updraft.Props.risePower;
                }
            }
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not PocketMapExit lowerExit || thing is Building_StairsDown or Building_ElevatorDown)
                {
                    continue;
                }
                if (StrataPortalUtility.IsSealedPortal(lowerExit.entrance ?? lowerExit))
                {
                    continue;
                }
                MapPortal upperEntrance = SmokeRiseUtility.GetUpperEntrance(lowerExit);
                if (upperEntrance == null || !upperEntrance.Spawned)
                {
                    continue;
                }
                Room lowerRoom = lowerExit.Position.GetRoom(map);
                if (lowerRoom == null || lowerRoom.UsesOutdoorTemperature || !store.ContainsKey(lowerRoom.ID))
                {
                    continue;
                }
                Room upperRoom = upperEntrance.Position.GetRoom(upperEntrance.Map);
                float rate = SmokeRiseUtility.NaturalShaftRise;
                if (bonusRate > 0f && SmokeRiseUtility.RoomContainsLevelExit(lowerRoom, map))
                {
                    rate = Mathf.Clamp01(rate + bonusRate);
                }
                TransferGasUp(channel, lowerRoom, upperRoom, upperEntrance.Map, rate, upperEntrance.Position);
            }
        }

        private void DisperseClouds(Dictionary<int, Cloud> store)
        {
            foreach (int id in store.Keys.ToList())
            {
                Cloud c = store[id];
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
                    store.Remove(id);
                }
                else
                {
                    store[id] = c;
                }
            }
        }

        private void CheckNaturalGasIgnition()
        {
            if (naturalGasClouds.Count == 0)
            {
                return;
            }
            foreach (KeyValuePair<int, Cloud> kv in naturalGasClouds.ToList())
            {
                Cloud c = kv.Value;
                if (c.density < AtmosphereChannelUtility.IgnitionDensity(AtmosphereChannel.NaturalGas))
                {
                    continue;
                }
                Room room = c.sample.IsValid && c.sample.InBounds(map) ? c.sample.GetRoom(map) : null;
                if (room == null || !RoomHasIgnitionSource(room))
                {
                    continue;
                }
                naturalGasClouds.Remove(kv.Key);
                GenExplosion.DoExplosion(c.sample, map, 2.9f, DamageDefOf.Flame, null, 12, 0.4f);
                Messages.Message("Natural gas ignited!", new TargetInfo(c.sample, map), MessageTypeDefOf.NegativeEvent);
            }
        }

        private bool RoomHasIgnitionSource(Room room)
        {
            foreach (CompExhaust emitter in Emitters)
            {
                if (emitter.parent.Spawned && emitter.Active && emitter.parent.GetRoom() == room)
                {
                    return true;
                }
            }
            foreach (Region region in room.Regions)
            {
                foreach (IntVec3 cell in region.Cells)
                {
                    if (CellHasFire(cell))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CellHasFire(IntVec3 cell)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def == ThingDefOf.Fire)
                {
                    return true;
                }
            }
            return false;
        }

        private void AffectAlternativeGases()
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
                ApplyGasHediff(pawn, room, AtmosphereChannel.Toxic, StrataSmokeDefOf.Strata_ToxGasExposure);
                ApplyGasHediff(pawn, room, AtmosphereChannel.NaturalGas, StrataSmokeDefOf.Strata_ToxGasExposure, scale: 0.35f);
            }
        }

        private static void ApplyGasHediff(Pawn pawn, Room room, AtmosphereChannel channel, HediffDef def, float scale = 1f)
        {
            if (def == null || room == null)
            {
                return;
            }
            float density = room.Map.GetComponent<SmokeMapComponent>()?.DensityInRoom(room, channel) ?? 0f;
            float threshold = AtmosphereChannelUtility.HarmThreshold(channel);
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (density > threshold)
            {
                hediff ??= pawn.health.GetOrAddHediff(def);
                hediff.Severity += (density - threshold) * AtmosphereChannelUtility.SeverityGain(channel) * scale;
            }
            else if (hediff != null && channel == AtmosphereChannel.Toxic)
            {
                hediff.Severity -= 0.03f;
                if (hediff.Severity <= 0f)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
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
                // Doors are their own one-cell "doorway room" and never show
                // up in the room's Regions - walk the border cells instead.
                // Vanilla wall vents flow smoke the same way they flow heat.
                foreach (IntVec3 borderCell in room.BorderCellsCached)
                {
                    if (!borderCell.InBounds(map))
                    {
                        continue;
                    }
                    Building opening = null;
                    bool isVent = false;
                    Building_Door door = borderCell.GetDoor(map);
                    if (door != null)
                    {
                        if (door.Open)
                        {
                            opening = door;
                        }
                    }
                    else
                    {
                        Building edifice = borderCell.GetEdifice(map);
                        if (edifice != null && SmokeVentUtility.IsOpenVent(edifice))
                        {
                            opening = edifice;
                            isVent = true;
                        }
                    }
                    if (opening == null || !countedDoors.Add(opening))
                    {
                        continue;
                    }
                    Room neighbor = null;
                    bool exterior = false;
                    foreach (IntVec3 dir in GenAdj.CardinalDirections)
                    {
                        IntVec3 beyond = opening.Position + dir;
                        if (!beyond.InBounds(map))
                        {
                            continue;
                        }
                        Room other = beyond.GetRoom(map);
                        if (other == null || other == room || other.IsDoorway)
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
                        density *= 1f - (isVent ? VentExteriorDrain : ExteriorDoorDrain);
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
                        float drop = (density - equilibrium) * (isVent ? VentInteriorFlow : InteriorDoorFlow);
                        if (drop > 0.005f)
                        {
                            density -= drop;
                            AddSmokeToRoom(neighbor, drop * cellsHere / cellsThere, opening.Position);
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

        // A working smoke outlet: open sky, an open exterior door, a vanilla
        // wall vent facing open air, a fan or louver whose exhaust side (or
        // duct run) reaches outdoors, or a powered updraft filter beside an
        // unsealed shaft.
        private bool RoomIsProperlyVentilated(Room room)
        {
            if (room.OpenRoofCount > 0 || SmokeVentUtility.RoomHasOpenExteriorDoor(room)
                || RoomHasOutdoorWallVent(room))
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

        private bool RoomHasOutdoorWallVent(Room room)
        {
            Map roomMap = room.Map;
            foreach (IntVec3 cell in room.BorderCellsCached)
            {
                if (!cell.InBounds(roomMap))
                {
                    continue;
                }
                Building edifice = cell.GetEdifice(roomMap);
                if (edifice != null && SmokeVentUtility.IsOpenVent(edifice)
                    && SmokeVentUtility.OpeningLeadsOutdoors(edifice, room))
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
            float toxic = DensityInRoom(cell.GetRoom(map), AtmosphereChannel.Toxic);
            float gas = DensityInRoom(cell.GetRoom(map), AtmosphereChannel.NaturalGas);
            if (density <= 0.001f && toxic <= 0.001f && gas <= 0.001f)
            {
                return;
            }
            Text.Font = GameFont.Small;
            Vector2 mouse = Event.current.mousePosition;
            var rect = new Rect(mouse.x + 12f, mouse.y + 12f, 150f, 48f);
            string line = density > 0.001f ? $"Smoke {Mathf.RoundToInt(density * 100f)}%" : null;
            if (toxic > 0.001f)
            {
                line = (line != null ? line + "\n" : "") + $"Toxic {Mathf.RoundToInt(toxic * 100f)}%";
            }
            if (gas > 0.001f)
            {
                line = (line != null ? line + "\n" : "") + $"Gas {Mathf.RoundToInt(gas * 100f)}%";
            }
            Widgets.Label(rect, line);
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
            AddGasToRoomInternal(clouds, room, amount, sample, AtmosphereChannel.Smoke);
        }

        private void AddGasToRoomInternal(Dictionary<int, Cloud> store, Room room, float amount, IntVec3 sample, AtmosphereChannel channel)
        {
            if (room == null || amount <= 0f)
            {
                return;
            }
            if (!sample.IsValid || !sample.InBounds(map) || sample.GetRoom(map) != room)
            {
                if (room.RegionCount == 0)
                {
                    return;
                }
                sample = room.Regions[0].AnyCell;
            }
            Cloud c = store.TryGetValue(room.ID, out Cloud existing) ? existing : new Cloud();
            float cap = channel == AtmosphereChannel.Smoke
                ? (RoomIsProperlyVentilated(room) ? Mathf.Max(VentilatedCap, c.density) : 1f)
                : 1f;
            c.density = Mathf.Min(cap, c.density + amount);
            c.sample = sample;
            store[room.ID] = c;
        }

        private void ProcessDirectionalVentsFor(AtmosphereChannel channel, Dictionary<int, Cloud> store)
        {
            foreach (CompExhaustVent vent in Vents)
            {
                if (!vent.parent.Spawned || !vent.Active)
                {
                    continue;
                }
                Room intake = vent.IntakeRoom;
                if (intake == null || intake.UsesOutdoorTemperature || !store.TryGetValue(intake.ID, out Cloud cloud))
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
                        AddGasToRoomInternal(store, exhaust, moved, vent.parent.Position, channel);
                    }
                }
                if (cloud.density < 0.01f)
                {
                    store.Remove(intake.ID);
                }
                else
                {
                    store[intake.ID] = cloud;
                }
            }
        }

        private void ProcessDoorFlowFor(AtmosphereChannel channel, Dictionary<int, Cloud> store)
        {
            if (store.Count == 0)
            {
                return;
            }
            var countedDoors = new HashSet<Building>();
            foreach (int id in store.Keys.ToList())
            {
                if (!store.TryGetValue(id, out Cloud c))
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
                foreach (IntVec3 borderCell in room.BorderCellsCached)
                {
                    if (!borderCell.InBounds(map))
                    {
                        continue;
                    }
                    Building opening = null;
                    bool isVent = false;
                    Building_Door door = borderCell.GetDoor(map);
                    if (door != null && door.Open)
                    {
                        opening = door;
                    }
                    else
                    {
                        Building edifice = borderCell.GetEdifice(map);
                        if (edifice != null && SmokeVentUtility.IsOpenVent(edifice))
                        {
                            opening = edifice;
                            isVent = true;
                        }
                    }
                    if (opening == null || !countedDoors.Add(opening))
                    {
                        continue;
                    }
                    Room neighbor = null;
                    bool exterior = false;
                    foreach (IntVec3 dir in GenAdj.CardinalDirections)
                    {
                        IntVec3 beyond = opening.Position + dir;
                        if (!beyond.InBounds(map))
                        {
                            continue;
                        }
                        Room other = beyond.GetRoom(map);
                        if (other == null || other == room || other.IsDoorway)
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
                        density *= 1f - (isVent ? VentExteriorDrain : ExteriorDoorDrain);
                    }
                    else if (neighbor != null && neighbor.CellCount > 0)
                    {
                        float cellsHere = Mathf.Max(room.CellCount, 1);
                        float cellsThere = neighbor.CellCount;
                        float there = store.TryGetValue(neighbor.ID, out Cloud nCloud) ? nCloud.density : 0f;
                        float equilibrium = (density * cellsHere + there * cellsThere) / (cellsHere + cellsThere);
                        float drop = (density - equilibrium) * (isVent ? VentInteriorFlow : InteriorDoorFlow);
                        if (drop > 0.005f)
                        {
                            density -= drop;
                            AddGasToRoomInternal(store, neighbor, drop * cellsHere / cellsThere, opening.Position, channel);
                        }
                    }
                }
                if (density != c.density)
                {
                    if (density < 0.01f)
                    {
                        store.Remove(id);
                    }
                    else
                    {
                        c.density = density;
                        store[id] = c;
                    }
                }
            }
        }

        private void RebuildCellDensity()
        {
            if (clouds.Count == 0 && toxicClouds.Count == 0 && naturalGasClouds.Count == 0)
            {
                if (cellDensity != null)
                {
                    System.Array.Clear(cellDensity, 0, cellDensity.Length);
                }
                if (cellColors != null)
                {
                    System.Array.Clear(cellColors, 0, cellColors.Length);
                }
                drawer?.SetDirty();
                return;
            }
            cellDensity ??= new float[map.cellIndices.NumGridCells];
            cellColors ??= new Color[map.cellIndices.NumGridCells];
            System.Array.Clear(cellDensity, 0, cellDensity.Length);
            System.Array.Clear(cellColors, 0, cellColors.Length);
            PaintChannel(clouds, AtmosphereChannel.Smoke);
            PaintChannel(toxicClouds, AtmosphereChannel.Toxic);
            PaintChannel(naturalGasClouds, AtmosphereChannel.NaturalGas);
            drawer?.SetDirty();
        }

        private void PaintChannel(Dictionary<int, Cloud> store, AtmosphereChannel channel)
        {
            foreach (Cloud c in store.Values)
            {
                Room room = c.sample.IsValid && c.sample.InBounds(map) ? c.sample.GetRoom(map) : null;
                if (room == null || room.UsesOutdoorTemperature)
                {
                    continue;
                }
                foreach (IntVec3 cell in room.Cells)
                {
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }
                    int index = map.cellIndices.CellToIndex(cell);
                    cellDensity[index] = Mathf.Max(cellDensity[index], c.density);
                    Color layer = AtmosphereChannelUtility.OverlayColor(channel, c.density);
                    if (layer.a > cellColors[index].a)
                    {
                        cellColors[index] = layer;
                    }
                }
            }
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

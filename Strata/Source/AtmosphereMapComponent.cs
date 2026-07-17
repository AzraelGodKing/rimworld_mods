using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // A lightweight, room-based multi-gas atmosphere simulation. Every gas is a
    // StrataGasDef channel in the same per-room cloud: combustion smoke from
    // burners, foul deep gas seeping from breached pockets, and whatever comes
    // later. Gas lingers in enclosed rooms and disperses where the room is open
    // to the sky, has an open roof, an exterior door, a vent, or (for buoyant
    // gases) an unsealed stairwell shaft. Per-gas flags decide whether it harms
    // pawns, explodes on open flame, or can be extracted - the movement
    // machinery is shared by all channels, so every ventilation tool works on
    // every gas.
    public class AtmosphereMapComponent : MapComponent, ICellBoolGiver
    {
        private const int CycleTicks = 60;
        private const float OpenRoofVent = 0.06f;  // per open-roof cell (capped)
        private const float OutdoorDisperse = 0.6f; // fraction cleared per cycle in open air
        private const float ExteriorDoorDrain = 0.4f; // per open exterior door - a visible flush
        private const float InteriorDoorFlow = 0.25f; // fraction of the density gap that crosses an open interior door
        private const float VentExteriorDrain = 0.3f; // per vanilla wall vent facing open air - near-complete emptying
        private const float VentInteriorFlow = 0.15f; // room-to-room equalization through a vanilla wall vent
        // Burners and inflows can never push a properly ventilated room past
        // this light haze, safely under harm thresholds - ventilation is a
        // guarantee, not a race between the emission rate and the vent rate.
        // Pre-existing thick gas still drains through the outlets visibly
        // instead of vanishing to the cap.
        private const float VentilatedCap = 0.12f;
        private const float CullThreshold = 0.01f;

        // Roughly atmospheric O₂ fraction for newly opened deep levels.
        public const float AmbientOxygen = 0.21f;

        // Per pawn per atmosphere cycle (~1 second at 1× speed).
        internal const float OxygenPerBreath = 0.0025f;
        internal const float CarbonDioxidePerBreath = 0.0022f;

        public readonly HashSet<CompExhaust> Emitters = new HashSet<CompExhaust>();
        public readonly HashSet<CompExhaustVent> Vents = new HashSet<CompExhaustVent>();
        public readonly HashSet<CompSmokeUpdraft> Updrafts = new HashSet<CompSmokeUpdraft>();
        public readonly HashSet<CompGasExchanger> GasExchangers = new HashSet<CompGasExchanger>();
        public readonly HashSet<CompGasScrubber> Scrubbers = new HashSet<CompGasScrubber>();
        public readonly HashSet<CompGasAirlock> GasSeals = new HashSet<CompGasAirlock>();

        // Rooms holding flammable gas AND an open flame - refreshed each cycle
        // for the alert (imminent-ignition warning).
        public readonly List<IntVec3> FlammableRiskCells = new List<IntVec3>();

        private class Cloud
        {
            // Density per gas, indexed by StrataGasDef.index.
            public float[] density;

            // A cell INSIDE the room; how the cloud resolves its room each
            // cycle and where the overlay paints from.
            public IntVec3 sample;

            // Room size at last normalize - lets a breach dilute mass-
            // conservingly when a pocket chamber merges into a larger room.
            public int cells;

            public float Total()
            {
                float t = 0f;
                for (int i = 0; i < density.Length; i++)
                {
                    t += density[i];
                }
                return t;
            }
        }

        private readonly Dictionary<int, Cloud> clouds = new Dictionary<int, Cloud>();

        // Gas injected before rooms exist (map generation seeds pressurized
        // pockets; incidents can queue too) - drained on the next cycle.
        private struct PendingSeed
        {
            public IntVec3 cell;
            public StrataGasDef gas;
            public float density;
        }

        private static readonly Dictionary<int, List<PendingSeed>> pendingSeedsByMap =
            new Dictionary<int, List<PendingSeed>>();

        // Loaded clouds waiting for rooms to be rebuilt after a load.
        private List<CloudSaveData> loadedClouds;

        // One-time O₂ seed for a freshly finalized underground map.
        private bool breathableAirSeeded;
        private int breathableSeedRetryTick = -1;
        private int breathableSeedRoomCursor;
        private const int BreathableSeedRoomsPerTick = 16;

        // Skip heavy O₂ grid work briefly after a level is created.
        private int atmosphereWarmupTick = -1;
        private const int AtmosphereWarmupTicks = 120;

        // PostSpawnSetup can run before Patch_MapComponents adds us; rescan once.
        private bool buildingCompsRegistered;

        // Per-cell density mirror for the drawn overlay (lazy-allocated),
        // one plane per gas.
        private float[][] cellDensity;
        private CellBoolDrawer drawer;
        private AtmosphereBreathGrid breathGrid;
        private bool breathGridSeeded;

        private static List<StrataGasDef> gasesCached;

        public static List<StrataGasDef> Gases =>
            gasesCached ??= DefDatabase<StrataGasDef>.AllDefsListForReading;

        public AtmosphereMapComponent(Map map) : base(map)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            atmosphereWarmupTick = Find.TickManager.TicksGame;
        }

        private bool AtmosphereWarmingUp()
        {
            if (atmosphereWarmupTick < 0)
            {
                return false;
            }
            if (clouds.Count > 0 || map.mapPawns.AllPawnsSpawned.Count > 0)
            {
                atmosphereWarmupTick = -1;
                return false;
            }
            return Find.TickManager.TicksGame - atmosphereWarmupTick < AtmosphereWarmupTicks;
        }

        private int AtmosphereCycleInterval()
        {
            return CycleTicks * StrataLevelPerfUtility.AtmosphereCycleMultiplier(map);
        }

        private bool ShouldSkipOverlayRebuild()
        {
            if (Find.CurrentMap == map || !StrataLevelPerfUtility.IsStrataPocketLevel(map))
            {
                return false;
            }
            return !Patch_GasOverlay.ShowGasOverlay;
        }

        public Color Color => Color.white;

        // Queue gas into a cell's room before the map is ready for room
        // lookups (during generation) or from another map. Applied without the
        // ventilated cap: seeds are pressurized pockets, not steady inflows.
        public static void QueueSeed(Map map, IntVec3 cell, StrataGasDef gas, float density)
        {
            if (map == null || gas == null || density <= 0f)
            {
                return;
            }
            if (!pendingSeedsByMap.TryGetValue(map.uniqueID, out List<PendingSeed> list))
            {
                pendingSeedsByMap[map.uniqueID] = list = new List<PendingSeed>();
            }
            list.Add(new PendingSeed { cell = cell, gas = gas, density = density });
        }

        public bool GetCellBool(int index) =>
            GasOverlayUtility.CellHasOverlayGas(cellDensity, index);

        public Color GetCellExtraColor(int index) =>
            GasOverlayUtility.GetCellOverlayColor(cellDensity, index);

        public float DensityInRoom(Room room, StrataGasDef gas)
        {
            if (room != null && gas != null && breathGrid != null && BreathingActive()
                && StrataMapUtility.IsUnderground(map)
                && (gas == StrataGasDefOf.Strata_Oxygen || gas == StrataGasDefOf.Strata_CarbonDioxide))
            {
                return breathGrid.AverageInRoom(room, gas);
            }
            return room != null && gas != null && clouds.TryGetValue(room.ID, out Cloud c)
                ? c.density[gas.index]
                : 0f;
        }

        public float DensityAtCell(IntVec3 cell, StrataGasDef gas)
        {
            if (breathGrid != null && BreathingActive() && StrataMapUtility.IsUnderground(map)
                && (gas == StrataGasDefOf.Strata_Oxygen || gas == StrataGasDefOf.Strata_CarbonDioxide))
            {
                return breathGrid.DensityAt(cell, gas);
            }
            Room room = cell.IsValid && cell.InBounds(map) ? cell.GetRoom(map) : null;
            return DensityInRoom(room, gas);
        }

        // Smoke density, kept for the original callers.
        public float DensityInRoom(Room room)
        {
            return DensityInRoom(room, StrataGasDefOf.Strata_Smoke);
        }

        // The densest cloud of a gas on this map, for alerts and dev tools.
        public bool TryGetWorstCloud(StrataGasDef gas, out IntVec3 cell, out float density)
        {
            cell = IntVec3.Invalid;
            density = 0f;
            foreach (Cloud c in clouds.Values)
            {
                if (c.density[gas.index] > density && c.sample.IsValid)
                {
                    density = c.density[gas.index];
                    cell = c.sample;
                }
            }
            return density > 0f;
        }

        // The densest cloud of any pawn-harming gas on this map.
        public bool TryGetWorstHarmfulCloud(out IntVec3 cell, out float density)
        {
            cell = IntVec3.Invalid;
            density = 0f;
            List<StrataGasDef> gases = Gases;
            for (int g = 0; g < gases.Count; g++)
            {
                StrataGasDef gas = gases[g];
                if (gas.harmHediff == null)
                {
                    continue;
                }
                if (TryGetWorstCloud(gas, out IntVec3 gasCell, out float gasDensity))
                {
                    float worry = gas.harmWhenBelow
                        ? Mathf.Max(0f, gas.harmThreshold - gasDensity)
                        : gasDensity;
                    if (worry > density)
                    {
                        density = worry;
                        cell = gasCell;
                    }
                }
            }
            return density > 0f;
        }

        // Dev helpers.
        public string DebugSummary()
        {
            if (clouds.Count == 0)
            {
                return "  (no active gas)";
            }
            var sb = new System.Text.StringBuilder();
            foreach (KeyValuePair<int, Cloud> kv in clouds)
            {
                var parts = new List<string>();
                foreach (StrataGasDef gas in Gases)
                {
                    if (kv.Value.density[gas.index] > 0f)
                    {
                        parts.Add($"{gas.label} {kv.Value.density[gas.index]:F2}");
                    }
                }
                sb.AppendLine($"  room {kv.Key} ({kv.Value.cells} cells): {string.Join(", ", parts)} @ {kv.Value.sample}");
            }
            return sb.ToString().TrimEnd();
        }

        public void ClearAll()
        {
            clouds.Clear();
            FlammableRiskCells.Clear();
            ClearCellDensity();
            drawer?.SetDirty();
        }

        // Dev: instantly fill the room containing a cell with a gas.
        public void DebugSaturate(IntVec3 cell, StrataGasDef gas = null, float density = 1f)
        {
            DebugSetGas(cell, gas ?? StrataGasDefOf.Strata_Smoke, density);
        }

        // Dev: set one gas channel in a room to an exact density.
        public void DebugSetGas(IntVec3 cell, StrataGasDef gas, float density)
        {
            if (gas == null || !cell.InBounds(map))
            {
                return;
            }
            Room room = cell.GetRoom(map);
            if (room == null)
            {
                return;
            }
            if (!clouds.TryGetValue(room.ID, out Cloud c))
            {
                c = new Cloud
                {
                    density = new float[DefDatabase<StrataGasDef>.DefCount],
                    cells = Mathf.Max(room.CellCount, 1),
                    sample = cell,
                };
                clouds[room.ID] = c;
            }
            c.density[gas.index] = Mathf.Max(0f, density);
            c.sample = cell;
            CullOrStore(room.ID, c);
            RebuildCellDensity();
        }

        // A pressurized burst - a breached pocket or an incident - that
        // ignores the ventilated cap. It still drains through outlets over the
        // following cycles.
        public void AddGasBurst(Room room, StrataGasDef gas, float density, IntVec3 sample)
        {
            if (room == null || gas == null || density <= 0f)
            {
                return;
            }
            AddGasToRoom(room, gas, density, sample, bypassCap: true);
            if (clouds.TryGetValue(room.ID, out Cloud c))
            {
                // Pressurized pocket: reach at least this density, not just add a trickle.
                c.density[gas.index] = Mathf.Max(c.density[gas.index], density);
                c.sample = sample.IsValid && sample.InBounds(map) && sample.GetRoom(map) == room
                    ? sample
                    : c.sample;
                clouds[room.ID] = c;
            }
            RebuildCellDensity();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            List<CloudSaveData> saved = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                saved = new List<CloudSaveData>();
                foreach (Cloud c in clouds.Values)
                {
                    if (c.sample.IsValid && c.Total() > 0f)
                    {
                        saved.Add(CloudSaveData.From(c));
                    }
                }
            }
            Scribe_Collections.Look(ref saved, "strataClouds", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && saved != null)
            {
                // Rooms are not rebuilt yet; re-anchor on the first cycle.
                loadedClouds = saved;
            }
            Scribe_Values.Look(ref breathableAirSeeded, "strataBreathableAirSeeded", defaultValue: false);
        }

        public override void MapComponentTick()
        {
            EnsureBuildingCompsRegistered();

            if (pendingSeedsByMap.ContainsKey(map.uniqueID))
            {
                DrainPendingSeeds();
            }

            if (!breathableAirSeeded && StrataMapUtility.IsUnderground(map) && !StrataDepth.IsStarterLevel(map))
            {
                TrySeedBreathableAir();
            }

            if ((Find.TickManager.TicksGame + map.uniqueID) % AtmosphereCycleInterval() != 0)
            {
                return;
            }

            DrainLoadedClouds();
            DrainPendingSeeds();
            NormalizeClouds();

            // The smoke toggle only silences the smoke channel; deep gas is
            // world content, not an optional overlay.
            if (StrataMod.Settings != null && !StrataMod.Settings.smokeEnabled)
            {
                ZeroGasEverywhere(StrataGasDefOf.Strata_Smoke);
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.breathingEnabled)
            {
                ZeroGasEverywhere(StrataGasDefOf.Strata_Oxygen);
                ZeroGasEverywhere(StrataGasDefOf.Strata_CarbonDioxide);
            }

            // 1. One-way wall vents (powered fans and passive louvers).
            ProcessDirectionalVents();

            // 1b. Conduit-layer gas pipes equalize every Strata gas channel
            // between connected rooms and bleed to outdoor pipe terminals.
            ProcessGasPipeNetworks();

            // 2. Buoyant gases rise; heavy gases sink through unsealed shafts.
            SmokeRiseUtility.ProcessMap(this);
            SmokeRiseUtility.ProcessGasSink(this);
            SmokeRiseUtility.ProcessTowerShafts(this);

            // 2b. Surface stairwells bleed fresh outdoor air; open underground
            // rooms exchange with the column above.
            ReplenishAmbientOxygen();

            // 3. Open doors move gas: exterior doors flush it outside fast
            // and visibly, interior doors spill it into the next room so it
            // spreads toward an exit.
            ProcessDoorFlow();

            // 4. Disperse existing gas (open roof, per-gas seepage). Persistent
            // gases (passiveLeak 0) keep a sealed pocket pressurized forever.
            foreach (int id in clouds.Keys.ToList())
            {
                Cloud c = clouds[id];
                Room r = c.sample.IsValid && c.sample.InBounds(map) ? c.sample.GetRoom(map) : null;
                if (r == null || r.UsesOutdoorTemperature)
                {
                    for (int g = 0; g < c.density.Length; g++)
                    {
                        c.density[g] *= 1f - OutdoorDisperse;
                    }
                }
                else
                {
                    float roofVent = OpenRoofVent * Mathf.Min(r.OpenRoofCount, 5);
                    foreach (StrataGasDef gas in Gases)
                    {
                        c.density[gas.index] *= 1f - Mathf.Clamp01(gas.passiveLeak + roofVent);
                    }
                }
                CullOrStore(id, c);
            }

            // 5. Emit from active sources in enclosed rooms (burners smoking,
            // deep vents seeping). Ventilation is a guarantee: emitters can
            // never push a properly ventilated room past a light haze - but
            // thick pre-existing gas drains naturally (and visibly) rather
            // than vanishing.
            bool smokeOff = StrataMod.Settings != null && !StrataMod.Settings.smokeEnabled;
            foreach (CompExhaust emitter in Emitters)
            {
                if (!emitter.parent.Spawned || !emitter.Active)
                {
                    continue;
                }
                StrataGasDef gas = emitter.GasDef;
                if (smokeOff && gas == StrataGasDefOf.Strata_Smoke)
                {
                    continue;
                }
                Room r = emitter.parent.GetRoom();
                if (r == null || r.UsesOutdoorTemperature)
                {
                    continue; // vents straight to open air
                }
                if (BreathingActive() && gas == StrataGasDefOf.Strata_Oxygen && emitter.Props.emitWhenPowered)
                {
                    EnsureBreathGrid()?.EmitOxygenPump(emitter.parent.Position, emitter.Props.emissionPerCycle, r, this);
                    continue;
                }
                // Life-support pumps raise uniform room concentration; combustion
                // smoke is total mass diluted across the chamber volume.
                float add = emitter.Props.emitWhenPowered
                    ? emitter.Props.emissionPerCycle
                    : emitter.Props.emissionPerCycle / Mathf.Max(r.CellCount, 1);
                bool bypassCap = emitter.Props.emitWhenPowered
                    || gas == StrataGasDefOf.Strata_Oxygen || gas == StrataGasDefOf.Strata_CarbonDioxide;
                AddGasToRoom(r, gas, add, emitter.parent.Position, bypassCap: bypassCap);
            }

            // 6. Flammable gas meets open flame.
            CheckIgnition();

            // 7. Black damp (and similar) displaces oxygen in the same room.
            ProcessOxygenDisplacement();

            // 7c. Geothermal geysers seep steam into their room.
            ProcessGeyserSteam();

            // 8. Scrubbers pull CO₂ (and other configured gases) from enclosed rooms.
            ProcessScrubbers();

            // 9. Per-cell O₂/CO₂: roof-column ambient, diffusion, plants, breathing.
            if (BreathingActive() && StrataMapUtility.IsUnderground(map) && !AtmosphereWarmingUp())
            {
                EnsureBreathGrid()?.ProcessCycle(this);
            }

            if (!NeedsCellDensityRebuild() || ShouldSkipOverlayRebuild())
            {
                AffectPawns();
                ThrowMotes();
                return;
            }

            RebuildCellDensity();
            AffectPawns();
            ThrowMotes();
        }

        // CompExhaust.PostSpawnSetup can run before this MapComponent exists
        // (FinalizeInit adds us later). One scan picks up anything missed.
        private AtmosphereBreathGrid EnsureBreathGrid()
        {
            if (!BreathingActive() || !StrataMapUtility.IsUnderground(map))
            {
                return null;
            }
            breathGrid ??= new AtmosphereBreathGrid(map);
            if (!breathGridSeeded)
            {
                breathGridSeeded = true;
                SeedBreathGridFromClouds();
            }
            return breathGrid;
        }

        private void SeedBreathGridFromClouds()
        {
            if (breathGrid == null)
            {
                return;
            }
            foreach (Room room in map.regionGrid.AllRooms)
            {
                if (room == null || room.IsDoorway)
                {
                    continue;
                }
                float o2 = GetBreathRoomDensity(room, StrataGasDefOf.Strata_Oxygen);
                float co2 = GetBreathRoomDensity(room, StrataGasDefOf.Strata_CarbonDioxide);
                if (o2 <= 0f && co2 <= 0f)
                {
                    continue;
                }
                foreach (IntVec3 cell in room.Cells)
                {
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }
                    if (o2 > 0f)
                    {
                        breathGrid.AddO2(cell, o2);
                    }
                    if (co2 > 0f)
                    {
                        breathGrid.AddCo2(cell, co2);
                    }
                }
            }
        }

        internal float GetBreathRoomDensity(Room room, StrataGasDef gas)
        {
            return room != null && gas != null && clouds.TryGetValue(room.ID, out Cloud c)
                ? c.density[gas.index]
                : 0f;
        }

        internal void AddBreathGasToRoom(Room room, StrataGasDef gas, float amount, IntVec3 sample, bool bypassCap = false)
        {
            AddGasToRoom(room, gas, amount, sample, bypassCap);
        }

        internal void ConsumeBreathGasFromRoom(Room room, StrataGasDef gas, float amount)
        {
            ConsumeGasFromRoom(room, gas, amount);
        }

        internal void SetBreathRoomDensity(Room room, StrataGasDef gas, float density, IntVec3 sample)
        {
            if (room == null || gas == null)
            {
                return;
            }
            if (!clouds.TryGetValue(room.ID, out Cloud c))
            {
                c = new Cloud
                {
                    density = new float[DefDatabase<StrataGasDef>.DefCount],
                    cells = Mathf.Max(room.CellCount, 1),
                };
                clouds[room.ID] = c;
            }
            c.density[gas.index] = density;
            c.sample = sample.IsValid ? sample : c.sample;
            c.cells = Mathf.Max(room.CellCount, 1);
            CullOrStore(room.ID, c);
        }

        internal void ProcessBreathPlants(AtmosphereBreathGrid grid)
        {
            List<Thing> plants = map.listerThings.ThingsInGroup(ThingRequestGroup.Plant);
            for (int i = 0; i < plants.Count; i++)
            {
                if (plants[i] is Plant plant)
                {
                    grid.ProcessPlant(plant, this);
                }
            }
        }

        private void EnsureBuildingCompsRegistered()
        {
            if (buildingCompsRegistered)
            {
                return;
            }
            buildingCompsRegistered = true;
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing is not ThingWithComps twc || !thing.Spawned)
                {
                    continue;
                }
                twc.GetComp<CompExhaust>()?.RegisterWithAtmosphere(this);
            }
        }

        private void DrainLoadedClouds()
        {
            if (loadedClouds == null)
            {
                return;
            }
            foreach (CloudSaveData data in loadedClouds)
            {
                data.Restore(this);
            }
            loadedClouds = null;
        }

        private void DrainPendingSeeds()
        {
            if (!pendingSeedsByMap.TryGetValue(map.uniqueID, out List<PendingSeed> seeds))
            {
                return;
            }

            var remaining = new List<PendingSeed>();
            foreach (PendingSeed seed in seeds)
            {
                Room room = seed.cell.InBounds(map) ? seed.cell.GetRoom(map) : null;
                if (room == null)
                {
                    remaining.Add(seed);
                    continue;
                }
                AddGasToRoom(room, seed.gas, seed.density, seed.cell, bypassCap: true);
            }

            if (remaining.Count > 0)
            {
                pendingSeedsByMap[map.uniqueID] = remaining;
            }
            else
            {
                pendingSeedsByMap.Remove(map.uniqueID);
            }
        }

        // Re-key every cloud to the room its sample currently resolves to.
        // Mining and construction change room identity: a breached pocket
        // chamber merges into the corridor that broke it open, and this is
        // where its pressurized gas dilutes (mass-conservingly) into the
        // larger combined room.
        private void NormalizeClouds()
        {
            if (clouds.Count == 0)
            {
                return;
            }
            List<int> keys = clouds.Keys.ToList();
            foreach (int id in keys)
            {
                if (!clouds.TryGetValue(id, out Cloud c))
                {
                    continue;
                }
                Room room = c.sample.IsValid && c.sample.InBounds(map) ? c.sample.GetRoom(map) : null;
                if (room == null)
                {
                    continue; // orphaned; the disperse step decays it like open air
                }
                int newCells = Mathf.Max(room.CellCount, 1);
                if (newCells > c.cells && c.cells > 0)
                {
                    // The room grew (a wall came down): the same gas mass now
                    // fills more space. On a shrink the gas split evenly with
                    // the space, so density is already right.
                    float scale = (float)c.cells / newCells;
                    for (int g = 0; g < c.density.Length; g++)
                    {
                        c.density[g] *= scale;
                    }
                }
                c.cells = newCells;
                if (room.ID == id)
                {
                    continue;
                }
                clouds.Remove(id);
                if (clouds.TryGetValue(room.ID, out Cloud other))
                {
                    // Two clouds landed in the same merged room; both are
                    // already on the merged room's volume basis, so add.
                    for (int g = 0; g < c.density.Length; g++)
                    {
                        other.density[g] += c.density[g];
                    }
                    other.cells = newCells;
                }
                else
                {
                    clouds[room.ID] = c;
                }
            }
        }

        private void ZeroGasEverywhere(StrataGasDef gas)
        {
            foreach (int id in clouds.Keys.ToList())
            {
                Cloud c = clouds[id];
                if (c.density[gas.index] <= 0f)
                {
                    continue;
                }
                c.density[gas.index] = 0f;
                CullOrStore(id, c);
            }
        }

        // Move gas through open doors. Runs on a snapshot of the gassy
        // rooms; gas pushed into a fresh room joins the simulation next
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
                bool changed = false;
                // Doors are their own one-cell "doorway room" and never show
                // up in the room's Regions - walk the border cells instead.
                // Vanilla wall vents flow gas the same way they flow heat.
                foreach (IntVec3 borderCell in room.BorderCellsCached)
                {
                    if (!borderCell.InBounds(map))
                    {
                        continue;
                    }
                    if (CellHasSealedGasAirlock(borderCell))
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
                        Building vent = SmokeVentUtility.FindOpenVentAt(map, borderCell);
                        if (vent != null)
                        {
                            opening = vent;
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
                        float keep = 1f - (isVent ? VentExteriorDrain : ExteriorDoorDrain);
                        for (int g = 0; g < c.density.Length; g++)
                        {
                            c.density[g] *= keep;
                        }
                        changed = true;
                    }
                    else if (neighbor != null && neighbor.CellCount > 0)
                    {
                        // Equalize toward the volume-weighted mix of the two
                        // rooms, conserving gas mass: what a small room
                        // loses in density, a big hall gains as a thin haze.
                        float cellsHere = Mathf.Max(room.CellCount, 1);
                        float cellsThere = neighbor.CellCount;
                        float flowRate = isVent ? VentInteriorFlow : InteriorDoorFlow;
                        foreach (StrataGasDef gas in Gases)
                        {
                            float density = c.density[gas.index];
                            float there = DensityInRoom(neighbor, gas);
                            float equilibrium = (density * cellsHere + there * cellsThere) / (cellsHere + cellsThere);
                            float drop = (density - equilibrium) * flowRate;
                            if (drop > 0.005f)
                            {
                                c.density[gas.index] = density - drop;
                                AddGasToRoom(neighbor, gas, drop * cellsHere / cellsThere, opening.Position);
                                changed = true;
                            }
                        }
                    }
                }
                if (changed)
                {
                    CullOrStore(id, c);
                }
            }
        }

        // A working gas outlet: open sky, an open exterior door, a vanilla
        // wall vent facing open air, a fan or louver whose exhaust side (or
        // duct run) reaches outdoors, or a powered updraft filter beside an
        // unsealed shaft.
        public bool RoomIsProperlyVentilated(Room room)
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
            foreach (CompGasExchanger exchanger in GasExchangers)
            {
                if (exchanger.parent.Spawned && exchanger.Active
                    && exchanger.parent.GetRoom() == room
                    && RoomHasOpenShaftUp(room))
                {
                    return true;
                }
            }
            return false;
        }

        internal float ShaftRiseBoostForRoom(Room room)
        {
            if (room == null)
            {
                return 0f;
            }
            float boost = 0f;
            foreach (CompSmokeUpdraft updraft in Updrafts)
            {
                if (updraft.parent.Spawned && updraft.Active && updraft.parent.GetRoom() == room)
                {
                    boost += updraft.Props.risePower;
                }
            }
            foreach (CompGasExchanger exchanger in GasExchangers)
            {
                if (exchanger.parent.Spawned && exchanger.Active && exchanger.parent.GetRoom() == room)
                {
                    boost += exchanger.Props.transferPower;
                }
            }
            return boost;
        }

        internal float ShaftSinkBoostForRoom(Room room)
        {
            if (room == null)
            {
                return 0f;
            }
            float boost = 0f;
            foreach (CompGasExchanger exchanger in GasExchangers)
            {
                if (exchanger.parent.Spawned && exchanger.Active && exchanger.parent.GetRoom() == room)
                {
                    boost += exchanger.Props.transferPower;
                }
            }
            return boost;
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
                // Includes non-edifice wall-mounted vents (e.g. VTE_WallMountedVent).
                Building vent = SmokeVentUtility.FindOpenVentAt(roomMap, cell);
                if (vent != null && SmokeVentUtility.OpeningLeadsOutdoors(vent, room))
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

        // Combined harmful-gas worry in a room (for sister-mod bridges).
        public float TotalHarmfulLoad(Room room)
        {
            if (room == null)
            {
                return 0f;
            }
            float total = 0f;
            List<StrataGasDef> gases = Gases;
            for (int g = 0; g < gases.Count; g++)
            {
                StrataGasDef gas = gases[g];
                if (gas.harmHediff == null)
                {
                    continue;
                }
                float density = DensityInRoom(room, gas);
                total += gas.harmWhenBelow
                    ? Mathf.Max(0f, gas.harmThreshold - density)
                    : density;
            }
            return total;
        }

        // Draw the gas overlay while the play-settings toggle is on.
        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            if (!Patch_GasOverlay.ShowGasOverlay || cellDensity == null || clouds.Count == 0)
            {
                return;
            }
            if (!StrataMapViewUtility.IsColonyMapView() || Find.CurrentMap != map)
            {
                return;
            }
            drawer ??= new CellBoolDrawer(this, map.Size.x, map.Size.z, 0.62f);
            drawer.MarkForDraw();
            drawer.CellBoolDrawerUpdate();
        }

        // Room gas mix readout under the cursor while the overlay is on.
        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (!Patch_GasOverlay.ShowGasOverlay || !StrataMapViewUtility.IsColonyMapView() || Find.CurrentMap != map)
            {
                return;
            }
            if (StrataMod.Settings != null && StrataMod.Settings.gasOverlayRoomLabels)
            {
                GasOverlayUtility.DrawRoomLabelsOnMap(map, this);
            }
            IntVec3 cell = UI.MouseCell();
            if (!cell.InBounds(map))
            {
                return;
            }
            Room room = cell.Fogged(map) ? null : cell.GetRoom(map);
            GasOverlayUtility.DrawCursorMixReadout(this, room);
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
                        Scale(cloud, 1f - rate);
                    }
                    else
                    {
                        HashSet<Room> pipeRooms = GasPipeUtility.RoomsOnNetwork(map, network);
                        pipeRooms.Remove(intake);
                        if (pipeRooms.Count > 0)
                        {
                            foreach (StrataGasDef gas in Gases)
                            {
                                float moved = cloud.density[gas.index] * rate;
                                if (moved <= 0f)
                                {
                                    continue;
                                }
                                float perRoom = moved / pipeRooms.Count;
                                cloud.density[gas.index] -= moved;
                                foreach (Room pipeRoom in pipeRooms)
                                {
                                    AddGasToRoom(pipeRoom, gas, perRoom, vent.parent.Position);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Room exhaust = vent.ExhaustRoom;
                    if (exhaust != null && exhaust.UsesOutdoorTemperature)
                    {
                        Scale(cloud, 1f - rate);
                    }
                    else if (SmokeVentUtility.CellIsOutdoor(map, SmokeVentUtility.ExhaustCell(vent.parent)))
                    {
                        Scale(cloud, 1f - rate);
                    }
                    else if (exhaust != null)
                    {
                        foreach (StrataGasDef gas in Gases)
                        {
                            float moved = cloud.density[gas.index] * rate;
                            if (moved <= 0f)
                            {
                                continue;
                            }
                            cloud.density[gas.index] -= moved;
                            AddGasToRoom(exhaust, gas, moved, vent.parent.Position);
                        }
                    }
                }

                CullOrStore(intake.ID, cloud);
            }
        }

        private void ProcessGasPipeNetworks()
        {
            if (clouds.Count == 0)
            {
                return;
            }
            GasPipeUtility.ForEachNetwork(map, network =>
            {
                HashSet<Room> rooms = GasPipeUtility.RoomsOnNetwork(map, network);
                if (rooms.Count == 0)
                {
                    return;
                }
                if (GasPipeUtility.NetworkReachesOutdoor(map, network))
                {
                    float keep = 1f - GasPipeUtility.PipeOutdoorDrain;
                    foreach (Room room in rooms)
                    {
                        if (!clouds.TryGetValue(room.ID, out Cloud cloud))
                        {
                            continue;
                        }
                        for (int g = 0; g < cloud.density.Length; g++)
                        {
                            cloud.density[g] *= keep;
                        }
                        CullOrStore(room.ID, cloud);
                    }
                }
                if (rooms.Count < 2)
                {
                    return;
                }
                List<Room> roomList = new List<Room>(rooms);
                foreach (StrataGasDef gas in Gases)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int i = 0; i < roomList.Count; i++)
                    {
                        Room room = roomList[i];
                        if (clouds.TryGetValue(room.ID, out Cloud sample))
                        {
                            sum += sample.density[gas.index];
                            count++;
                        }
                    }
                    if (count < 2)
                    {
                        continue;
                    }
                    float average = sum / count;
                    for (int i = 0; i < roomList.Count; i++)
                    {
                        Room room = roomList[i];
                        if (!clouds.TryGetValue(room.ID, out Cloud cloud))
                        {
                            continue;
                        }
                        float delta = (average - cloud.density[gas.index]) * GasPipeUtility.PipeInteriorFlow;
                        if (delta == 0f)
                        {
                            continue;
                        }
                        cloud.density[gas.index] += delta;
                        CullOrStore(room.ID, cloud);
                    }
                }
            });
        }

        private static void Scale(Cloud cloud, float factor)
        {
            for (int g = 0; g < cloud.density.Length; g++)
            {
                cloud.density[g] *= factor;
            }
        }

        // Move every buoyant gas up an unsealed shaft.
        internal void TransferGasUp(Room sourceRoom, Room upperRoom, Map upperMap, float rate, IntVec3 sample)
        {
            if (sourceRoom == null || rate <= 0f || !clouds.TryGetValue(sourceRoom.ID, out Cloud cloud))
            {
                return;
            }
            rate = Mathf.Clamp01(rate);
            AtmosphereMapComponent upperAtmosphere = upperMap.GetComponent<AtmosphereMapComponent>();
            bool upperReceives = upperRoom != null && !upperRoom.UsesOutdoorTemperature && upperAtmosphere != null;
            foreach (StrataGasDef gas in Gases)
            {
                if (!gas.buoyant)
                {
                    continue;
                }
                float moved = cloud.density[gas.index] * rate;
                if (moved <= 0f)
                {
                    continue;
                }
                cloud.density[gas.index] -= moved;
                if (upperReceives)
                {
                    upperAtmosphere.AddGasToRoom(upperRoom, gas, moved, sample);
                }
            }
            CullOrStore(sourceRoom.ID, cloud);
        }

        // Move every heavy (non-buoyant) gas down an unsealed shaft.
        internal void TransferGasDown(Room sourceRoom, Room lowerRoom, Map lowerMap, float rate, IntVec3 sample)
        {
            if (sourceRoom == null || rate <= 0f || !clouds.TryGetValue(sourceRoom.ID, out Cloud cloud))
            {
                return;
            }
            rate = Mathf.Clamp01(rate);
            AtmosphereMapComponent lowerAtmosphere = lowerMap.GetComponent<AtmosphereMapComponent>();
            bool lowerReceives = lowerRoom != null && !lowerRoom.UsesOutdoorTemperature && lowerAtmosphere != null;
            foreach (StrataGasDef gas in Gases)
            {
                if (gas.buoyant)
                {
                    continue;
                }
                float moved = cloud.density[gas.index] * rate;
                if (moved <= 0f)
                {
                    continue;
                }
                cloud.density[gas.index] -= moved;
                if (lowerReceives)
                {
                    lowerAtmosphere.AddGasToRoom(lowerRoom, gas, moved, sample);
                }
            }
            CullOrStore(sourceRoom.ID, cloud);
        }

        // Seed breathable O₂ in every enclosed room when an underground map
        // first finalizes (or once after load if a pre-breathing save had none).
        // B1 keeps ambient O₂ via ReplenishAmbientOxygen every cycle instead.
        public void TrySeedBreathableAir()
        {
            if (!BreathingActive() || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            if (StrataDepth.IsStarterLevel(map))
            {
                SeedAllEnclosedRoomsWithAmbientOxygen();
                return;
            }

            if (breathableAirSeeded)
            {
                return;
            }

            if (breathableSeedRetryTick < 0)
            {
                breathableSeedRetryTick = Find.TickManager.TicksGame;
            }

            int seeded = SeedAllEnclosedRoomsWithAmbientOxygen(BreathableSeedRoomsPerTick);
            int roomCount = map.regionGrid.AllRooms.Count;
            if ((seeded > 0 && breathableSeedRoomCursor == 0 && roomCount > 0)
                || Find.TickManager.TicksGame - breathableSeedRetryTick > 1200)
            {
                breathableAirSeeded = true;
            }
        }

        private int SeedAllEnclosedRoomsWithAmbientOxygen(int maxRooms = int.MaxValue)
        {
            StrataGasDef oxygen = StrataGasDefOf.Strata_Oxygen;
            if (oxygen == null)
            {
                return 0;
            }

            IReadOnlyList<Room> rooms = map.regionGrid.AllRooms;
            if (rooms.Count == 0)
            {
                return 0;
            }

            int count = 0;
            int end = Mathf.Min(rooms.Count, breathableSeedRoomCursor + maxRooms);
            for (int i = breathableSeedRoomCursor; i < end; i++)
            {
                Room room = rooms[i];
                if (room == null || room.UsesOutdoorTemperature || !TryGetSampleCell(room, out IntVec3 sample))
                {
                    continue;
                }
                AddGasToRoom(room, oxygen, AmbientOxygen, sample, bypassCap: true);
                count++;
            }
            breathableSeedRoomCursor = end >= rooms.Count ? 0 : end;
            return count;
        }

        private bool NeedsCellDensityRebuild()
        {
            if (clouds.Count > 0)
            {
                return true;
            }
            if (BreathingActive() && StrataMapUtility.IsUnderground(map) && breathGrid != null && breathGridSeeded)
            {
                return !AtmosphereWarmingUp();
            }
            return Emitters.Count > 0;
        }

        private bool BreathingActive()
        {
            return StrataMod.Settings == null || StrataMod.Settings.breathingEnabled;
        }

        // Surface stairwell landings and open-to-sky underground rooms stay
        // topped up with fresh O₂ so shafts can feed the column.
        private void ReplenishAmbientOxygen()
        {
            if (!BreathingActive() || StrataGasDefOf.Strata_Oxygen == null)
            {
                return;
            }
            StrataGasDef oxygen = StrataGasDefOf.Strata_Oxygen;
            if (!StrataMapUtility.IsUnderground(map))
            {
                foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (thing is not Building_StairsDown stairsDown || !stairsDown.Spawned)
                    {
                        continue;
                    }
                    Room room = stairsDown.Position.GetRoom(map);
                    if (room == null)
                    {
                        continue;
                    }
                    TopUpOxygen(room, oxygen, stairsDown.Position);
                }
                return;
            }
            foreach (Room room in map.regionGrid.AllRooms)
            {
                if (room == null || !room.UsesOutdoorTemperature || !TryGetSampleCell(room, out IntVec3 sample))
                {
                    continue;
                }
                TopUpOxygen(room, oxygen, sample);
            }
        }

        private void TopUpOxygen(Room room, StrataGasDef oxygen, IntVec3 sample)
        {
            float current = DensityInRoom(room, oxygen);
            if (current >= AmbientOxygen - 0.01f)
            {
                return;
            }
            AddGasToRoom(room, oxygen, AmbientOxygen - current, sample, bypassCap: true);
        }

        private static bool TryGetSampleCell(Room room, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (room == null || room.RegionCount <= 0)
            {
                return false;
            }
            Region region = room.FirstRegion;
            if (region == null)
            {
                return false;
            }
            cell = region.AnyCell;
            return cell.IsValid;
        }

        private void ProcessOxygenDisplacement()
        {
            if (!BreathingActive() || clouds.Count == 0 || StrataGasDefOf.Strata_Oxygen == null)
            {
                return;
            }
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
                float displace = 0f;
                foreach (StrataGasDef gas in Gases)
                {
                    if (gas.displacesOxygen <= 0f)
                    {
                        continue;
                    }
                    float density = c.density[gas.index];
                    if (density >= gas.harmThreshold * 0.5f)
                    {
                        displace += gas.displacesOxygen * density;
                    }
                }
                if (displace > 0f)
                {
                    ConsumeGasFromRoom(room, StrataGasDefOf.Strata_Oxygen, displace);
                }
            }
        }

        private void ProcessGeyserSteam()
        {
            if (StrataGasDefOf.Strata_Steam == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            List<Thing> geysers = map.listerThings.ThingsOfDef(ThingDefOf.SteamGeyser);
            if (geysers == null || geysers.Count == 0)
            {
                return;
            }
            for (int i = 0; i < geysers.Count; i++)
            {
                Thing geyser = geysers[i];
                Room room = geyser.GetRoom();
                if (room == null || room.UsesOutdoorTemperature)
                {
                    continue;
                }
                AddGasToRoom(room, StrataGasDefOf.Strata_Steam, 0.012f / Mathf.Max(room.CellCount, 1),
                    geyser.Position);
            }
        }

        private void ProcessScrubbers()
        {
            if (Scrubbers.Count == 0)
            {
                return;
            }
            foreach (CompGasScrubber scrubber in Scrubbers)
            {
                if (!scrubber.parent.Spawned || !scrubber.Active)
                {
                    continue;
                }
                Room room = scrubber.parent.GetRoom();
                if (room == null || room.UsesOutdoorTemperature)
                {
                    continue;
                }
                StrataGasDef gas = scrubber.GasDef;
                if (gas == null)
                {
                    continue;
                }
                ConsumeGasFromRoom(room, gas, scrubber.Props.scrubPerCycle);
                if (scrubber.Props.restoreOxygenPerCycle > 0f && StrataGasDefOf.Strata_Oxygen != null)
                {
                    AddGasToRoom(room, StrataGasDefOf.Strata_Oxygen, scrubber.Props.restoreOxygenPerCycle,
                        scrubber.parent.Position, bypassCap: true);
                }
            }
        }

        private void ConsumeGasFromRoom(Room room, StrataGasDef gas, float amount)
        {
            if (room == null || gas == null || amount <= 0f || !clouds.TryGetValue(room.ID, out Cloud cloud))
            {
                return;
            }
            cloud.density[gas.index] = Mathf.Max(0f, cloud.density[gas.index] - amount);
            CullOrStore(room.ID, cloud);
        }

        internal void ConsumeGasFromRoomPublic(Room room, StrataGasDef gas, float amount)
        {
            ConsumeGasFromRoom(room, gas, amount);
        }

        internal void AddGasToRoomPublic(Room room, StrataGasDef gas, float amount, IntVec3 sample)
        {
            AddGasToRoom(room, gas, amount, sample);
        }

        private bool CellHasSealedGasAirlock(IntVec3 cell)
        {
            Building building = cell.GetEdifice(map);
            CompGasAirlock seal = building?.GetComp<CompGasAirlock>();
            return seal != null && seal.IsSealed;
        }

        private void AddGasToRoom(Room room, StrataGasDef gas, float amount, IntVec3 sample, bool bypassCap = false)
        {
            if (room == null || gas == null || amount <= 0f)
            {
                return;
            }
            // The sample cell must sit INSIDE the room: it is how the cloud
            // resolves its room each cycle and where the overlay paints.
            // Callers often pass a door or vent cell, which belongs to its own
            // one-cell room and would make the gas invisible.
            if (!sample.IsValid || !sample.InBounds(map) || sample.GetRoom(map) != room)
            {
                if (!TryGetSampleCell(room, out sample))
                {
                    return;
                }
            }
            if (!clouds.TryGetValue(room.ID, out Cloud c))
            {
                c = new Cloud
                {
                    density = new float[DefDatabase<StrataGasDef>.DefCount],
                    cells = Mathf.Max(room.CellCount, 1),
                };
                clouds[room.ID] = c;
            }
            // Inflows respect the ventilation guarantee the same way burners
            // do; pressurized bursts (breached pockets, incidents) do not.
            float limit = bypassCap ? float.MaxValue
                : RoomIsProperlyVentilated(room) ? Mathf.Max(VentilatedCap, c.density[gas.index])
                : 1f;
            c.density[gas.index] = Mathf.Min(limit, c.density[gas.index] + amount);
            c.sample = sample;
        }

        private void CullOrStore(int id, Cloud cloud)
        {
            bool anyLeft = false;
            for (int g = 0; g < cloud.density.Length; g++)
            {
                if (cloud.density[g] < CullThreshold)
                {
                    cloud.density[g] = 0f;
                }
                else
                {
                    anyLeft = true;
                }
            }
            if (!anyLeft)
            {
                clouds.Remove(id);
            }
            else
            {
                clouds[id] = cloud;
            }
        }

        // A room full of flammable gas explodes the moment it holds an open
        // flame: raw fires, lit torches and campfires, and any actively
        // burning smoke emitter (a running wood generator is an open firebox).
        private void CheckIgnition()
        {
            FlammableRiskCells.Clear();
            if (clouds.Count == 0)
            {
                return;
            }
            foreach (int id in clouds.Keys.ToList())
            {
                if (!clouds.TryGetValue(id, out Cloud c))
                {
                    continue;
                }
                float flammableTotal = 0f;
                float lowestIgnition = float.MaxValue;
                foreach (StrataGasDef gas in Gases)
                {
                    if (gas.flammable && c.density[gas.index] > 0.01f)
                    {
                        flammableTotal += c.density[gas.index];
                        lowestIgnition = Mathf.Min(lowestIgnition, gas.ignitionDensity);
                    }
                }
                if (flammableTotal < lowestIgnition * 0.5f)
                {
                    continue;
                }
                Room room = c.sample.IsValid && c.sample.InBounds(map) ? c.sample.GetRoom(map) : null;
                if (room == null)
                {
                    continue;
                }
                Thing flame = FindOpenFlame(room);
                if (flame == null)
                {
                    continue;
                }
                if (flammableTotal < lowestIgnition)
                {
                    // Gas is pooling around an open flame but hasn't reached
                    // ignition density yet - warn.
                    FlammableRiskCells.Add(c.sample);
                    continue;
                }
                Ignite(room, c, flame, flammableTotal);
            }
        }

        public static Thing FindOpenFlame(Room room)
        {
            if (room == null)
            {
                return null;
            }
            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                if (thing == null || !thing.Spawned || thing.GetRoom() != room)
                {
                    continue;
                }
                if (IsIgnitionSource(thing))
                {
                    return thing;
                }
            }
            return null;
        }

        // Open flame or fuel-burning combustion — not gas vents, pumps, or scrubbers.
        public static bool IsIgnitionSource(Thing thing)
        {
            if (thing is Fire)
            {
                return true;
            }
            if (thing is not ThingWithComps twc)
            {
                return false;
            }
            if (twc.GetComp<CompDeepGasVent>() != null)
            {
                return false;
            }
            CompExhaust exhaust = twc.GetComp<CompExhaust>();
            if (exhaust != null && exhaust.Active)
            {
                if (exhaust.Props.emitWhenPowered)
                {
                    return false;
                }
                StrataGasDef gas = exhaust.GasDef;
                if (gas != null && gas != StrataGasDefOf.Strata_Smoke)
                {
                    return false;
                }
                CompPowerTrader power = twc.GetComp<CompPowerTrader>();
                if (power != null && power.PowerOn && power.PowerOutput > 0f)
                {
                    return true;
                }
                CompRefuelable refuel = twc.GetComp<CompRefuelable>();
                if (refuel != null && refuel.HasFuel)
                {
                    return true;
                }
                return false;
            }
            if (twc.GetComp<CompFireOverlay>() != null
                && twc.GetComp<CompRefuelable>()?.HasFuel != false
                && twc.GetComp<CompFlickable>()?.SwitchIsOn != false)
            {
                return true;
            }
            return false;
        }

        private void Ignite(Room room, Cloud cloud, Thing flame, float flammableTotal)
        {
            float radius = Mathf.Clamp(1.9f + flammableTotal * 2.2f, 2.4f, 8.5f);
            GenExplosion.DoExplosion(flame.Position, map, radius, DamageDefOf.Flame, flame);
            // Big rooms catch scattered secondary fires beyond the blast.
            int fires = Mathf.Min(Mathf.RoundToInt(flammableTotal * room.CellCount * 0.05f), 10);
            for (int i = 0; i < fires; i++)
            {
                IntVec3 cell = room.Cells.RandomElement();
                if (cell.InBounds(map))
                {
                    FireUtility.TryStartFireIn(cell, map, Rand.Range(0.3f, 0.8f), flame);
                }
            }
            foreach (StrataGasDef gas in Gases)
            {
                if (gas.flammable)
                {
                    cloud.density[gas.index] = 0f;
                }
            }
            // After a burn, the room fills with black damp — oxygen-poor residue.
            if (StrataGasDefOf.Strata_BlackDamp != null)
            {
                float residue = Mathf.Clamp(flammableTotal * 0.55f, 0.2f, 0.85f);
                cloud.density[StrataGasDefOf.Strata_BlackDamp.index] =
                    Mathf.Max(cloud.density[StrataGasDefOf.Strata_BlackDamp.index], residue);
                if (StrataGasDefOf.Strata_Oxygen != null)
                {
                    cloud.density[StrataGasDefOf.Strata_Oxygen.index] *= 0.35f;
                }
            }
            Find.LetterStack.ReceiveLetter("Strata_GasIgnitionLetterTitle".Translate(),
                "Strata_GasIgnitionLetterText".Translate(flame.LabelShort),
                LetterDefOf.NegativeEvent, new TargetInfo(flame.Position, map));
        }

        private void ClearCellDensity()
        {
            if (cellDensity == null)
            {
                return;
            }
            for (int g = 0; g < cellDensity.Length; g++)
            {
                if (cellDensity[g] != null)
                {
                    System.Array.Clear(cellDensity[g], 0, cellDensity[g].Length);
                }
            }
        }

        private void RebuildCellDensity()
        {
            cellDensity ??= new float[DefDatabase<StrataGasDef>.DefCount][];
            ClearCellDensity();
            StrataGasDef oxygen = StrataGasDefOf.Strata_Oxygen;
            StrataGasDef co2 = StrataGasDefOf.Strata_CarbonDioxide;
            bool useBreathGrid = breathGrid != null && BreathingActive() && StrataMapUtility.IsUnderground(map);
            foreach (Cloud c in clouds.Values)
            {
                Room room = c.sample.IsValid && c.sample.InBounds(map) ? c.sample.GetRoom(map) : null;
                if (room == null || room.UsesOutdoorTemperature)
                {
                    continue;
                }
                foreach (StrataGasDef gas in Gases)
                {
                    if (useBreathGrid && (gas == oxygen || gas == co2))
                    {
                        continue;
                    }
                    float d = c.density[gas.index];
                    if (d <= 0f)
                    {
                        continue;
                    }
                    cellDensity[gas.index] ??= new float[map.cellIndices.NumGridCells];
                    float[] plane = cellDensity[gas.index];
                    foreach (IntVec3 cell in room.Cells)
                    {
                        if (cell.InBounds(map) && !cell.Fogged(map))
                        {
                            plane[map.cellIndices.CellToIndex(cell)] = d;
                        }
                    }
                }
            }
            if (useBreathGrid)
            {
                breathGrid.PaintCellDensity(cellDensity, oxygen);
                breathGrid.PaintCellDensity(cellDensity, co2);
            }
            drawer?.SetDirty();
        }

        private void AffectPawns()
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            List<StrataGasDef> gases = Gases;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.RaceProps == null || !pawn.RaceProps.IsFlesh || pawn.Dead)
                {
                    continue;
                }
                if (StrataCagedBirdRegistry.IsContained(pawn))
                {
                    continue;
                }
                Room room = pawn.GetRoom();
                if (room != null && room.UsesOutdoorTemperature)
                {
                    // Open air is always breathable; clear hypoxia / CO₂ buildup.
                    for (int g = 0; g < gases.Count; g++)
                    {
                        StrataGasDef gas = gases[g];
                        if (gas.harmHediff == null)
                        {
                            continue;
                        }
                        Hediff outdoor = pawn.health.hediffSet.GetFirstHediffOfDef(gas.harmHediff);
                        if (outdoor != null)
                        {
                            outdoor.Severity -= gas.severityDecay * 2f;
                            if (outdoor.Severity <= 0f)
                            {
                                pawn.health.RemoveHediff(outdoor);
                            }
                        }
                    }
                    continue;
                }
                for (int g = 0; g < gases.Count; g++)
                {
                    StrataGasDef gas = gases[g];
                    if (gas.harmHediff == null)
                    {
                        continue;
                    }
                    if (!BreathingActive()
                        && (gas == StrataGasDefOf.Strata_Oxygen || gas == StrataGasDefOf.Strata_CarbonDioxide))
                    {
                        continue;
                    }
                    float density = (breathGrid != null && BreathingActive()
                        && StrataMapUtility.IsUnderground(map)
                        && (gas == StrataGasDefOf.Strata_Oxygen || gas == StrataGasDefOf.Strata_CarbonDioxide))
                        ? breathGrid.DensityAt(pawn.Position, gas)
                        : DensityInRoom(room, gas);
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(gas.harmHediff);
                    if (gas.harmWhenBelow)
                    {
                        if (density < gas.harmThreshold)
                        {
                            hediff ??= pawn.health.GetOrAddHediff(gas.harmHediff);
                            hediff.Severity += (gas.harmThreshold - density) * gas.severityGain;
                        }
                        else if (hediff != null)
                        {
                            hediff.Severity -= gas.severityDecay;
                            if (hediff.Severity <= 0f)
                            {
                                pawn.health.RemoveHediff(hediff);
                            }
                        }
                        continue;
                    }
                    if (density > gas.harmThreshold)
                    {
                        float scale = gas == StrataGasDefOf.Strata_Smoke
                            ? StrataMod.Settings?.smokeSeverityScale ?? 1f
                            : 1f;
                        if (scale <= 0f)
                        {
                            continue;
                        }
                        hediff ??= pawn.health.GetOrAddHediff(gas.harmHediff);
                        hediff.Severity += (density - gas.harmThreshold) * gas.severityGain * scale;
                    }
                    else if (hediff != null)
                    {
                        hediff.Severity -= gas.severityDecay;
                        if (hediff.Severity <= 0f)
                        {
                            pawn.health.RemoveHediff(hediff);
                        }
                    }
                }
            }
        }

        private void ThrowMotes()
        {
            foreach (Cloud c in clouds.Values)
            {
                if (!c.sample.InBounds(map) || c.sample.Fogged(map))
                {
                    continue;
                }
                foreach (StrataGasDef gas in Gases)
                {
                    if (!gas.throwsMotes || c.density[gas.index] <= gas.moteThreshold)
                    {
                        continue;
                    }
                    float density = c.density[gas.index];
                    Room room = c.sample.GetRoom(map);
                    // Denser gas: more, bigger puffs scattered across the room.
                    int puffs = Mathf.Clamp(Mathf.RoundToInt(density * 4f), 1, 5);
                    for (int i = 0; i < puffs; i++)
                    {
                        IntVec3 cell = room != null && room.CellCount > 1 ? room.Cells.RandomElement() : c.sample;
                        if (cell.InBounds(map) && !cell.Fogged(map) && Rand.Value < 0.6f)
                        {
                            FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), map, 1f + density * 2f);
                        }
                    }
                }
            }
        }

        // One saved cloud: a sample cell plus the gases in it. Room IDs are
        // not stable across loads, so clouds re-anchor to whatever room the
        // sample cell resolves to on the first cycle after loading.
        private class CloudSaveData : IExposable
        {
            private IntVec3 sample;
            private List<string> gasDefs;
            private List<float> amounts;

            public static CloudSaveData From(Cloud cloud)
            {
                var data = new CloudSaveData
                {
                    sample = cloud.sample,
                    gasDefs = new List<string>(),
                    amounts = new List<float>(),
                };
                foreach (StrataGasDef gas in Gases)
                {
                    if (cloud.density[gas.index] > 0f)
                    {
                        data.gasDefs.Add(gas.defName);
                        data.amounts.Add(cloud.density[gas.index]);
                    }
                }
                return data;
            }

            public void Restore(AtmosphereMapComponent atmosphere)
            {
                if (gasDefs == null || amounts == null)
                {
                    return;
                }
                Room room = sample.IsValid && sample.InBounds(atmosphere.map)
                    ? sample.GetRoom(atmosphere.map)
                    : null;
                if (room == null)
                {
                    return;
                }
                for (int i = 0; i < gasDefs.Count && i < amounts.Count; i++)
                {
                    StrataGasDef gas = DefDatabase<StrataGasDef>.GetNamedSilentFail(gasDefs[i]);
                    if (gas != null)
                    {
                        atmosphere.AddGasToRoom(room, gas, amounts[i], sample, bypassCap: true);
                    }
                }
            }

            public void ExposeData()
            {
                Scribe_Values.Look(ref sample, "sample");
                Scribe_Collections.Look(ref gasDefs, "gases", LookMode.Value);
                Scribe_Collections.Look(ref amounts, "amounts", LookMode.Value);
            }
        }
    }

    // Save-compat: maps saved before the atmosphere generalization carry a
    // component of this class; loading it as a subclass keeps those saves
    // clean while GetComponent<AtmosphereMapComponent>() finds it.
    public class SmokeMapComponent : AtmosphereMapComponent
    {
        public SmokeMapComponent(Map map) : base(map)
        {
        }
    }
}

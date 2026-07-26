using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Per-cell O₂ / CO₂ for underground maps. Natural caverns use localized
    // falloff from pumps and roof-column ambient; player-built sealed rooms
    // share a uniform mix that pressurizes evenly.
    internal sealed class AtmosphereBreathGrid
    {
        internal enum BreathWorkPhase
        {
            Diffusion = 0,
            Ambient = 1,
            Exchange = 2,
            RoomSync = 3,
        }

        private const float O2PumpRadius = 16f;
        private const float DiffusionRate = 0.11f;
        private const float RoofAmbientRate = 0.2f;
        private const float PlantO2PerCycle = AtmosphereMapComponent.PlantO2PerCycle;
        private const float PlantCo2PerCycle = AtmosphereMapComponent.PlantCo2PerCycle;
        private const float MinPlantGrowth = AtmosphereMapComponent.MinPlantGrowth;
        private const float BuoyancyBias = 0.045f;

        private readonly Map map;
        private float[] o2;
        private float[] co2;
        private float[] scratchO2;
        private float[] scratchCo2;
        private bool[] skipDiffusionCell;
        private HashSet<int> colonyBuiltRoomIds;
        private int colonyBuiltCacheTick = -1;
        private int roomBatchCursor;
        private int plantBatchCursor;

        public AtmosphereBreathGrid(Map map)
        {
            this.map = map;
        }

        public void ProcessCycle(AtmosphereMapComponent atmosphere)
        {
            ProcessCyclePhase(atmosphere, BreathWorkPhase.Diffusion);
            ProcessCyclePhase(atmosphere, BreathWorkPhase.Ambient);
            ProcessCyclePhase(atmosphere, BreathWorkPhase.Exchange);
            ProcessCyclePhase(atmosphere, BreathWorkPhase.RoomSync);
        }

        public void ProcessCyclePhase(AtmosphereMapComponent atmosphere, BreathWorkPhase phase)
        {
            if (map == null || atmosphere == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            EnsureArrays();
            switch (phase)
            {
                case BreathWorkPhase.Diffusion:
                    ProcessDiffusionPhase();
                    break;
                case BreathWorkPhase.Ambient:
                    ApplyNaturalRoofAmbientBatched();
                    break;
                case BreathWorkPhase.Exchange:
                    ProcessBreathPlantsBatched(atmosphere);
                    ProcessBreathing(atmosphere);
                    break;
                case BreathWorkPhase.RoomSync:
                    EqualizeColonySealedRoomsBatched(atmosphere);
                    SyncRoomCloudsBatched(atmosphere);
                    break;
            }
        }

        private void ProcessDiffusionPhase()
        {
            bool appliedDiff = false;
            if (StrataOffThreadWork.OffThreadAtmosphereEnabled)
            {
                int tick = Find.TickManager?.TicksGame ?? 0;
                StrataOffThreadWork.TryDrainBreathDiffusion(map, o2, co2, tick, out appliedDiff);
            }
            if (StrataOffThreadWork.OffThreadAtmosphereEnabled)
            {
                if (!TryEnqueueOffThreadDiffusion())
                {
                    if (!appliedDiff && StrataOffThreadWork.HasPendingBreathDiffusion(map.uniqueID))
                    {
                        StrataOffThreadWork.CancelMap(map.uniqueID);
                        DiffuseOpenCavern();
                    }
                }
            }
            else
            {
                DiffuseOpenCavern();
            }
        }

        private bool TryEnqueueOffThreadDiffusion()
        {
            if (StrataOffThreadWork.HasPendingBreathDiffusion(map.uniqueID))
            {
                return false;
            }
            EnsureSkipDiffusionMask();
            return StrataOffThreadWork.TryEnqueueBreathDiffusion(
                map, o2, co2, skipDiffusionCell, Find.TickManager.TicksGame);
        }

        private void RefreshColonyBuiltCacheIfNeeded()
        {
            int tick = Find.TickManager.TicksGame;
            int ttl = StrataLevelPerfUtility.LiteAtmosphereOnMap(map) ? 600 : 240;
            if (colonyBuiltRoomIds != null && tick - colonyBuiltCacheTick < ttl)
            {
                return;
            }
            colonyBuiltRoomIds ??= new HashSet<int>();
            colonyBuiltRoomIds.Clear();
            foreach (Room room in map.regionGrid.AllRooms)
            {
                if (room != null && StrataRoomUtility.RoomIsColonyBuiltEnclosure(room))
                {
                    colonyBuiltRoomIds.Add(room.ID);
                }
            }
            colonyBuiltCacheTick = tick;
        }

        private bool RoomIsColonyBuiltCached(Room room)
        {
            if (room == null)
            {
                return false;
            }
            RefreshColonyBuiltCacheIfNeeded();
            return colonyBuiltRoomIds.Contains(room.ID);
        }

        private void EnsureSkipDiffusionMask()
        {
            int cells = map.cellIndices.NumGridCells;
            if (skipDiffusionCell == null || skipDiffusionCell.Length != cells)
            {
                skipDiffusionCell = new bool[cells];
            }
            else
            {
                System.Array.Clear(skipDiffusionCell, 0, cells);
            }
            RefreshColonyBuiltCacheIfNeeded();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                Room room = cell.GetRoom(map);
                if (room != null && colonyBuiltRoomIds.Contains(room.ID))
                {
                    skipDiffusionCell[map.cellIndices.CellToIndex(cell)] = true;
                }
            }
        }

        public void EmitOxygenPump(IntVec3 center, float emissionPerCycle, Room room, AtmosphereMapComponent atmosphere)
        {
            if (emissionPerCycle <= 0f || atmosphere == null || !center.InBounds(map))
            {
                return;
            }
            EnsureArrays();
            if (room != null && RoomIsColonyBuiltCached(room))
            {
                atmosphere.AddBreathGasToRoom(room, StrataGasDefOf.Strata_Oxygen, emissionPerCycle, center, bypassCap: true);
                return;
            }
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, O2PumpRadius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                float dist = cell.DistanceTo(center);
                if (dist > O2PumpRadius)
                {
                    continue;
                }
                float falloff = 1f - dist / O2PumpRadius;
                AddCellO2(cell, emissionPerCycle * falloff * falloff);
            }
        }

        public void ProcessBreathing(AtmosphereMapComponent atmosphere)
        {
            StrataGasDef oxygen = StrataGasDefOf.Strata_Oxygen;
            StrataGasDef co2 = StrataGasDefOf.Strata_CarbonDioxide;
            if (oxygen == null || co2 == null)
            {
                return;
            }
            EnsureArrays();
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.RaceProps == null || pawn.Dead || !pawn.Spawned)
                {
                    continue;
                }
                if (!StrataPawnUtility.IsPlantBreather(pawn)
                    && (!pawn.RaceProps.IsFlesh || !StrataLevelPerfUtility.ShouldAffectAnimalGasHarm(pawn, map)))
                {
                    continue;
                }
                IntVec3 cell = pawn.Position;
                if (!cell.InBounds(map))
                {
                    continue;
                }
                Room room = pawn.GetRoom();
                if (room == null || room.UsesOutdoorTemperature)
                {
                    continue;
                }
                float bodyScale = pawn.BodySize;
                float o2Need = AtmosphereMapComponent.OxygenPerBreath * bodyScale;
                float co2Out = AtmosphereMapComponent.CarbonDioxidePerBreath * bodyScale;
                if (StrataPawnUtility.IsPlantBreather(pawn))
                {
                    if (room != null && RoomIsColonyBuiltCached(room))
                    {
                        atmosphere.ConsumeBreathGasFromRoom(room, co2, PlantCo2PerCycle * bodyScale);
                        atmosphere.AddBreathGasToRoom(room, oxygen, PlantO2PerCycle * bodyScale, cell, bypassCap: true);
                    }
                    else
                    {
                        ConsumeCellCo2(cell, PlantCo2PerCycle * bodyScale);
                        AddCellO2(cell, PlantO2PerCycle * bodyScale);
                    }
                }
                else if (room != null && RoomIsColonyBuiltCached(room))
                {
                    atmosphere.ConsumeBreathGasFromRoom(room, oxygen, o2Need);
                    atmosphere.AddBreathGasToRoom(room, co2, co2Out, cell);
                }
                else
                {
                    ConsumeCellO2(cell, o2Need);
                    AddCellCo2(cell, co2Out);
                }
            }
        }

        public void ProcessPlant(Plant plant, AtmosphereMapComponent atmosphere)
        {
            if (plant == null || !plant.Spawned || plant.Destroyed || plant.def?.plant == null)
            {
                return;
            }
            if (plant.LifeStage == PlantLifeStage.Sowing || plant.Growth < MinPlantGrowth)
            {
                return;
            }
            Room room = plant.GetRoom();
            if (room == null || room.UsesOutdoorTemperature)
            {
                return;
            }
            EnsureArrays();
            float scale = plant.Growth;
            float o2 = PlantO2PerCycle * scale;
            float co2 = PlantCo2PerCycle * scale;
            IntVec3 cell = plant.Position;
            if (RoomIsColonyBuiltCached(room))
            {
                atmosphere.ConsumeBreathGasFromRoom(room, StrataGasDefOf.Strata_CarbonDioxide, co2);
                atmosphere.AddBreathGasToRoom(room, StrataGasDefOf.Strata_Oxygen, o2, cell);
            }
            else
            {
                ConsumeCellCo2(cell, co2);
                AddCellO2(cell, o2);
            }
        }

        private void ProcessBreathPlantsBatched(AtmosphereMapComponent atmosphere)
        {
            List<Thing> plants = map.listerThings.ThingsInGroup(ThingRequestGroup.Plant);
            if (plants == null || plants.Count == 0)
            {
                return;
            }
            int batch = StrataLevelPerfUtility.BreathPlantsPerBatch(map);
            int end = Mathf.Min(plants.Count, plantBatchCursor + batch);
            for (int i = plantBatchCursor; i < end; i++)
            {
                if (plants[i] is Plant plant)
                {
                    ProcessPlant(plant, atmosphere);
                }
            }
            plantBatchCursor = end >= plants.Count ? 0 : end;
        }

        public float DensityAt(IntVec3 cell, StrataGasDef gas)
        {
            if (!cell.IsValid || !cell.InBounds(map) || gas == null)
            {
                return 0f;
            }
            EnsureArrays();
            int index = map.cellIndices.CellToIndex(cell);
            if (gas == StrataGasDefOf.Strata_Oxygen)
            {
                return o2[index];
            }
            if (gas == StrataGasDefOf.Strata_CarbonDioxide)
            {
                return co2[index];
            }
            return 0f;
        }

        public float AverageInRoom(Room room, StrataGasDef gas)
        {
            if (room == null || gas == null || room.CellCount <= 0)
            {
                return 0f;
            }
            EnsureArrays();
            float sum = 0f;
            int count = 0;
            foreach (IntVec3 cell in room.Cells)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                int index = map.cellIndices.CellToIndex(cell);
                sum += gas == StrataGasDefOf.Strata_Oxygen ? o2[index] : co2[index];
                count++;
            }
            return count > 0 ? sum / count : 0f;
        }

        public void PaintCellDensity(float[][] cellDensity, StrataGasDef gas)
        {
            if (cellDensity == null || gas == null || o2 == null || co2 == null)
            {
                return;
            }
            int gasIndex = gas.index;
            if (gasIndex < 0 || gasIndex >= cellDensity.Length)
            {
                return;
            }
            cellDensity[gasIndex] ??= new float[map.cellIndices.NumGridCells];
            float[] plane = cellDensity[gasIndex];
            bool isO2 = gas == StrataGasDefOf.Strata_Oxygen;
            float[] source = isO2 ? o2 : co2;
            int len = Mathf.Min(plane.Length, source.Length);
            for (int i = 0; i < len; i++)
            {
                float value = source[i];
                if (value <= 0f)
                {
                    continue;
                }
                IntVec3 cell = map.cellIndices.IndexToCell(i);
                if (cell.InBounds(map) && !cell.Fogged(map))
                {
                    plane[i] = value;
                }
            }
        }

        private void ApplyNaturalRoofAmbientBatched()
        {
            IReadOnlyList<Room> rooms = map.regionGrid.AllRooms;
            if (rooms.Count == 0)
            {
                roomBatchCursor = 0;
                return;
            }
            if (roomBatchCursor >= rooms.Count)
            {
                roomBatchCursor = 0;
            }
            int batch = StrataLevelPerfUtility.BreathRoomsPerBatch(map);
            int end = Mathf.Min(rooms.Count, roomBatchCursor + batch);
            for (int i = roomBatchCursor; i < end; i++)
            {
                if (i >= rooms.Count)
                {
                    break;
                }
                Room room = rooms[i];
                if (room == null || room.IsDoorway || RoomIsColonyBuiltCached(room))
                {
                    continue;
                }
                foreach (IntVec3 cell in room.Cells)
                {
                    if (!cell.InBounds(map) || !map.roofGrid.Roofed(cell))
                    {
                        continue;
                    }
                    if (!RoofCollapseUtility.ConnectedToRoofHolder(cell, map, assumeRoofAtRoot: false))
                    {
                        continue;
                    }
                    int index = map.cellIndices.CellToIndex(cell);
                    AtmosphericMix.TargetMix target = AtmosphericMix.TargetForMap(map);
                    o2[index] = Mathf.Lerp(o2[index], target.oxygen, RoofAmbientRate);
                    co2[index] = Mathf.Lerp(co2[index], target.carbonDioxide, RoofAmbientRate * 0.35f);
                }
            }
            roomBatchCursor = end >= rooms.Count ? 0 : end;
        }

        private void DiffuseOpenCavern()
        {
            scratchO2 ??= new float[o2.Length];
            scratchCo2 ??= new float[co2.Length];
            System.Array.Copy(o2, scratchO2, o2.Length);
            System.Array.Copy(co2, scratchCo2, co2.Length);
            RefreshColonyBuiltCacheIfNeeded();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                Room room = cell.GetRoom(map);
                if (room != null && colonyBuiltRoomIds.Contains(room.ID))
                {
                    continue;
                }
                int index = map.cellIndices.CellToIndex(cell);
                float neighborO2 = 0f;
                float neighborCo2 = 0f;
                int neighbors = 0;
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + offset;
                    if (!next.InBounds(map))
                    {
                        continue;
                    }
                    int nextIndex = map.cellIndices.CellToIndex(next);
                    neighborO2 += o2[nextIndex];
                    neighborCo2 += co2[nextIndex];
                    neighbors++;
                }
                if (neighbors == 0)
                {
                    continue;
                }
                neighborO2 /= neighbors;
                neighborCo2 /= neighbors;
                o2[index] = Mathf.Lerp(scratchO2[index], neighborO2, DiffusionRate);
                co2[index] = Mathf.Lerp(scratchCo2[index], neighborCo2, DiffusionRate);
            }
            ApplyBuoyancyBias();
        }

        // O₂ rises, CO₂ sinks — cheap vertical nudge in open caverns only.
        private void ApplyBuoyancyBias()
        {
            RefreshColonyBuiltCacheIfNeeded();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                Room room = cell.GetRoom(map);
                if (room != null && colonyBuiltRoomIds.Contains(room.ID))
                {
                    continue;
                }
                int index = map.cellIndices.CellToIndex(cell);
                float upO2 = 0f;
                float downCo2 = 0f;
                int upCount = 0;
                int downCount = 0;
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + offset;
                    if (!next.InBounds(map))
                    {
                        continue;
                    }
                    if (next.z > cell.z)
                    {
                        int nextIndex = map.cellIndices.CellToIndex(next);
                        upO2 += o2[nextIndex];
                        upCount++;
                    }
                    else if (next.z < cell.z)
                    {
                        int nextIndex = map.cellIndices.CellToIndex(next);
                        downCo2 += co2[nextIndex];
                        downCount++;
                    }
                }
                if (upCount > 0)
                {
                    o2[index] = Mathf.Lerp(o2[index], upO2 / upCount, BuoyancyBias);
                }
                if (downCount > 0)
                {
                    co2[index] = Mathf.Lerp(co2[index], downCo2 / downCount, BuoyancyBias);
                }
            }
        }

        private void EqualizeColonySealedRoomsBatched(AtmosphereMapComponent atmosphere)
        {
            StrataGasDef oxygenDef = StrataGasDefOf.Strata_Oxygen;
            StrataGasDef co2Def = StrataGasDefOf.Strata_CarbonDioxide;
            IReadOnlyList<Room> rooms = map.regionGrid.AllRooms;
            if (rooms.Count == 0)
            {
                roomBatchCursor = 0;
                return;
            }
            if (roomBatchCursor >= rooms.Count)
            {
                roomBatchCursor = 0;
            }
            int batch = StrataLevelPerfUtility.BreathRoomsPerBatch(map);
            int end = Mathf.Min(rooms.Count, roomBatchCursor + batch);
            for (int i = roomBatchCursor; i < end; i++)
            {
                if (i >= rooms.Count)
                {
                    break;
                }
                Room room = rooms[i];
                if (!RoomIsColonyBuiltCached(room))
                {
                    continue;
                }
                float avgO2 = atmosphere.GetBreathRoomDensity(room, oxygenDef);
                float avgCo2 = atmosphere.GetBreathRoomDensity(room, co2Def);
                foreach (IntVec3 cell in room.Cells)
                {
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }
                    int index = map.cellIndices.CellToIndex(cell);
                    o2[index] = avgO2;
                    co2[index] = avgCo2;
                }
            }
            roomBatchCursor = end >= rooms.Count ? 0 : end;
        }

        private void SyncRoomCloudsBatched(AtmosphereMapComponent atmosphere)
        {
            IReadOnlyList<Room> rooms = map.regionGrid.AllRooms;
            if (rooms.Count == 0)
            {
                roomBatchCursor = 0;
                return;
            }
            if (roomBatchCursor >= rooms.Count)
            {
                roomBatchCursor = 0;
            }
            int batch = StrataLevelPerfUtility.BreathRoomsPerBatch(map);
            int end = Mathf.Min(rooms.Count, roomBatchCursor + batch);
            for (int i = roomBatchCursor; i < end; i++)
            {
                if (i >= rooms.Count)
                {
                    break;
                }
                Room room = rooms[i];
                if (room == null || room.IsDoorway)
                {
                    continue;
                }
                if (!TryGetSampleCell(room, out IntVec3 sample))
                {
                    continue;
                }
                float avgO2 = AverageInRoom(room, StrataGasDefOf.Strata_Oxygen);
                float avgCo2 = AverageInRoom(room, StrataGasDefOf.Strata_CarbonDioxide);
                atmosphere.SetBreathRoomDensity(room, StrataGasDefOf.Strata_Oxygen, avgO2, sample);
                atmosphere.SetBreathRoomDensity(room, StrataGasDefOf.Strata_CarbonDioxide, avgCo2, sample);
            }
            roomBatchCursor = end >= rooms.Count ? 0 : end;
        }

        private static bool TryGetSampleCell(Room room, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            Region region = room?.FirstRegion;
            if (region == null)
            {
                return false;
            }
            cell = region.AnyCell;
            return cell.IsValid;
        }

        private void EnsureArrays()
        {
            int cells = map.cellIndices.NumGridCells;
            if (o2 == null || o2.Length != cells)
            {
                o2 = new float[cells];
                co2 = new float[cells];
                scratchO2 = null;
                scratchCo2 = null;
                skipDiffusionCell = null;
                colonyBuiltRoomIds = null;
                colonyBuiltCacheTick = -1;
            }
        }

        public void AddO2(IntVec3 cell, float amount) => AddCellO2(cell, amount);

        public void AddCo2(IntVec3 cell, float amount) => AddCellCo2(cell, amount);

        private void AddCellO2(IntVec3 cell, float amount)
        {
            if (amount <= 0f || !cell.InBounds(map))
            {
                return;
            }
            EnsureArrays();
            int index = map.cellIndices.CellToIndex(cell);
            o2[index] = Mathf.Min(1f, o2[index] + amount);
        }

        private void AddCellCo2(IntVec3 cell, float amount)
        {
            if (amount <= 0f || !cell.InBounds(map))
            {
                return;
            }
            EnsureArrays();
            int index = map.cellIndices.CellToIndex(cell);
            co2[index] = Mathf.Min(1f, co2[index] + amount);
        }

        private void ConsumeCellO2(IntVec3 cell, float amount)
        {
            if (amount <= 0f || !cell.InBounds(map))
            {
                return;
            }
            EnsureArrays();
            int index = map.cellIndices.CellToIndex(cell);
            o2[index] = Mathf.Max(0f, o2[index] - amount);
        }

        private void ConsumeCellCo2(IntVec3 cell, float amount)
        {
            if (amount <= 0f || !cell.InBounds(map))
            {
                return;
            }
            EnsureArrays();
            int index = map.cellIndices.CellToIndex(cell);
            co2[index] = Mathf.Max(0f, co2[index] - amount);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MultiFloors.Jobs;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace MultiFloors.Gravships;

[HotSwappable]
public static class MF_GravshipUtility
{
	public static bool IsOnSubstructure(this IntVec3 cell, Map map)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (!ModsConfig.OdysseyActive)
		{
			return false;
		}
		bool? obj;
		if (map == null)
		{
			obj = null;
		}
		else
		{
			TerrainDef obj2 = map.terrainGrid.FoundationAt(cell);
			obj = ((obj2 != null) ? new bool?(obj2.IsSubstructure) : ((bool?)null));
		}
		bool? flag = obj;
		return flag == true;
	}

	public static bool HasEntranceInGroundValidSubstructure(Building_GravEngine baseEngine, Map curMap)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		MF_LevelMapComp mF_LevelMapComp = ((Thing)baseEngine).Map.GroundMap()?.LevelMapComp();
		if (mF_LevelMapComp == null)
		{
			return false;
		}
		if (!mF_LevelMapComp.ValidStairsOnLevel.TryGetValue(curMap.Level(), out var value))
		{
			return false;
		}
		foreach (Stair item in value)
		{
			if (item is StairEntrance && baseEngine.ValidSubstructureNoRegen.Contains(((Thing)item).Position))
			{
				return true;
			}
		}
		return false;
	}

	public static void PlaceSubGravshipInMap(SubGravship subGravship, IntVec3 root, Map map, bool isTopDeck, out List<Thing> spawned)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		spawned = null;
		spawned = subGravship.ThingPlacements.Select((KeyValuePair<Thing, Data> x) => x.Key).ToList();
		GenCollection.SortBy<Thing, int>(spawned, (Func<Thing, int>)GravshipUtility.ThingSpawnPriority);
		DisableDirtyingScope val = map.pathing.DisableIncrementalScope();
		try
		{
			SpawnFoundations(subGravship, map, root);
			SpawnTerrain(subGravship, map, root);
			SpawnGas(subGravship, map, root);
			CopyZonesIntoMap(subGravship, map, root);
			CopyDesignations(subGravship, map, root);
			SpawnNonPawnThings(subGravship, map, spawned, root, isTopDeck);
			SpawnPawns(subGravship, map, root);
			SpawnRoofs(subGravship, map, root);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		CopyAreasIntoMap(subGravship, map, root);
		UpdateBillDestinations(map);
		PowerBuildings(subGravship);
		subGravship.StoryState.CopyTo(map.storyState);
		map.autoSlaughterManager.configs = subGravship.autoSlaughterConfigs;
		map.autoSlaughterManager.Notify_ConfigChanged();
		map.substructureGrid.MarkDirty();
		Current.Game.GetComponent<MF_GravshipManager>().RemoveSubGravship(subGravship);
	}

	public static void PostPlacementSetup(SubGravship subGravship, Map newMap, IntVec3 root, List<Thing> spawned)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		ApplyTemperatureVacuumFromBase(subGravship, root, newMap);
		newMap.listerFilthInHomeArea.RebuildAll();
		newMap.resourceCounter.UpdateResourceCounts();
		PostSwapMap(subGravship, spawned);
	}

	public static void ApplyTemperatureVacuumFromBase(SubGravship subGravship, IntVec3 root, Map map)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (!ModsConfig.OdysseyActive || subGravship == null)
		{
			return;
		}
		foreach (Data roomTemperature in subGravship.RoomTemperatures)
		{
			Room room = GridsUtility.GetRoom(root + roomTemperature.local, map);
			if (room != null && !room.UsesOutdoorTemperature)
			{
				room.Temperature = roomTemperature.temperature;
				room.Vacuum = (room.ExposedToSpace ? 1f : roomTemperature.vacuum);
			}
		}
	}

	public static HashSet<IntVec3> GetCellsAdjacentToSubstructure(IEnumerable<CellRect> occupiedRects, int expansion = 1)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (!ModsConfig.OdysseyActive)
		{
			return null;
		}
		HashSet<IntVec3> hashSet = new HashSet<IntVec3>();
		foreach (CellRect occupiedRect in occupiedRects)
		{
			CellRect current = occupiedRect;
			CellRect val = ((CellRect)(ref current)).ExpandedBy(expansion);
			for (int i = val.minX; i <= val.maxX; i++)
			{
				for (int j = val.minZ; j <= val.maxZ; j++)
				{
					hashSet.Add(new IntVec3(i, 0, j));
				}
			}
		}
		return hashSet;
	}

	private static void SpawnFoundations(SubGravship subGravship, Map map, IntVec3 root)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		SpawnTerrain(map, root, subGravship.Foundations, clear: true);
	}

	private static void SpawnTerrain(SubGravship subGravship, Map map, IntVec3 root)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		SpawnTerrain(map, root, subGravship.Terrains);
		foreach (var terrainColor in subGravship.TerrainColors)
		{
			IntVec3 item = terrainColor.Item1;
			ColorDef item2 = terrainColor.Item2;
			IntVec3 val = root + item;
			if (GenGrid.InBounds(val, map))
			{
				map.terrainGrid.SetTerrainColor(val, item2);
			}
		}
	}

	private static void SpawnPawns(SubGravship subGravship, Map map, IntVec3 root)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		Pawn val = default(Pawn);
		Data val2 = default(Data);
		foreach (KeyValuePair<Pawn, Data> pawnPlacement in subGravship.PawnPlacements)
		{
			pawnPlacement.Deconstruct(ref val, ref val2);
			Pawn val3 = val;
			Data val4 = val2;
			if (((Thing)val3).Discarded || ((Thing)val3).Destroyed)
			{
				break;
			}
			GenSpawn.Spawn((Thing)(object)val3, root + val4.local, map, val4.rotation, (WipeMode)0, false, false);
			if (val3.drafter != null)
			{
				val3.drafter.Drafted = val4.drafted;
			}
			if (val3.Downed || val3.Deathresting)
			{
				Building_Bed val5 = map.thingGrid.ThingAt<Building_Bed>(((Thing)val3).Position);
				if (val5 != null)
				{
					RestUtility.TuckIntoBed(val5, val3, val3, false);
				}
			}
			val3.pather.ResetToCurrentPosition();
			if (ChangingLevelJobDefs.IsChangingLevel(val3.CurJobDef))
			{
				val3.jobs.StopAll(false, true);
			}
		}
	}

	private static void SpawnNonPawnThings(SubGravship subGravship, Map map, List<Thing> gravshipThings, IntVec3 root, bool isTopDeck)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		foreach (Thing gravshipThing in gravshipThings)
		{
			if (!isTopDeck || !(gravshipThing is StairEntrance))
			{
				Data val = subGravship.ThingPlacements[gravshipThing];
				if (!gravshipThing.Destroyed)
				{
					Notify_TransportedOnSubGravship((PawnFlyer)(object)((gravshipThing is PawnFlyer) ? gravshipThing : null), subGravship);
					GenSpawn.Spawn(gravshipThing, root + val.local, map, val.rotation, (WipeMode)0, false, false);
				}
			}
		}
	}

	private static void Notify_TransportedOnSubGravship(PawnFlyer flyer, SubGravship subGravship)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (flyer != null)
		{
			IntVec3 val = ((Thing)subGravship.Engine).Position - subGravship.originalPosition;
			flyer.startVec += ((IntVec3)(ref val)).ToVector3();
			flyer.destCell += val;
			flyer.positionLastComputedTick = -1;
		}
	}

	private static void SpawnRoofs(SubGravship subGravship, Map map, IntVec3 root)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		foreach (var (val, val2) in subGravship.Roofs)
		{
			map.roofGrid.SetRoof(root + val, val2);
		}
	}

	private static void SpawnGas(SubGravship subGravship, Map map, IntVec3 root)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		foreach (var (val, num) in subGravship.Gases)
		{
			map.gasGrid.SetDirect(root + val, num);
			map.mapDrawer.MapMeshDirty(root + val, MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Gas));
		}
	}

	private static void PowerBuildings(SubGravship subGravship)
	{
		Thing val = default(Thing);
		Thing val2 = default(Thing);
		foreach (KeyValuePair<Thing, Thing> connectParent in subGravship.ConnectParents)
		{
			connectParent.Deconstruct(ref val, ref val2);
			Thing val3 = val;
			Thing val4 = val2;
			if (val4 != null && val4.Spawned)
			{
				ThingCompUtility.TryGetComp<CompPower>(val3).ConnectToTransmitter(ThingCompUtility.TryGetComp<CompPower>(val4), false);
			}
		}
		bool flag = default(bool);
		foreach (KeyValuePair<Thing, bool> item in subGravship.PoweredOn)
		{
			item.Deconstruct(ref val2, ref flag);
			Thing obj = val2;
			bool powerOn = flag;
			ThingCompUtility.TryGetComp<CompPowerTrader>(obj).PowerOn = powerOn;
		}
	}

	internal static void PostSwapMap(SubGravship subGravship, List<Thing> things)
	{
		foreach (Thing thing in things)
		{
			if (thing != null && !thing.Destroyed)
			{
				thing.PostSwapMap();
			}
		}
		foreach (Pawn pawn in subGravship.Pawns)
		{
			if (pawn != null && !((Thing)pawn).Destroyed)
			{
				((Thing)pawn).PostSwapMap();
			}
		}
	}

	private static void SpawnTerrain(Map map, IntVec3 root, Dictionary<IntVec3, TerrainDef> data, bool clear = false)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		IntVec3 val = default(IntVec3);
		TerrainDef val2 = default(TerrainDef);
		foreach (KeyValuePair<IntVec3, TerrainDef> datum in data)
		{
			datum.Deconstruct(ref val, ref val2);
			IntVec3 val3 = val;
			TerrainDef val4 = val2;
			IntVec3 val5 = root + val3;
			if (val4 == null || !GenGrid.InBounds(val5, map))
			{
				continue;
			}
			if (clear)
			{
				ClearThingsAt(val5, map, (ClearMode)0);
			}
			map.snowGrid.SetDepth(val5, 0f);
			map.sandGrid.SetDepth(val5, 0f);
			if (!val4.isFoundation && !GenGrid.SupportsStructureType(val5, map, ((BuildableDef)val4).terrainAffordanceNeeded))
			{
				map.terrainGrid.SetTerrain(val5, map.Biome.TerrainForAffordance(((BuildableDef)val4).terrainAffordanceNeeded));
			}
			if (val4.isFoundation)
			{
				if (map.terrainGrid.CanRemoveTopLayerAt(val5))
				{
					map.terrainGrid.RemoveTopLayer(val5, false);
				}
				map.terrainGrid.SetFoundation(val5, val4);
			}
			else
			{
				map.terrainGrid.SetTerrain(val5, val4);
			}
			map.fogGrid.FloodUnfogAdjacent(val5, false);
		}
	}

	public static void ClearAreaForGravship(Map map, IntVec3 root, HashSet<IntVec3> clearCells)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		ClearArea(map, root, clearCells, (ClearMode)1);
	}

	public static void ClearArea(Map map, IntVec3 root, HashSet<IntVec3> clearCells, ClearMode mode)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if (!ModsConfig.OdysseyActive)
		{
			return;
		}
		bool flag = default(bool);
		MapGenerator.TryGetVar<bool>("DontGenerateClearedGravShipTerrain", ref flag);
		if (flag)
		{
			return;
		}
		foreach (IntVec3 clearCell in clearCells)
		{
			IntVec3 val = root + clearCell;
			if (!GenGrid.InBounds(val, map))
			{
				continue;
			}
			if ((int)mode <= 1)
			{
				if (ModsConfig.BiotechActive)
				{
					map.pollutionGrid.SetPolluted(val, false, true);
				}
				if (!GenGrid.SupportsStructureType(val, map, TerrainAffordanceDefOf.Walkable))
				{
					TerrainDef replacementTerrain = GetReplacementTerrain(GridsUtility.GetTerrain(val, map), map);
					if (replacementTerrain != null)
					{
						map.terrainGrid.SetTerrain(val, replacementTerrain);
					}
				}
			}
			ClearThingsAt(val, map, mode);
			if (ShouldRemoveRoof(map, val, mode))
			{
				map.roofGrid.SetRoof(val, (RoofDef)null);
			}
			if (map.roofGrid.RoofAt(val) == null)
			{
				map.fogGrid.FloodUnfogAdjacent(val, false);
			}
		}
	}

	private static bool ShouldRemoveRoof(Map map, IntVec3 cell, ClearMode mode)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if ((int)mode != 3)
		{
			return true;
		}
		Room room = GridsUtility.GetRoom(cell, map);
		if (room != null && room.ProperRoom)
		{
			return false;
		}
		Building edifice = GridsUtility.GetEdifice(cell, map);
		if (edifice != null && !((Thing)edifice).def.building.isNaturalRock)
		{
			return false;
		}
		return map.roofGrid.RoofAt(cell) != RoofDefOf.RoofConstructed;
	}

	private static TerrainDef GetReplacementTerrain(TerrainDef original, Map map)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		if (original == TerrainDefOf.Space)
		{
			return null;
		}
		if (original.gravshipReplacementTerrain != null)
		{
			return original.gravshipReplacementTerrain;
		}
		if ((int)((BuildableDef)original).passability == 2 || original.dangerous)
		{
			return map.Biome.TerrainForAffordance(TerrainAffordanceDefOf.Medium);
		}
		return null;
	}

	private static void ClearThingsAt(IntVec3 cell, Map map, ClearMode clearMode)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected I4, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		Zone obj = map.zoneManager.ZoneAt(cell);
		if (obj != null)
		{
			obj.RemoveCell(cell);
		}
		((Area)map.areaManager.BuildRoof)[cell] = false;
		((Area)map.areaManager.NoRoof)[cell] = false;
		List<Thing> list = map.thingGrid.ThingsListAt(cell);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Thing val = list[num];
			if (val.def.destroyable && !val.Destroyed)
			{
				switch ((int)clearMode)
				{
				case 0:
					val.Destroy((DestroyMode)0);
					break;
				case 1:
					if (val.def.IsPlant)
					{
						PlantProperties plant = val.def.plant;
						if (plant == null || !plant.IsTree)
						{
							break;
						}
					}
					val.Destroy((DestroyMode)0);
					break;
				case 3:
				{
					BuildingProperties building2 = val.def.building;
					if (building2 != null && building2.isNaturalRock)
					{
						val.Destroy((DestroyMode)0);
					}
					break;
				}
				case 2:
				{
					BuildingProperties building = val.def.building;
					if (building != null && !building.canLandGravshipOn)
					{
						val.Destroy((DestroyMode)0);
					}
					break;
				}
				default:
					throw new ArgumentOutOfRangeException("clearMode", clearMode, null);
				}
			}
		}
	}

	private static void CopyZonesIntoMap(SubGravship subGravship, Map map, IntVec3 root)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		foreach (VirtualMoveableStockpile stockpile in subGravship.areas.Stockpiles)
		{
			stockpile.TryCreateStockpile(map.zoneManager, root);
		}
		foreach (VirtualMoveableGrowZone growZone in subGravship.areas.GrowZones)
		{
			growZone.TryCreateGrowZone(map.zoneManager, root);
		}
	}

	private static void CopyAreasIntoMap(SubGravship subGravship, Map map, IntVec3 root)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		foreach (VirtualMoveableArea_Allowed allowedArea in subGravship.areas.AllowedAreas)
		{
			allowedArea.TryCreateArea(map.areaManager, root);
		}
		if (subGravship.areas.HomeArea != null)
		{
			foreach (IntVec3 relativeCell in subGravship.areas.HomeArea.RelativeCells)
			{
				((Area)map.areaManager.Home)[root + relativeCell] = true;
			}
			foreach (Pawn assignedPawn in subGravship.areas.HomeArea.AssignedPawns)
			{
				assignedPawn.playerSettings.AreaRestrictionInPawnCurrentMap = (Area)(object)map.areaManager.Home;
			}
		}
		if (subGravship.areas.BuildRoofArea != null)
		{
			foreach (IntVec3 relativeCell2 in subGravship.areas.BuildRoofArea.RelativeCells)
			{
				((Area)map.areaManager.BuildRoof)[root + relativeCell2] = true;
			}
		}
		if (subGravship.areas.NoRoofArea != null)
		{
			foreach (IntVec3 relativeCell3 in subGravship.areas.NoRoofArea.RelativeCells)
			{
				((Area)map.areaManager.NoRoof)[root + relativeCell3] = true;
			}
		}
		if (subGravship.areas.SnowClearArea != null)
		{
			foreach (IntVec3 relativeCell4 in subGravship.areas.SnowClearArea.RelativeCells)
			{
				((Area)map.areaManager.SnowOrSandClear)[root + relativeCell4] = true;
			}
		}
		if (ModsConfig.BiotechActive && subGravship.areas.PollutionClearArea != null)
		{
			foreach (IntVec3 relativeCell5 in subGravship.areas.PollutionClearArea.RelativeCells)
			{
				((Area)map.areaManager.PollutionClear)[root + relativeCell5] = true;
			}
		}
		foreach (MoveableStorageGroup storageGroup in subGravship.areas.StorageGroups)
		{
			storageGroup.TryCreateStorageGroup(map);
		}
	}

	private static void UpdateBillDestinations(Map map)
	{
		foreach (Bill item in BillUtility.GlobalBills())
		{
			foreach (StorageGroup item2 in map.storageGroups.StorageGroupsForReading)
			{
				ISlotGroup slotGroup = item.GetSlotGroup();
				StorageGroup val = (StorageGroup)(object)((slotGroup is StorageGroup) ? slotGroup : null);
				if (val == null || val.RenamableLabel != item2.RenamableLabel)
				{
					continue;
				}
				item.SetStoreMode(BillStoreModeDefOf.SpecificStockpile, (ISlotGroup)(object)item2);
				goto IL_0129;
			}
			foreach (Zone allZone in map.zoneManager.AllZones)
			{
				Zone_Stockpile val2 = (Zone_Stockpile)(object)((allZone is Zone_Stockpile) ? allZone : null);
				if (val2 == null)
				{
					continue;
				}
				ISlotGroup slotGroup2 = item.GetSlotGroup();
				SlotGroup val3 = (SlotGroup)(object)((slotGroup2 is SlotGroup) ? slotGroup2 : null);
				if (val3 != null)
				{
					ISlotGroupParent parent = val3.parent;
					Zone val4 = (Zone)(object)((parent is Zone) ? parent : null);
					if (val4 != null && val4.RenamableLabel == allZone.RenamableLabel)
					{
						item.SetStoreMode(BillStoreModeDefOf.SpecificStockpile, (ISlotGroup)(object)val2.GetSlotGroup());
						goto IL_0129;
					}
				}
			}
			if (item.GetStoreMode() == BillStoreModeDefOf.SpecificStockpile)
			{
				item.SetStoreMode(BillStoreModeDefOf.DropOnFloor, (ISlotGroup)null);
			}
			IL_0129:;
		}
	}

	private static void CopyDesignations(SubGravship subGravship, Map map, IntVec3 root)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		foreach (var (val, val2) in subGravship.TerrainDesignations)
		{
			if (val2 != null)
			{
				val2.target = LocalTargetInfo.op_Implicit(root + val);
				map.designationManager.AddDesignation(val2);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MultiFloors.Gravships;

[HotSwappable]
public class SubGravship : IExposable, ILoadReferenceable
{
	[Unsaved(false)]
	public IntVec3 originalPosition;

	public Vector3 engineToCenter;

	public VirtualMoveableAreas areas;

	public List<AutoSlaughterConfig> autoSlaughterConfigs;

	private int uniqueID;

	private int level;

	private Rot4 rotation;

	private Building_SubGravEngine engine;

	private Dictionary<Thing, PositionData> things;

	private Dictionary<Pawn, PositionData> pawns;

	private Dictionary<Thing, bool> powerOn;

	private Dictionary<Thing, Thing> connectParents;

	private Dictionary<IntVec3, RoofDef> roofs;

	private Dictionary<IntVec3, TerrainDef> foundations;

	private Dictionary<IntVec3, TerrainDef> terrains;

	private Dictionary<IntVec3, ColorDef> terrainColors;

	private Dictionary<IntVec3, Designation> terrainDesignations;

	private Dictionary<IntVec3, uint> gases;

	private List<RoomTemperatureVacuum> roomTemperatures;

	private VirtualStoryState storyState;

	private List<Thing> tmpPoweredThings = new List<Thing>();

	private List<bool> tmpPoweredThingsOn = new List<bool>();

	private List<Thing> tmpConnectedThings = new List<Thing>();

	private List<Thing> tmpConnectedParents = new List<Thing>();

	private static readonly Dictionary<Faction, int> tmpFactionMembersKidnappedCount = new Dictionary<Faction, int>();

	private Dictionary<Thing, Data> tmpThingPlacements;

	private Rot4 tmpThingsRot;

	private Dictionary<Pawn, Data> tmpPawns;

	private Rot4 tmpPawnsRot;

	private Dictionary<IntVec3, TerrainDef> tmpFoundations;

	private Rot4 tmpFoundationRot;

	private Dictionary<IntVec3, TerrainDef> tmpTerrains;

	private Rot4 tmpTerrainRot;

	public int Level => level;

	public IEnumerable<Pawn> Pawns => pawns.Keys;

	public IEnumerable<Thing> Things => things.Keys;

	public VirtualStoryState StoryState => storyState;

	public Rot4 Rotation
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return rotation;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			rotation = value;
		}
	}

	public Building_SubGravEngine Engine => engine;

	public Dictionary<Thing, bool> PoweredOn => powerOn;

	public Dictionary<Thing, Thing> ConnectParents => connectParents;

	public Dictionary<Thing, Data> ThingPlacements => GetPlacementValues<Thing>(things, ref tmpThingPlacements, ref tmpThingsRot);

	public Dictionary<Pawn, Data> PawnPlacements => GetPlacementValues<Pawn>(pawns, ref tmpPawns, ref tmpPawnsRot);

	public Dictionary<IntVec3, TerrainDef> Foundations => GetRotatedValues(foundations, ref tmpFoundations, ref tmpFoundationRot);

	public Dictionary<IntVec3, TerrainDef> Terrains => GetRotatedValues(terrains, ref tmpTerrains, ref tmpTerrainRot);

	public IEnumerable<(IntVec3, RoofDef)> Roofs => GetRotatedValues(roofs);

	public IEnumerable<(IntVec3, ColorDef)> TerrainColors => GetRotatedValues(terrainColors);

	public IEnumerable<(IntVec3, Designation)> TerrainDesignations => GetRotatedValues(terrainDesignations);

	public IEnumerable<(IntVec3, uint)> Gases => GetRotatedValues(gases);

	public IEnumerable<Data> RoomTemperatures
	{
		get
		{
			foreach (RoomTemperatureVacuum roomTemperature in roomTemperatures)
			{
				yield return new Data
				{
					local = PrefabUtility.GetAdjustedLocalPosition(roomTemperature.roomCell, rotation),
					temperature = roomTemperature.temperature,
					vacuum = roomTemperature.vacuum
				};
			}
		}
	}

	public SubGravship()
	{
		things = new Dictionary<Thing, PositionData>();
		pawns = new Dictionary<Pawn, PositionData>();
		powerOn = new Dictionary<Thing, bool>();
		connectParents = new Dictionary<Thing, Thing>();
		roofs = new Dictionary<IntVec3, RoofDef>();
		foundations = new Dictionary<IntVec3, TerrainDef>();
		terrains = new Dictionary<IntVec3, TerrainDef>();
		terrainColors = new Dictionary<IntVec3, ColorDef>();
		terrainDesignations = new Dictionary<IntVec3, Designation>();
		gases = new Dictionary<IntVec3, uint>();
		areas = new VirtualMoveableAreas();
		roomTemperatures = new List<RoomTemperatureVacuum>();
	}

	public SubGravship(Building_SubGravEngine engine)
		: this()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		if (ModsConfig.OdysseyActive)
		{
			uniqueID = Current.Game.GetComponent<MF_GravshipManager>().GetSubGravshipUniqueID();
			this.engine = engine;
			Map map = ((Thing)engine).Map;
			level = map.Level();
			originalPosition = ((Thing)engine).Position;
			HashSet<IntVec3> validSubstructure = GetValidSubstructure(engine, map);
			storyState = new VirtualStoryState();
			storyState.CopyFrom(map.storyState);
			autoSlaughterConfigs = map.autoSlaughterManager.configs;
			TransferZones(map, originalPosition, validSubstructure);
			CopyCellContents(map, originalPosition, validSubstructure);
			CopyAreas(map, originalPosition, validSubstructure);
			CopyStorageGroups(map);
			CheckAffectedGoodwill();
			CellRect val = CellRect.FromCellList((IEnumerable<IntVec3>)engine.ValidSubstructure);
			engineToCenter = ((CellRect)(ref val)).CenterVector3 - ((IntVec3)(ref originalPosition)).ToVector3();
		}
	}

	public string GetUniqueLoadID()
	{
		return "MF_SubGravship_" + uniqueID;
	}

	public void ExposeData()
	{
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Invalid comparison between Unknown and I4
		Scribe_Values.Look<int>(ref uniqueID, "mf_subGravshipUniqueID", 0, false);
		Scribe_Values.Look<int>(ref level, "mf_subGravshipLevel", 0, false);
		Dictionary<IntVec3, RoofDef> dictionary = roofs;
		if (dictionary != null)
		{
			GenCollection.RemoveAll<IntVec3, RoofDef>(dictionary, (Predicate<KeyValuePair<IntVec3, RoofDef>>)((KeyValuePair<IntVec3, RoofDef> x) => x.Value == null));
		}
		Dictionary<IntVec3, TerrainDef> dictionary2 = foundations;
		if (dictionary2 != null)
		{
			GenCollection.RemoveAll<IntVec3, TerrainDef>(dictionary2, (Predicate<KeyValuePair<IntVec3, TerrainDef>>)((KeyValuePair<IntVec3, TerrainDef> x) => x.Value == null));
		}
		Dictionary<IntVec3, TerrainDef> dictionary3 = terrains;
		if (dictionary3 != null)
		{
			GenCollection.RemoveAll<IntVec3, TerrainDef>(dictionary3, (Predicate<KeyValuePair<IntVec3, TerrainDef>>)((KeyValuePair<IntVec3, TerrainDef> x) => x.Value == null));
		}
		Dictionary<IntVec3, ColorDef> dictionary4 = terrainColors;
		if (dictionary4 != null)
		{
			GenCollection.RemoveAll<IntVec3, ColorDef>(dictionary4, (Predicate<KeyValuePair<IntVec3, ColorDef>>)((KeyValuePair<IntVec3, ColorDef> x) => x.Value == null));
		}
		Dictionary<IntVec3, Designation> dictionary5 = terrainDesignations;
		if (dictionary5 != null)
		{
			GenCollection.RemoveAll<IntVec3, Designation>(dictionary5, (Predicate<KeyValuePair<IntVec3, Designation>>)((KeyValuePair<IntVec3, Designation> x) => x.Value == null));
		}
		Scribe_References.Look<Building_SubGravEngine>(ref engine, "engine", false);
		Scribe_Collections.Look<Thing, PositionData>(ref things, "things", (LookMode)2, (LookMode)2);
		Scribe_Collections.Look<Pawn, PositionData>(ref pawns, "pawns", (LookMode)2, (LookMode)2);
		Scribe_Collections.Look<Thing, bool>(ref powerOn, "powerOn", (LookMode)3, (LookMode)1, ref tmpPoweredThings, ref tmpPoweredThingsOn, true, false, false);
		Scribe_Collections.Look<Thing, Thing>(ref connectParents, "connectParents", (LookMode)3, (LookMode)3, ref tmpConnectedThings, ref tmpConnectedParents, true, false, false);
		Scribe_Collections.Look<IntVec3, RoofDef>(ref roofs, "roofs", (LookMode)1, (LookMode)4);
		Scribe_Collections.Look<IntVec3, TerrainDef>(ref foundations, "foundations", (LookMode)1, (LookMode)4);
		Scribe_Collections.Look<IntVec3, TerrainDef>(ref terrains, "terrains", (LookMode)1, (LookMode)4);
		Scribe_Collections.Look<IntVec3, ColorDef>(ref terrainColors, "terrainColors", (LookMode)1, (LookMode)4);
		Scribe_Collections.Look<IntVec3, uint>(ref gases, "gases", (LookMode)1, (LookMode)1);
		Scribe_Collections.Look<IntVec3, Designation>(ref terrainDesignations, "terrainDesignations", (LookMode)1, (LookMode)2);
		Scribe_Deep.Look<VirtualMoveableAreas>(ref areas, "areas", Array.Empty<object>());
		Scribe_Values.Look<Vector3>(ref engineToCenter, "engineToCenter", default(Vector3), false);
		Scribe_Values.Look<Rot4>(ref rotation, "rotation", default(Rot4), false);
		Scribe_Collections.Look<RoomTemperatureVacuum>(ref roomTemperatures, "roomTemperatures", (LookMode)2, Array.Empty<object>());
		Scribe_Deep.Look<VirtualStoryState>(ref storyState, "storyState", Array.Empty<object>());
		Scribe_Collections.Look<AutoSlaughterConfig>(ref autoSlaughterConfigs, "autoSlaughterConfigs", (LookMode)2, Array.Empty<object>());
		if ((int)Scribe.mode == 4 && connectParents == null)
		{
			connectParents = new Dictionary<Thing, Thing>();
		}
	}

	public bool ContainsThing(Thing thing)
	{
		return things.ContainsKey(thing);
	}

	public bool ContainsPawn(Pawn pawn)
	{
		return pawns.ContainsKey(pawn);
	}

	public void TransferThingsToSubGravship()
	{
		List<Thing> list = Things.ToList();
		GenCollection.SortByDescending<Thing, int>(list, (Func<Thing, int>)GravshipUtility.ThingSpawnPriority);
		foreach (Thing item in list)
		{
			item.PreSwapMap();
		}
		foreach (Thing item2 in list)
		{
			if (item2.Spawned)
			{
				((Entity)item2).DeSpawn((DestroyMode)1);
			}
		}
		foreach (Pawn pawn in Pawns)
		{
			((Thing)pawn).PreSwapMap();
			if (((Thing)pawn).Spawned)
			{
				((Entity)pawn).DeSpawn((DestroyMode)1);
			}
		}
	}

	private static HashSet<IntVec3> GetValidSubstructure(Building_SubGravEngine engine, Map map)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		HashSet<IntVec3> hashSet = new HashSet<IntVec3>();
		foreach (IntVec3 item in engine.ValidSubstructure)
		{
			hashSet.Add(item);
		}
		HashSet<IntVec3> hashSet2 = hashSet;
		List<IntVec3> list = new List<IntVec3>();
		foreach (IntVec3 item2 in hashSet2)
		{
			if (!item2.IsOnSubstructure(map))
			{
				list.Add(item2);
			}
		}
		foreach (IntVec3 item3 in list)
		{
			hashSet2.Remove(item3);
		}
		return hashSet2;
	}

	private void TransferZones(Map oldMap, IntVec3 origin, HashSet<IntVec3> engineFloors)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		for (int num = oldMap.zoneManager.AllZones.Count - 1; num >= 0; num--)
		{
			Zone val = oldMap.zoneManager.AllZones[num];
			Zone_Stockpile val2 = (Zone_Stockpile)(object)((val is Zone_Stockpile) ? val : null);
			if (val2 == null)
			{
				Zone_Growing val3 = (Zone_Growing)(object)((val is Zone_Growing) ? val : null);
				if (val3 != null)
				{
					VirtualMoveableGrowZone virtualMoveableGrowZone = null;
					foreach (IntVec3 cell in ((Zone)val3).Cells)
					{
						if (engineFloors.Contains(cell))
						{
							if (virtualMoveableGrowZone == null)
							{
								virtualMoveableGrowZone = new VirtualMoveableGrowZone(this, val3);
							}
							virtualMoveableGrowZone.Add(cell - origin);
						}
					}
					if (virtualMoveableGrowZone != null)
					{
						areas.GrowZones.Add(virtualMoveableGrowZone);
						val.Delete();
					}
				}
			}
			else
			{
				VirtualMoveableStockpile virtualMoveableStockpile = null;
				foreach (IntVec3 cell2 in ((Zone)val2).Cells)
				{
					if (engineFloors.Contains(cell2))
					{
						if (virtualMoveableStockpile == null)
						{
							virtualMoveableStockpile = new VirtualMoveableStockpile(this, val2);
						}
						virtualMoveableStockpile.Add(cell2 - origin);
					}
				}
				if (virtualMoveableStockpile != null)
				{
					areas.Stockpiles.Add(virtualMoveableStockpile);
					val.Delete();
				}
			}
		}
	}

	private void CopyAreas(Map oldMap, IntVec3 origin, HashSet<IntVec3> engineFloors)
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		foreach (Area area in oldMap.areaManager.AllAreas)
		{
			if (!(area is Area_Home))
			{
				Area obj = area;
				Area_Allowed val = (Area_Allowed)(object)((obj is Area_Allowed) ? obj : null);
				if (val != null)
				{
					VirtualMoveableArea_Allowed virtualMoveableArea_Allowed = new VirtualMoveableArea_Allowed(this, (Area)(object)val);
					foreach (IntVec3 activeCell in area.ActiveCells)
					{
						if (engineFloors.Contains(activeCell))
						{
							virtualMoveableArea_Allowed.Add(activeCell - origin);
						}
					}
					virtualMoveableArea_Allowed.AssignedPawns.AddRange(oldMap.mapPawns.AllPawnsSpawned.Where(delegate(Pawn pawn)
					{
						Pawn_PlayerSettings playerSettings3 = pawn.playerSettings;
						return ((playerSettings3 != null) ? playerSettings3.AreaRestrictionInPawnCurrentMap : null) == area;
					}));
					areas.AllowedAreas.Add(virtualMoveableArea_Allowed);
					continue;
				}
				VirtualMoveableArea virtualMoveableArea = null;
				foreach (IntVec3 activeCell2 in area.ActiveCells)
				{
					if (engineFloors.Contains(activeCell2))
					{
						if (virtualMoveableArea == null)
						{
							virtualMoveableArea = new VirtualMoveableArea(this, area.Label, area.RenamableLabel, area.Color, area.ID);
						}
						virtualMoveableArea.Add(activeCell2 - origin);
					}
				}
				if (virtualMoveableArea == null)
				{
					continue;
				}
				if (!(area is Area_NoRoof))
				{
					if (!(area is Area_BuildRoof))
					{
						if (area is Area_SnowOrSandClear)
						{
							areas.SnowClearArea = virtualMoveableArea;
						}
						else if (ModsConfig.BiotechActive && area is Area_PollutionClear)
						{
							areas.PollutionClearArea = virtualMoveableArea;
						}
					}
					else
					{
						areas.BuildRoofArea = virtualMoveableArea;
					}
				}
				else
				{
					areas.NoRoofArea = virtualMoveableArea;
				}
				continue;
			}
			areas.HomeArea = new VirtualMoveableArea_Allowed(this, area);
			foreach (IntVec3 activeCell3 in area.ActiveCells)
			{
				if (engineFloors.Contains(activeCell3))
				{
					areas.HomeArea.Add(activeCell3 - origin);
				}
			}
			areas.HomeArea.AssignedPawns.AddRange(oldMap.mapPawns.AllPawnsSpawned.Where(delegate(Pawn pawn)
			{
				Pawn_PlayerSettings playerSettings3 = pawn.playerSettings;
				return ((playerSettings3 != null) ? playerSettings3.AreaRestrictionInPawnCurrentMap : null) == area;
			}));
			foreach (Pawn item in oldMap.mapPawns.AllPawnsUnspawned)
			{
				if (((Thing)item).SpawnedParentOrMe != null && things.ContainsKey(((Thing)item).SpawnedParentOrMe))
				{
					Pawn_PlayerSettings playerSettings = item.playerSettings;
					if (((playerSettings != null) ? playerSettings.AreaRestrictionInPawnCurrentMap : null) == area)
					{
						areas.HomeArea.AssignedPawns.Add(item);
					}
				}
			}
			foreach (Pawn assignedPawn in areas.HomeArea.AssignedPawns)
			{
				Pawn_PlayerSettings playerSettings2 = assignedPawn.playerSettings;
				if (playerSettings2 != null)
				{
					playerSettings2.Notify_MapRemoved(((Thing)assignedPawn).MapHeld);
				}
			}
		}
	}

	private void CopyStorageGroups(Map oldMap)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		foreach (StorageGroup item in oldMap.storageGroups.StorageGroupsForReading)
		{
			MoveableStorageGroup val = null;
			foreach (IStorageGroupMember member in item.members)
			{
				Thing val2 = (Thing)(object)((member is Thing) ? member : null);
				if (val2 != null && things.ContainsKey(val2))
				{
					if (val == null)
					{
						val = new MoveableStorageGroup(item);
					}
					val.members.Add(member);
				}
			}
			if (val == null)
			{
				continue;
			}
			if (GenCollection.Any<IStorageGroupMember>(val.members))
			{
				areas.StorageGroups.Add(val);
			}
			foreach (IStorageGroupMember member2 in val.members)
			{
				StorageGroupUtility.SetStorageGroup(member2, (StorageGroup)null, false);
			}
		}
	}

	private void CopyCellContents(Map oldMap, IntVec3 origin, HashSet<IntVec3> engineFloors)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Expected O, but got Unknown
		HashSet<Room> hashSet = new HashSet<Room>();
		AddThing((Thing)(object)engine, ((Thing)engine).Position - origin);
		foreach (IntVec3 engineFloor in engineFloors)
		{
			IntVec3 val = engineFloor - origin;
			List<Thing> list = oldMap.thingGrid.ThingsListAt(engineFloor);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				Thing val2 = list[num];
				if (ShouldBringOnGravship(val2, engineFloor))
				{
					Building val3 = (Building)(object)((val2 is Building) ? val2 : null);
					if (val3 == null || !((Thing)val3).def.building.isAttachment)
					{
						foreach (Thing attachedBuilding in GenConstruct.GetAttachedBuildings(val2))
						{
							Rot4 opposite = attachedBuilding.Rotation;
							opposite = ((Rot4)(ref opposite)).Opposite;
							AddThing(attachedBuilding, val + ((Rot4)(ref opposite)).FacingCell);
						}
						AddThing(val2, val);
					}
				}
			}
			foundations[val] = oldMap.terrainGrid.FoundationAt(engineFloor);
			terrains[val] = oldMap.terrainGrid.TerrainAt(engineFloor);
			terrainColors[val] = oldMap.terrainGrid.ColorAt(engineFloor);
			roofs[val] = oldMap.roofGrid.RoofAt(engineFloor);
			foreach (Designation item in oldMap.designationManager.AllDesignationsAt(engineFloor))
			{
				if (item.def == DesignationDefOf.RemoveFloor || item.def == DesignationDefOf.PaintFloor || item.def == DesignationDefOf.RemovePaintFloor)
				{
					terrainDesignations[val] = item;
					oldMap.designationManager.RemoveDesignation(item);
				}
			}
			gases[val] = oldMap.gasGrid.GetDirect(engineFloor);
			Room room = GridsUtility.GetRoom(engineFloor, oldMap);
			if (room != null && hashSet.Add(room) && !room.UsesOutdoorTemperature)
			{
				roomTemperatures.Add(new RoomTemperatureVacuum
				{
					roomCell = val,
					temperature = room.Temperature,
					vacuum = room.Vacuum
				});
			}
		}
	}

	private static bool ShouldBringOnGravship(Thing thing, IntVec3 cell)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Invalid comparison between Unknown and I4
		if (thing is Mote)
		{
			return false;
		}
		if (thing is Skyfaller)
		{
			return false;
		}
		if (!thing.def.bringAlongOnGravship)
		{
			return false;
		}
		if (cell != thing.Position)
		{
			return false;
		}
		if ((int)thing.def.category == 10 && !(thing is Blueprint) && !(thing is Frame))
		{
			return false;
		}
		return true;
	}

	private void AddThing(Thing thing, IntVec3 offset)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		if (thing is Mote || thing is Skyfaller || !thing.def.bringAlongOnGravship)
		{
			return;
		}
		Pawn val = (Pawn)(object)((thing is Pawn) ? thing : null);
		if (val != null)
		{
			if (pawns.ContainsKey(val))
			{
				return;
			}
			pawns.Add(val, new PositionData(offset, ((Thing)val).Rotation, val.Drafted));
			Thing val2 = default(Thing);
			if (val.carryTracker.CarriedThing != null && val.carryTracker.TryDropCarriedThing(((Thing)val).Position, (ThingPlaceMode)1, ref val2, (Action<Thing, int>)null) && !val2.Destroyed)
			{
				AddThing(val2, offset);
			}
			Pawn_PlayerSettings playerSettings = val.playerSettings;
			if (playerSettings != null)
			{
				playerSettings.Notify_MapRemoved(thing.Map);
			}
		}
		else
		{
			if (things.ContainsKey(thing) || !engine.OnValidSubstructure(thing))
			{
				return;
			}
			things.Add(thing, new PositionData(offset, thing.Rotation, false));
		}
		CompPowerTrader val3 = default(CompPowerTrader);
		if (ThingCompUtility.TryGetComp<CompPowerTrader>(thing, ref val3))
		{
			powerOn.Add(thing, val3.PowerOn);
		}
		CompPower val4 = default(CompPower);
		if (ThingCompUtility.TryGetComp<CompPower>(thing, ref val4))
		{
			connectParents.Add(thing, (Thing)(object)((ThingComp)(val4.connectParent?)).parent);
		}
	}

	private static bool ConsideredKidnapped(Building_SubGravEngine engine, Pawn pawn)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (!ModsConfig.OdysseyActive)
		{
			return false;
		}
		if (!engine.ValidSubstructureAt(((Thing)pawn).PositionHeld))
		{
			return false;
		}
		if (((Thing)pawn).Faction == Faction.OfPlayer || pawn.IsPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
		{
			return false;
		}
		if (FactionUtility.HostileTo(((Thing)pawn).Faction, Faction.OfPlayer) || ((Thing)pawn).Faction == null)
		{
			return false;
		}
		return true;
	}

	private void CheckAffectedGoodwill()
	{
		tmpFactionMembersKidnappedCount.Clear();
		int num = 0;
		Pawn val = default(Pawn);
		PositionData val2 = default(PositionData);
		Faction val4 = default(Faction);
		int num2 = default(int);
		foreach (KeyValuePair<Pawn, PositionData> pawn in pawns)
		{
			pawn.Deconstruct(ref val, ref val2);
			Pawn val3 = val;
			if (ConsideredKidnapped(engine, val3))
			{
				tmpFactionMembersKidnappedCount.TryAdd(((Thing)val3).Faction, num);
				Faction faction = ((Thing)val3).Faction;
				Dictionary<Faction, int> dictionary = tmpFactionMembersKidnappedCount;
				val4 = faction;
				num2 = dictionary[val4]++;
				num = num2;
			}
		}
		foreach (KeyValuePair<Faction, int> item in tmpFactionMembersKidnappedCount)
		{
			item.Deconstruct(ref val4, ref num2);
			Faction faction = val4;
			num = num2;
			Faction obj = faction;
			num2 = -10 * num;
			obj.TryAffectGoodwillWith(Faction.OfPlayer, num2, true, true, HistoryEventDefOf.PawnKidnappedOnGravShip, (GlobalTargetInfo?)null);
		}
	}

	private Dictionary<T, Data> GetPlacementValues<T>(Dictionary<T, PositionData> thingLocs, ref Dictionary<T, Data> working, ref Rot4 prevRotation) where T : Thing
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Invalid comparison between Unknown and I4
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		if (working != null && prevRotation == rotation)
		{
			return working;
		}
		if (working == null)
		{
			working = new Dictionary<T, Data>(thingLocs.Count);
		}
		working.Clear();
		T val = default(T);
		PositionData val2 = default(PositionData);
		foreach (KeyValuePair<T, PositionData> thingLoc in thingLocs)
		{
			thingLoc.Deconstruct(ref val, ref val2);
			T val3 = val;
			PositionData val4 = val2;
			IntVec3 adjustedLocalPosition = PrefabUtility.GetAdjustedLocalPosition(val4.position, rotation);
			IntVec2 size = ((Thing)val3).def.size;
			bool flag = true;
			if (!((Thing)val3).def.rotatable && size.x == size.z)
			{
				GenAdj.AdjustForRotation(ref adjustedLocalPosition, ref size, ((BuildableDef)((Thing)val3).def).defaultPlacingRot, rotation);
				flag = false;
			}
			else if (!((Thing)val3).def.rotatable && (int)((Thing)val3).def.category != 3)
			{
				flag = false;
			}
			Rot4 val5 = ((!flag) ? Rot4.North : ((Rot4)(ref rotation)).Rotated(val4.relativeRotation));
			working[val3] = new Data
			{
				local = adjustedLocalPosition,
				rotation = val5,
				drafted = val4.drafted
			};
		}
		prevRotation = rotation;
		return working;
	}

	private Dictionary<IntVec3, T> GetRotatedValues<T>(Dictionary<IntVec3, T> dictionary, ref Dictionary<IntVec3, T> working, ref Rot4 prevRotation)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		if (working != null && prevRotation == rotation)
		{
			return working;
		}
		if (working == null)
		{
			working = new Dictionary<IntVec3, T>(dictionary.Count);
		}
		working.Clear();
		IntVec3 val = default(IntVec3);
		T val2 = default(T);
		foreach (KeyValuePair<IntVec3, T> item in dictionary)
		{
			item.Deconstruct(ref val, ref val2);
			IntVec3 val3 = val;
			T value = val2;
			working[PrefabUtility.GetAdjustedLocalPosition(val3, rotation)] = value;
		}
		prevRotation = rotation;
		return working;
	}

	private IEnumerable<(IntVec3, T)> GetRotatedValues<T>(Dictionary<IntVec3, T> dictionary)
	{
		IntVec3 val = default(IntVec3);
		T val2 = default(T);
		foreach (KeyValuePair<IntVec3, T> item2 in dictionary)
		{
			item2.Deconstruct(ref val, ref val2);
			IntVec3 val3 = val;
			T item = val2;
			yield return (PrefabUtility.GetAdjustedLocalPosition(val3, rotation), item);
		}
	}
}

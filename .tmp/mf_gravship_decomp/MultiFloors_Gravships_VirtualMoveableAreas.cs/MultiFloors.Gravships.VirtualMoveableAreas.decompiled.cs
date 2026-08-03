using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace MultiFloors.Gravships;

[HotSwappable]
public class VirtualMoveableAreas : IExposable
{
	public List<VirtualMoveableStockpile> Stockpiles = new List<VirtualMoveableStockpile>();

	public List<VirtualMoveableGrowZone> GrowZones = new List<VirtualMoveableGrowZone>();

	public List<VirtualMoveableArea_Allowed> AllowedAreas = new List<VirtualMoveableArea_Allowed>();

	public List<MoveableStorageGroup> StorageGroups = new List<MoveableStorageGroup>();

	public VirtualMoveableArea_Allowed HomeArea;

	public VirtualMoveableArea BuildRoofArea;

	public VirtualMoveableArea NoRoofArea;

	public VirtualMoveableArea SnowClearArea;

	public VirtualMoveableArea PollutionClearArea;

	public void ExposeData()
	{
		Scribe_Collections.Look<VirtualMoveableStockpile>(ref Stockpiles, "stockpiles", (LookMode)2, Array.Empty<object>());
		Scribe_Collections.Look<VirtualMoveableGrowZone>(ref GrowZones, "growZones", (LookMode)2, Array.Empty<object>());
		Scribe_Collections.Look<VirtualMoveableArea_Allowed>(ref AllowedAreas, "allowedAreas", (LookMode)2, Array.Empty<object>());
		Scribe_Collections.Look<MoveableStorageGroup>(ref StorageGroups, "storageGroups", (LookMode)2, Array.Empty<object>());
		Scribe_Deep.Look<VirtualMoveableArea_Allowed>(ref HomeArea, "homeArea", Array.Empty<object>());
		Scribe_Deep.Look<VirtualMoveableArea>(ref BuildRoofArea, "buildRoofArea", Array.Empty<object>());
		Scribe_Deep.Look<VirtualMoveableArea>(ref NoRoofArea, "noRoofArea", Array.Empty<object>());
		Scribe_Deep.Look<VirtualMoveableArea>(ref SnowClearArea, "snowClearArea", Array.Empty<object>());
		Scribe_Deep.Look<VirtualMoveableArea>(ref PollutionClearArea, "pollutionClearArea", Array.Empty<object>());
	}
}

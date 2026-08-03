using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(AutoBuildRoofAreaSetter), "TryGenerateAreaNow")]
internal static class Patch_AutoBuildRoofAreaSetter_TryGenerateAreaNow
{
	private static bool Prefix(Room room)
	{
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.RoofSync))
		{
			return true;
		}
		try
		{
			if (room == null || room.Dereferenced)
			{
				return true;
			}
			Map map = room.Map;
			if (map == null || map.Level() != 1)
			{
				return true;
			}
			List<Region> regions = room.Regions;
			if (regions == null || regions.Count == 0)
			{
				return false;
			}
			for (int i = 0; i < regions.Count; i++)
			{
				Region val = regions[i];
				if (val == null || !val.valid || val.Map != map)
				{
					ABLog.Dev("Sky auto-roof guard: skipped a mid-rebuild room (detached region).");
					return false;
				}
			}
			TerrainGrid terrainGrid = map.terrainGrid;
			foreach (IntVec3 borderCell in room.BorderCells)
			{
				if (!GenGrid.InBounds(borderCell, map) || terrainGrid.TerrainAt(borderCell) != ABDefOf.AB_OpenAir)
				{
					continue;
				}
				Area buildRoof = (Area)(object)map.areaManager.BuildRoof;
				foreach (IntVec3 cell in room.Cells)
				{
					if (buildRoof[cell])
					{
						buildRoof[cell] = false;
					}
				}
				return false;
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.RoofSync, e, "sky auto-roof guard");
		}
		return true;
	}
}

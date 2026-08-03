using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public static class LevelSync
{
	private static Dictionary<TerrainDef, Color> minedRockColors;

	private static Dictionary<TerrainDef, ThingDef> minedRockDefs;

	public static void ReconcileRooftops(Map sky)
	{
		int cursor = 0;
		while (ReconcileRooftopsSlice(sky, ref cursor, int.MaxValue))
		{
		}
	}

	public static bool ReconcileRooftopsSlice(Map sky, ref int cursor, int maxCells)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.RoofSync) || sky == null)
		{
			return false;
		}
		try
		{
			Map val = sky.LowerMap() ?? sky.GroundMap();
			if (val == null || val.Disposed || val == sky)
			{
				return false;
			}
			TerrainGrid terrainGrid = sky.terrainGrid;
			TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
			TerrainDef aB_RoofSurface = ABDefOf.AB_RoofSurface;
			CellIndices cellIndices = sky.cellIndices;
			bool flag = sky.Level() == 1;
			int numGridCells = ((CellIndices)(ref cellIndices)).NumGridCells;
			int num = ((maxCells >= numGridCells - cursor) ? numGridCells : (cursor + maxCells));
			int num2 = 0;
			while (cursor < num)
			{
				IntVec3 val2 = ((CellIndices)(ref cellIndices)).IndexToCell(cursor);
				if (flag)
				{
					RoofDef val3 = sky.roofGrid.RoofAt(val2);
					if (val3 != null && val3.isNatural)
					{
						Building val4 = sky.edificeGrid[val2];
						if (val4 == null || !((Thing)val4).def.mineable)
						{
							sky.roofGrid.SetRoof(val2, (RoofDef)null);
							num2++;
						}
					}
				}
				TerrainDef val5 = terrainGrid.TerrainAt(val2);
				if (val5 == aB_OpenAir || val5 == aB_RoofSurface)
				{
					TerrainDef val6 = ((GenGrid.InBounds(val2, val) && CoveredBelow(val, val2)) ? aB_RoofSurface : aB_OpenAir);
					if (val5 != val6 && (val6 != aB_OpenAir || !IsStairsPlatform(sky, val2)))
					{
						terrainGrid.SetTerrain(val2, val6);
						num2++;
					}
				}
				cursor++;
			}
			if (num2 > 0)
			{
				ABLog.Dev("Rooftop reconciliation fixed " + num2 + " cell(s) on map " + sky.uniqueID + ".");
			}
			return cursor < numGridCells;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.RoofSync, e, "rooftop reconciliation");
			return false;
		}
	}

	public static void OnLowerMeshDirty(Map map, IntVec3 c, ulong flags)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		if (!LevelComp.AnySkyLevels || !ABGuard.On(ABGuard.Rendering) || (flags & (MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Things) | MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Buildings) | MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.FogOfWar))) == 0)
		{
			return;
		}
		try
		{
			Map val = map.UpperMap();
			if (val != null && !val.Disposed && GenGrid.InBounds(c, val))
			{
				LevelComp levelComp = val.Levels();
				if (levelComp != null && levelComp.level > 0)
				{
					val.mapDrawer.MapMeshDirty(c, MapMeshFlagDef.op_Implicit(ABDefOf.AB_BelowThings), true, false);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Rendering, e, "below mesh mirror");
		}
	}

	public static void OnGroundRoofChanged(Map ground, IntVec3 c)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.RoofSync))
		{
			return;
		}
		try
		{
			Map val = ground.UpperMap();
			if (val == null || val.Disposed || !GenGrid.InBounds(c, val))
			{
				return;
			}
			SyncCellFromBelow(val, ground, c, allowFloorCollapse: true);
			IntVec3[] adjacentCells = GenAdj.AdjacentCells;
			for (int i = 0; i < adjacentCells.Length; i++)
			{
				IntVec3 val2 = c + adjacentCells[i];
				if (GenGrid.InBounds(val2, val) && GenGrid.InBounds(val2, ground))
				{
					SyncCellFromBelow(val, ground, val2, allowFloorCollapse: false);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.RoofSync, e, "roof to sky sync");
		}
	}

	public static void OnSkyTerrainChanged(Map sky, IntVec3 c)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.RoofSync))
		{
			return;
		}
		try
		{
			TerrainDef val = sky.terrainGrid.TerrainAt(c);
			Map val2 = sky.LowerMap();
			bool flag = val2 != null && !val2.Disposed && GenGrid.InBounds(c, val2);
			if (flag)
			{
				val2.mapDrawer.MapMeshDirty(c, MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Roofs));
			}
			if (val == ABDefOf.AB_OpenAir)
			{
				DropCellContents(sky, c);
			}
			else if (val.Removable && flag && val2.roofGrid.RoofAt(c) == null)
			{
				val2.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.RoofSync, e, "sky floor to roof sync");
		}
	}

	public static void OnSkyThingSpawned(Map sky, Thing thing)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.RoofSync))
		{
			return;
		}
		try
		{
			if (thing == null || !thing.Spawned || thing.Map != sky || !ShouldFall(thing) || sky.terrainGrid.TerrainAt(thing.Position) != ABDefOf.AB_OpenAir)
			{
				return;
			}
			Thing t = thing;
			Map m = sky;
			LongEventHandler.ExecuteWhenFinished((Action)delegate
			{
				//IL_0065: Unknown result type (might be due to invalid IL or missing references)
				//IL_0040: Unknown result type (might be due to invalid IL or missing references)
				try
				{
					if (t.Spawned && t.Map == m && !m.Disposed && m.terrainGrid.TerrainAt(t.Position) == ABDefOf.AB_OpenAir)
					{
						DropThing(t, t.Position, m);
					}
				}
				catch (Exception e2)
				{
					ABGuard.Disable(ABGuard.RoofSync, e2, "air spawn fall");
				}
			});
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.RoofSync, e, "air spawn check");
		}
	}

	public static void OnLevelMineableDespawned(Map levelMap, Thing thing)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.RoofSync))
		{
			return;
		}
		try
		{
			if (!(thing is Mineable))
			{
				return;
			}
			IntVec3 position = thing.Position;
			if (!GenGrid.InBounds(position, levelMap))
			{
				return;
			}
			FogGrid fogGrid = levelMap.fogGrid;
			if (fogGrid.IsFogged(position))
			{
				fogGrid.Unfog(position);
			}
			fogGrid.FloodUnfogAdjacent(position, false);
			if (levelMap.Level() == 1)
			{
				RoofDef val = levelMap.roofGrid.RoofAt(position);
				if (val != null && val.isNatural)
				{
					levelMap.roofGrid.SetRoof(position, (RoofDef)null);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.RoofSync, e, "mining fog reveal");
		}
	}

	private static void SyncCellFromBelow(Map sky, Map ground, IntVec3 c, bool allowFloorCollapse)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		TerrainGrid terrainGrid = sky.terrainGrid;
		TerrainDef val = terrainGrid.TerrainAt(c);
		if (CoveredBelow(ground, c))
		{
			if (val == ABDefOf.AB_OpenAir)
			{
				terrainGrid.SetTerrain(c, ABDefOf.AB_RoofSurface);
			}
		}
		else if (val == ABDefOf.AB_RoofSurface)
		{
			if (!IsStairsPlatform(sky, c))
			{
				terrainGrid.SetTerrain(c, ABDefOf.AB_OpenAir);
			}
		}
		else if (allowFloorCollapse && val != ABDefOf.AB_OpenAir && val.Removable && ground.roofGrid.RoofAt(c) == null)
		{
			terrainGrid.RemoveTopLayer(c, true);
			if (terrainGrid.TerrainAt(c) == ABDefOf.AB_RoofSurface)
			{
				terrainGrid.SetTerrain(c, ABDefOf.AB_OpenAir);
			}
		}
	}

	internal static bool CoveredBelow(Map ground, IntVec3 c)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		RoofDef val = ground.roofGrid.RoofAt(c);
		if (val != null)
		{
			return !val.isNatural;
		}
		Building val2 = ground.edificeGrid[c];
		if (val2 == null || (int)((BuildableDef)((Thing)val2).def).passability != 2 || ((Thing)val2).def.mineable || val2 is Building_ABStairs || (((Thing)val2).def.building != null && ((Thing)val2).def.building.isNaturalRock))
		{
			return false;
		}
		IntVec3[] adjacentCells = GenAdj.AdjacentCells;
		for (int i = 0; i < adjacentCells.Length; i++)
		{
			IntVec3 val3 = c + adjacentCells[i];
			if (GenGrid.InBounds(val3, ground))
			{
				RoofDef val4 = ground.roofGrid.RoofAt(val3);
				if (val4 != null && !val4.isNatural)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static bool TryGetMinedRockDef(TerrainDef leaveTerrain, out ThingDef rockDef)
	{
		rockDef = null;
		if (leaveTerrain == null)
		{
			return false;
		}
		if (minedRockDefs == null)
		{
			TryGetMinedRockColor(leaveTerrain, out var _);
		}
		return minedRockDefs != null && minedRockDefs.TryGetValue(leaveTerrain, out rockDef);
	}

	internal static bool TryGetMinedRockColor(TerrainDef leaveTerrain, out Color color)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		color = default(Color);
		if (leaveTerrain == null)
		{
			return false;
		}
		if (minedRockColors == null)
		{
			minedRockColors = new Dictionary<TerrainDef, Color>();
			minedRockDefs = new Dictionary<TerrainDef, ThingDef>();
			List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
			for (int i = 0; i < allDefsListForReading.Count; i++)
			{
				ThingDef val = allDefsListForReading[i];
				TerrainDef val2 = val.building?.leaveTerrain;
				if (val2 != null && val.mineable && !minedRockColors.ContainsKey(val2))
				{
					Color val3 = ((val.graphicData != null) ? val.graphicData.color : Color.white);
					if (val3 == Color.white && val.stuffProps != null)
					{
						val3 = val.stuffProps.color;
					}
					minedRockColors[val2] = val3;
					minedRockDefs[val2] = val;
				}
			}
		}
		return minedRockColors.TryGetValue(leaveTerrain, out color);
	}

	internal static bool IsStairsPlatform(Map sky, IntVec3 c)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		List<Building_ABStairs> list = sky.Levels()?.Stairs;
		if (list == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Building_ABStairs building_ABStairs = list[i];
			if (building_ABStairs != null && ((Thing)building_ABStairs).Spawned)
			{
				CellRect val = GenAdj.OccupiedRect((Thing)(object)building_ABStairs);
				val = ((CellRect)(ref val)).ExpandedBy(1);
				if (((CellRect)(ref val)).Contains(c))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool ShouldFall(Thing t)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Invalid comparison between Unknown and I4
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Invalid comparison between Unknown and I4
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		if (t is Mineable || t is Blueprint || t is Frame || t is Explosion || t is Building_ABStairs)
		{
			return false;
		}
		ThingCategory category = t.def.category;
		return (int)category == 2 || (int)category == 1 || (int)category == 3;
	}

	private static void DropCellContents(Map sky, IntVec3 c)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		Map val = sky.LowerMap();
		if (val == null || val.Disposed)
		{
			return;
		}
		List<Thing> list = GridsUtility.GetThingList(c, sky).ToList();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Thing val2 = list[num];
			if (!val2.Destroyed && val2.Spawned)
			{
				if (val2 is Building_ABStairs building_ABStairs)
				{
					Messages.Message(TaggedString.op_Implicit(Translator.Translate("AB_StairsCollapsed")), LookTargets.op_Implicit(new TargetInfo(c, val, false)), MessageTypeDefOf.NegativeEvent, true);
					((Thing)building_ABStairs).Destroy((DestroyMode)2);
				}
				else if (ShouldFall(val2))
				{
					DropThing(val2, c, sky);
				}
			}
		}
	}

	private static void DropThing(Thing t, IntVec3 c, Map sky)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		Map val = sky.LowerMap();
		if (val == null || val.Disposed || !GenGrid.InBounds(c, val))
		{
			return;
		}
		Pawn val2 = (Pawn)(object)((t is Pawn) ? t : null);
		if (val2 != null)
		{
			((Entity)val2).DeSpawn((DestroyMode)0);
			IntVec3 val3 = (GenGrid.Standable(c, val) ? c : CellFinder.StandableCellNear(c, val, 4f, (Predicate<IntVec3>)null));
			if (!((IntVec3)(ref val3)).IsValid)
			{
				val3 = c;
			}
			GenSpawn.Spawn((Thing)(object)val2, val3, val, (WipeMode)0);
			((Thing)val2).TakeDamage(new DamageInfo(DamageDefOf.Blunt, 9f, 0f, -1f, (Thing)null, (BodyPartRecord)null, (ThingDef)null, (SourceCategory)0, (Thing)null, true, true, (QualityCategory)2, true, false));
			Pawn_StanceTracker stances = val2.stances;
			if (stances != null)
			{
				StunHandler stunner = stances.stunner;
				if (stunner != null)
				{
					stunner.StunFor(90, (Thing)null, false, true, false);
				}
			}
		}
		else if ((int)t.def.category == 2)
		{
			((Entity)t).DeSpawn((DestroyMode)0);
			GenPlace.TryPlaceThing(t, c, val, (ThingPlaceMode)1, (Action<Thing, int>)null, (Predicate<IntVec3>)null, (Rot4?)null, 1);
		}
		else
		{
			Building val4 = (Building)(object)((t is Building) ? t : null);
			if (val4 != null)
			{
				((Thing)val4).Destroy((DestroyMode)2);
			}
		}
	}
}

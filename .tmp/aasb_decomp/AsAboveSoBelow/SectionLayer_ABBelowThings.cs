using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public class SectionLayer_ABBelowThings : SectionLayer
{
	internal static int DiagCalls;

	internal static int DiagNoComp;

	internal static int DiagNoLower;

	internal static int DiagAirCells;

	internal static int DiagConsidered;

	internal static int DiagPrinted;

	internal static int DiagPrintErrors;

	private readonly List<int> vertCountsBefore = new List<int>();

	public override bool Visible => ABGuard.On(ABGuard.Rendering);

	internal static void DiagReset()
	{
		DiagCalls = (DiagNoComp = (DiagNoLower = (DiagAirCells = (DiagConsidered = (DiagPrinted = (DiagPrintErrors = 0))))));
	}

	internal static string DiagSummary()
	{
		return "regens=" + DiagCalls + " earlyNoComp=" + DiagNoComp + " earlyNoLower=" + DiagNoLower + " airCells=" + DiagAirCells + " considered=" + DiagConsidered + " printed=" + DiagPrinted + " printErrors=" + DiagPrintErrors;
	}

	public SectionLayer_ABBelowThings(Section section)
		: base(section)
	{
		((MapDrawLayer)this).relevantChangeTypes = MapMeshFlagDef.op_Implicit(ABDefOf.AB_BelowThings) | MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Terrain);
	}

	public unsafe override void Regenerate()
	{
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Invalid comparison between Unknown and I4
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Invalid comparison between Unknown and I4
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		((MapDrawLayer)this).ClearSubMeshes((MeshParts)63);
		Map map = base.section.map;
		if (!ABGuard.On(ABGuard.Rendering))
		{
			return;
		}
		try
		{
			DiagCalls++;
			LevelComp levelComp = map.Levels();
			if (levelComp == null || levelComp.level <= 0)
			{
				if (levelComp == null)
				{
					DiagNoComp++;
				}
				return;
			}
			Map lowerMap = levelComp.lowerMap;
			if (lowerMap == null || lowerMap.Disposed)
			{
				DiagNoLower++;
				return;
			}
			TerrainGrid terrainGrid = map.terrainGrid;
			TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
			FogGrid fogGrid = lowerMap.fogGrid;
			float num = Mathf.Clamp(ABMod.Settings?.belowThingScale ?? 0.85f, 0.5f, 1f);
			bool flag = num < 0.999f;
			bool flag2 = false;
			CellRect cellRect = base.section.CellRect;
			Enumerator enumerator = ((CellRect)(ref cellRect)).GetEnumerator();
			try
			{
				while (((Enumerator)(ref enumerator)).MoveNext())
				{
					IntVec3 current = ((Enumerator)(ref enumerator)).Current;
					if (!GenGrid.InBounds(current, lowerMap))
					{
						continue;
					}
					TerrainDef val = terrainGrid.TerrainAt(current);
					if (val != aB_OpenAir)
					{
						continue;
					}
					DiagAirCells++;
					List<Thing> list = lowerMap.thingGrid.ThingsListAtFast(current);
					for (int i = 0; i < list.Count; i++)
					{
						Thing val2 = list[i];
						DrawerType drawerType = val2.def.drawerType;
						if ((int)drawerType != 2 && (int)drawerType != 3)
						{
							continue;
						}
						IntVec3 position = val2.Position;
						if (val2.def.size.x != 1 || val2.def.size.z != 1)
						{
							if (!IsBelowPrintAnchor(val2, current, map, terrainGrid, aB_OpenAir))
							{
								continue;
							}
						}
						else if (position.x != current.x || position.z != current.z)
						{
							continue;
						}
						if (!val2.def.seeThroughFog && fogGrid.IsFogged(position))
						{
							continue;
						}
						DiagConsidered++;
						try
						{
							if (flag && CanScale(val2))
							{
								SnapshotVertCounts();
								val2.Print((SectionLayer)(object)this);
								ScaleNewVerts(GenThing.TrueCenter(val2), num);
							}
							else
							{
								val2.Print((SectionLayer)(object)this);
							}
							flag2 = true;
							DiagPrinted++;
						}
						catch (Exception ex)
						{
							DiagPrintErrors++;
							Log.WarningOnce("[As above, So below] Below print failed for " + ((Entity)val2).LabelCap + ": " + ex.Message, val2.thingIDNumber ^ 0x2D6E2F88);
						}
					}
				}
			}
			finally
			{
				((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
			}
			if (flag2)
			{
				((MapDrawLayer)this).FinalizeMesh((MeshParts)63);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Rendering, e, "below things layer");
		}
	}

	private static bool IsBelowPrintAnchor(Thing t, IntVec3 c, Map sky, TerrainGrid skyTerrain, TerrainDef air)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		CellRect val = GenAdj.OccupiedRect(t);
		IntVec3 val2 = default(IntVec3);
		for (int i = val.minZ; i <= val.maxZ; i++)
		{
			for (int j = val.minX; j <= val.maxX; j++)
			{
				((IntVec3)(ref val2))._002Ector(j, 0, i);
				if (GenGrid.InBounds(val2, sky) && skyTerrain.TerrainAt(val2) == air)
				{
					return val2.x == c.x && val2.z == c.z;
				}
			}
		}
		return false;
	}

	private static bool CanScale(Thing t)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
		ThingDef def = t.def;
		if (def.mineable || (def.building != null && def.building.isNaturalRock))
		{
			return false;
		}
		GraphicData graphicData = def.graphicData;
		return graphicData == null || (int)graphicData.linkType == 0;
	}

	private void SnapshotVertCounts()
	{
		vertCountsBefore.Clear();
		List<LayerSubMesh> subMeshes = ((MapDrawLayer)this).subMeshes;
		for (int i = 0; i < subMeshes.Count; i++)
		{
			vertCountsBefore.Add(subMeshes[i].verts.Count);
		}
	}

	private void ScaleNewVerts(Vector3 center, float scale)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		List<LayerSubMesh> subMeshes = ((MapDrawLayer)this).subMeshes;
		for (int i = 0; i < subMeshes.Count; i++)
		{
			List<Vector3> verts = subMeshes[i].verts;
			int num = ((i < vertCountsBefore.Count) ? vertCountsBefore[i] : 0);
			for (int j = num; j < verts.Count; j++)
			{
				Vector3 val = verts[j];
				verts[j] = new Vector3(center.x + (val.x - center.x) * scale, val.y, center.z + (val.z - center.z) * scale);
			}
		}
	}

	public override void DrawLayer()
	{
		if (!((MapDrawLayer)this).Visible)
		{
			return;
		}
		List<LayerSubMesh> subMeshes = ((MapDrawLayer)this).subMeshes;
		for (int i = 0; i < subMeshes.Count; i++)
		{
			LayerSubMesh val = subMeshes[i];
			if (val.finalized && !val.disabled)
			{
				LevelRenderer.DrawBelowSubMesh(val);
			}
		}
	}
}

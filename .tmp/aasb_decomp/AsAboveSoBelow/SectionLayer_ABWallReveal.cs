using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public class SectionLayer_ABWallReveal : SectionLayer
{
	private const float Eps = 0.001f;

	private readonly List<int> vertsBefore = new List<int>();

	private readonly List<int> trisBefore = new List<int>();

	private readonly List<Vector4> clipRects = new List<Vector4>();

	private readonly List<Vector3> qVerts = new List<Vector3>();

	private readonly List<Vector3> qUvs = new List<Vector3>();

	private readonly List<Color32> qCols = new List<Color32>();

	private float stripAltitude;

	public override bool Visible => ABGuard.On(ABGuard.Rendering) && (ABMod.Settings?.drawWallReveal ?? true);

	public SectionLayer_ABWallReveal(Section section)
		: base(section)
	{
		((MapDrawLayer)this).relevantChangeTypes = MapMeshFlagDef.op_Implicit(ABDefOf.AB_BelowThings) | MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Terrain);
	}

	public unsafe override void Regenerate()
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		((MapDrawLayer)this).ClearSubMeshes((MeshParts)63);
		Map map = base.section.map;
		if (!ABGuard.On(ABGuard.Rendering) || map.Level() != 1)
		{
			return;
		}
		try
		{
			Map val = map.LowerMap();
			if (val == null || val.Disposed)
			{
				return;
			}
			TerrainGrid terrainGrid = map.terrainGrid;
			TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
			TerrainDef aB_RoofSurface = ABDefOf.AB_RoofSurface;
			FogGrid fogGrid = val.fogGrid;
			float width = Mathf.Clamp(ABMod.Settings?.wallRevealWidth ?? 0.5f, 0.15f, 0.75f);
			stripAltitude = Altitudes.AltitudeFor((AltitudeLayer)7);
			bool flag = false;
			CellRect cellRect = base.section.CellRect;
			Enumerator enumerator = ((CellRect)(ref cellRect)).GetEnumerator();
			try
			{
				while (((Enumerator)(ref enumerator)).MoveNext())
				{
					IntVec3 current = ((Enumerator)(ref enumerator)).Current;
					if (!GenGrid.InBounds(current, val) || !IsRimCell(map, terrainGrid, current, aB_OpenAir, aB_RoofSurface))
					{
						continue;
					}
					Building val2 = val.edificeGrid[current];
					if (!ABRimPrint.QualifiesAsSupport(val2) || (!((Thing)val2).def.seeThroughFog && fogGrid.IsFogged(((Thing)val2).Position)) || !IsFirstQualifyingCell(map, val, terrainGrid, val2, current, aB_OpenAir, aB_RoofSurface))
					{
						continue;
					}
					GatherStrips(map, val, terrainGrid, val2, aB_OpenAir, aB_RoofSurface, width);
					if (clipRects.Count == 0)
					{
						continue;
					}
					ABRimPrint.Snapshot(((MapDrawLayer)this).subMeshes, vertsBefore, trisBefore);
					try
					{
						((Thing)val2).Print((SectionLayer)(object)this);
						if (ClipNewGeometry())
						{
							flag = true;
						}
					}
					catch (Exception ex)
					{
						ABRimPrint.Rollback(((MapDrawLayer)this).subMeshes, vertsBefore, trisBefore);
						Log.WarningOnce("[As above, So below] Rim reveal print failed for " + ((Entity)val2).LabelCap + ": " + ex.Message, ((Thing)val2).thingIDNumber ^ 0x2D6E2F8A);
					}
				}
			}
			finally
			{
				((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
			}
			if (flag)
			{
				((MapDrawLayer)this).FinalizeMesh((MeshParts)63);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Rendering, e, "wall reveal layer");
		}
	}

	private static bool AirAt(Map sky, TerrainGrid grid, IntVec3 c, TerrainDef air)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		return GenGrid.InBounds(c, sky) && grid.TerrainAt(c) == air;
	}

	private static bool IsRimCell(Map sky, TerrainGrid grid, IntVec3 c, TerrainDef air, TerrainDef rooftop)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		if (!GenGrid.InBounds(c, sky) || grid.TerrainAt(c) != rooftop)
		{
			return false;
		}
		return AirAt(sky, grid, c + IntVec3.North, air) || AirAt(sky, grid, c + IntVec3.South, air) || AirAt(sky, grid, c + IntVec3.East, air) || AirAt(sky, grid, c + IntVec3.West, air);
	}

	private static bool IsFirstQualifyingCell(Map sky, Map lower, TerrainGrid grid, Building ed, IntVec3 c, TerrainDef air, TerrainDef rooftop)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		CellRect val = GenAdj.OccupiedRect((Thing)(object)ed);
		IntVec3 val2 = default(IntVec3);
		for (int i = val.minZ; i <= val.maxZ; i++)
		{
			for (int j = val.minX; j <= val.maxX; j++)
			{
				((IntVec3)(ref val2))._002Ector(j, 0, i);
				if (GenGrid.InBounds(val2, lower) && IsRimCell(sky, grid, val2, air, rooftop))
				{
					return val2.x == c.x && val2.z == c.z;
				}
			}
		}
		return false;
	}

	private void GatherStrips(Map sky, Map lower, TerrainGrid grid, Building ed, TerrainDef air, TerrainDef rooftop, float width)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		clipRects.Clear();
		CellRect val = GenAdj.OccupiedRect((Thing)(object)ed);
		IntVec3 val2 = default(IntVec3);
		for (int i = val.minZ; i <= val.maxZ; i++)
		{
			for (int j = val.minX; j <= val.maxX; j++)
			{
				((IntVec3)(ref val2))._002Ector(j, 0, i);
				if (!GenGrid.InBounds(val2, lower) || !IsRimCell(sky, grid, val2, air, rooftop))
				{
					continue;
				}
				bool flag = AirAt(sky, grid, val2 + IntVec3.North, air);
				bool flag2 = AirAt(sky, grid, val2 + IntVec3.South, air);
				bool flag3 = AirAt(sky, grid, val2 + IntVec3.East, air);
				bool flag4 = AirAt(sky, grid, val2 + IntVec3.West, air);
				if (flag)
				{
					clipRects.Add(new Vector4((float)j, (float)i + 1f - width, (float)j + 1f, (float)i + 1f));
				}
				if (flag2)
				{
					clipRects.Add(new Vector4((float)j, (float)i, (float)j + 1f, (float)i + width));
				}
				float num = (float)i + (flag2 ? width : 0f);
				float num2 = (float)i + 1f - (flag ? width : 0f);
				if (num2 - num > 0.001f)
				{
					if (flag3)
					{
						clipRects.Add(new Vector4((float)j + 1f - width, num, (float)j + 1f, num2));
					}
					if (flag4)
					{
						clipRects.Add(new Vector4((float)j, num, (float)j + width, num2));
					}
				}
			}
		}
	}

	private bool ClipNewGeometry()
	{
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		bool result = false;
		List<LayerSubMesh> subMeshes = ((MapDrawLayer)this).subMeshes;
		for (int i = 0; i < subMeshes.Count; i++)
		{
			LayerSubMesh val = subMeshes[i];
			int num = ((i < vertsBefore.Count) ? vertsBefore[i] : 0);
			int num2 = ((i < trisBefore.Count) ? trisBefore[i] : 0);
			int num3 = val.verts.Count - num;
			if (num3 <= 0)
			{
				continue;
			}
			if (ABRimPrint.IsShadowMaterial(val.material) || num3 % 4 != 0 || val.tris.Count - num2 != num3 / 4 * 6 || val.uvs.Count != val.verts.Count || val.colors.Count != val.verts.Count)
			{
				ABRimPrint.Truncate(val, num, num2);
				continue;
			}
			qVerts.Clear();
			qUvs.Clear();
			qCols.Clear();
			for (int j = num; j < val.verts.Count; j++)
			{
				qVerts.Add(val.verts[j]);
				qUvs.Add(val.uvs[j]);
				qCols.Add(val.colors[j]);
			}
			ABRimPrint.Truncate(val, num, num2);
			int num4 = qVerts.Count / 4;
			for (int k = 0; k < num4; k++)
			{
				if (EmitClippedQuad(val, k * 4))
				{
					result = true;
				}
			}
		}
		return result;
	}

	private bool EmitClippedQuad(LayerSubMesh sub, int b)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		float num = float.MaxValue;
		float num2 = float.MinValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		for (int i = 0; i < 4; i++)
		{
			Vector3 val = qVerts[b + i];
			num = Mathf.Min(num, val.x);
			num2 = Mathf.Max(num2, val.x);
			num3 = Mathf.Min(num3, val.z);
			num4 = Mathf.Max(num4, val.z);
		}
		if (num2 - num < 0.001f || num4 - num3 < 0.001f)
		{
			return false;
		}
		bool flag = true;
		int num5 = -1;
		int num6 = -1;
		int num7 = -1;
		int num8 = -1;
		for (int j = 0; j < 4; j++)
		{
			Vector3 val2 = qVerts[b + j];
			bool flag2 = val2.x - num < 0.001f;
			bool flag3 = num2 - val2.x < 0.001f;
			bool flag4 = val2.z - num3 < 0.001f;
			bool flag5 = num4 - val2.z < 0.001f;
			if ((!flag2 && !flag3) || (!flag4 && !flag5))
			{
				flag = false;
				break;
			}
			if (flag2 && flag4)
			{
				num5 = b + j;
			}
			else if (flag2)
			{
				num6 = b + j;
			}
			else if (flag5)
			{
				num7 = b + j;
			}
			else
			{
				num8 = b + j;
			}
		}
		if (!flag || num5 < 0 || num6 < 0 || num7 < 0 || num8 < 0)
		{
			for (int k = 0; k < clipRects.Count; k++)
			{
				Vector4 val3 = clipRects[k];
				if (num >= val3.x - 0.001f && num3 >= val3.y - 0.001f && num2 <= val3.z + 0.001f && num4 <= val3.w + 0.001f)
				{
					CopyQuad(sub, b);
					return true;
				}
			}
			return false;
		}
		bool result = false;
		for (int l = 0; l < clipRects.Count; l++)
		{
			Vector4 val4 = clipRects[l];
			float num9 = Mathf.Max(num, val4.x);
			float num10 = Mathf.Max(num3, val4.y);
			float num11 = Mathf.Min(num2, val4.z);
			float num12 = Mathf.Min(num4, val4.w);
			if (!(num11 - num9 < 0.001f) && !(num12 - num10 < 0.001f))
			{
				EmitSubQuad(sub, num5, num6, num7, num8, num, num2, num3, num4, num9, num10, num11, num12);
				result = true;
			}
		}
		return result;
	}

	private void CopyQuad(LayerSubMesh sub, int b)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		int count = sub.verts.Count;
		for (int i = 0; i < 4; i++)
		{
			Vector3 val = qVerts[b + i];
			sub.verts.Add(new Vector3(val.x, stripAltitude, val.z));
			sub.uvs.Add(qUvs[b + i]);
			sub.colors.Add(qCols[b + i]);
		}
		AddQuadTris(sub, count);
	}

	private void EmitSubQuad(LayerSubMesh sub, int i00, int i01, int i11, int i10, float xMin, float xMax, float zMin, float zMax, float cx0, float cz0, float cx1, float cz1)
	{
		int count = sub.verts.Count;
		AddClippedVert(sub, i00, i01, i11, i10, xMin, xMax, zMin, zMax, cx0, cz0);
		AddClippedVert(sub, i00, i01, i11, i10, xMin, xMax, zMin, zMax, cx0, cz1);
		AddClippedVert(sub, i00, i01, i11, i10, xMin, xMax, zMin, zMax, cx1, cz1);
		AddClippedVert(sub, i00, i01, i11, i10, xMin, xMax, zMin, zMax, cx1, cz0);
		AddQuadTris(sub, count);
	}

	private static void AddQuadTris(LayerSubMesh sub, int vi)
	{
		sub.tris.Add(vi);
		sub.tris.Add(vi + 1);
		sub.tris.Add(vi + 2);
		sub.tris.Add(vi);
		sub.tris.Add(vi + 2);
		sub.tris.Add(vi + 3);
	}

	private void AddClippedVert(LayerSubMesh sub, int i00, int i01, int i11, int i10, float xMin, float xMax, float zMin, float zMax, float x, float z)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		float num = (x - xMin) / (xMax - xMin);
		float num2 = (z - zMin) / (zMax - zMin);
		sub.verts.Add(new Vector3(x, stripAltitude, z));
		sub.uvs.Add(Vector3.Lerp(Vector3.Lerp(qUvs[i00], qUvs[i10], num), Vector3.Lerp(qUvs[i01], qUvs[i11], num), num2));
		sub.colors.Add(Color32.Lerp(Color32.Lerp(qCols[i00], qCols[i10], num), Color32.Lerp(qCols[i01], qCols[i11], num), num2));
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
				LevelRenderer.DrawWallRevealSubMesh(val);
			}
		}
	}
}

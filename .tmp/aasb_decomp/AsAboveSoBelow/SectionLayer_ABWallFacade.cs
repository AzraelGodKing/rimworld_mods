using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public class SectionLayer_ABWallFacade : SectionLayer
{
	private const float Eps = 0.001f;

	private const float DrapeAllowance = 1f;

	private readonly List<int> vertsBefore = new List<int>();

	private readonly List<int> trisBefore = new List<int>();

	private readonly List<Vector4> clipRects = new List<Vector4>();

	private readonly List<Vector3> qVerts = new List<Vector3>();

	private readonly List<Vector3> qUvs = new List<Vector3>();

	private readonly List<Color32> qCols = new List<Color32>();

	private float shiftZ;

	public override bool Visible => false;

	public SectionLayer_ABWallFacade(Section section)
		: base(section)
	{
		((MapDrawLayer)this).relevantChangeTypes = MapMeshFlagDef.op_Implicit(ABDefOf.AB_BelowThings) | MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Terrain);
	}

	public override void Regenerate()
	{
		((MapDrawLayer)this).ClearSubMeshes((MeshParts)63);
	}

	private unsafe void Regenerate_Retired()
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
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
			TerrainDef aB_MountainTop = ABDefOf.AB_MountainTop;
			FogGrid fogGrid = val.fogGrid;
			shiftZ = Mathf.Max(0.25f, 0.08f);
			bool flag = false;
			CellRect cellRect = base.section.CellRect;
			Enumerator enumerator = ((CellRect)(ref cellRect)).GetEnumerator();
			try
			{
				while (((Enumerator)(ref enumerator)).MoveNext())
				{
					IntVec3 current = ((Enumerator)(ref enumerator)).Current;
					if (!GenGrid.InBounds(current, val) || !IsSouthRimCell(map, terrainGrid, current, aB_OpenAir, aB_MountainTop))
					{
						continue;
					}
					Building val2 = val.edificeGrid[current];
					if (!ABRimPrint.QualifiesAsSupport(val2) || (!((Thing)val2).def.seeThroughFog && fogGrid.IsFogged(((Thing)val2).Position)) || !IsFirstQualifyingCell(map, val, terrainGrid, val2, current, aB_OpenAir, aB_MountainTop))
					{
						continue;
					}
					GatherSliverRects(map, val, terrainGrid, val2, aB_OpenAir, aB_MountainTop);
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
						Log.WarningOnce("[As above, So below] Facade print failed for " + ((Entity)val2).LabelCap + ": " + ex.Message, ((Thing)val2).thingIDNumber ^ 0x2D6E2F8B);
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
			ABGuard.Disable(ABGuard.Rendering, e, "wall facade layer");
		}
	}

	private static bool IsSouthRimCell(Map sky, TerrainGrid grid, IntVec3 c, TerrainDef air, TerrainDef cap)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (!GenGrid.InBounds(c, sky))
		{
			return false;
		}
		TerrainDef val = grid.TerrainAt(c);
		if (val == null || val == air || val == cap)
		{
			return false;
		}
		IntVec3 val2 = c + IntVec3.South;
		return GenGrid.InBounds(val2, sky) && grid.TerrainAt(val2) == air;
	}

	private static bool IsFirstQualifyingCell(Map sky, Map lower, TerrainGrid grid, Building ed, IntVec3 c, TerrainDef air, TerrainDef cap)
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
				if (GenGrid.InBounds(val2, lower) && IsSouthRimCell(sky, grid, val2, air, cap))
				{
					return val2.x == c.x && val2.z == c.z;
				}
			}
		}
		return false;
	}

	private void GatherSliverRects(Map sky, Map lower, TerrainGrid grid, Building ed, TerrainDef air, TerrainDef cap)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		clipRects.Clear();
		CellRect val = GenAdj.OccupiedRect((Thing)(object)ed);
		IntVec3 val2 = default(IntVec3);
		for (int i = val.minZ; i <= val.maxZ; i++)
		{
			for (int j = val.minX; j <= val.maxX; j++)
			{
				((IntVec3)(ref val2))._002Ector(j, 0, i);
				if (GenGrid.InBounds(val2, lower) && IsSouthRimCell(sky, grid, val2, air, cap))
				{
					clipRects.Add(new Vector4((float)j, (float)i - 1f, (float)j + 1f, (float)i + shiftZ));
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
					CopyQuadShifted(sub, b);
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
				int count = sub.verts.Count;
				AddClippedVert(sub, num5, num6, num7, num8, num, num2, num3, num4, num9, num10);
				AddClippedVert(sub, num5, num6, num7, num8, num, num2, num3, num4, num9, num12);
				AddClippedVert(sub, num5, num6, num7, num8, num, num2, num3, num4, num11, num12);
				AddClippedVert(sub, num5, num6, num7, num8, num, num2, num3, num4, num11, num10);
				AddQuadTris(sub, count);
				result = true;
			}
		}
		return result;
	}

	private void CopyQuadShifted(LayerSubMesh sub, int b)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		int count = sub.verts.Count;
		for (int i = 0; i < 4; i++)
		{
			Vector3 val = qVerts[b + i];
			sub.verts.Add(new Vector3(val.x, val.y, val.z - shiftZ));
			sub.uvs.Add(qUvs[b + i]);
			sub.colors.Add(qCols[b + i]);
		}
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
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		float num = (x - xMin) / (xMax - xMin);
		float num2 = (z - zMin) / (zMax - zMin);
		float num3 = Mathf.Lerp(Mathf.Lerp(qVerts[i00].y, qVerts[i10].y, num), Mathf.Lerp(qVerts[i01].y, qVerts[i11].y, num), num2);
		sub.verts.Add(new Vector3(x, num3, z - shiftZ));
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
				LevelRenderer.DrawWallFacadeSubMesh(val);
			}
		}
	}
}

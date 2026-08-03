using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
public class SectionLayer_ABCeiling : SectionLayer
{
	private static readonly Material CeilingMat = MaterialPool.MatFrom("Terrain/AB_CeilingHint", ShaderDatabase.Transparent);

	public override bool Visible => ABGuard.On(ABGuard.Rendering) && (ABMod.Settings?.showCeilingHint ?? true);

	public SectionLayer_ABCeiling(Section section)
		: base(section)
	{
		((MapDrawLayer)this).relevantChangeTypes = MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Roofs);
	}

	public unsafe override void Regenerate()
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		((MapDrawLayer)this).ClearSubMeshes((MeshParts)63);
		Map map = base.section.map;
		if (!ABGuard.On(ABGuard.Rendering) || map.Level() != 0)
		{
			return;
		}
		Map val = map.UpperMap();
		if (val == null || val.Disposed)
		{
			return;
		}
		try
		{
			TerrainGrid terrainGrid = val.terrainGrid;
			TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
			TerrainDef aB_RoofSurface = ABDefOf.AB_RoofSurface;
			float num = Altitudes.AltitudeFor((AltitudeLayer)31);
			LayerSubMesh val2 = null;
			CellRect cellRect = base.section.CellRect;
			Enumerator enumerator = ((CellRect)(ref cellRect)).GetEnumerator();
			try
			{
				while (((Enumerator)(ref enumerator)).MoveNext())
				{
					IntVec3 current = ((Enumerator)(ref enumerator)).Current;
					if (!GenGrid.InBounds(current, val))
					{
						continue;
					}
					TerrainDef val3 = terrainGrid.TerrainAt(current);
					if (val3 != aB_OpenAir && val3 != aB_RoofSurface && val3.Removable)
					{
						if (val2 == null)
						{
							val2 = ((MapDrawLayer)this).GetSubMesh(CeilingMat);
						}
						int count = val2.verts.Count;
						val2.verts.Add(new Vector3((float)current.x, num, (float)current.z));
						val2.verts.Add(new Vector3((float)current.x, num, (float)(current.z + 1)));
						val2.verts.Add(new Vector3((float)(current.x + 1), num, (float)(current.z + 1)));
						val2.verts.Add(new Vector3((float)(current.x + 1), num, (float)current.z));
						val2.uvs.Add(new Vector3(0f, 0f, 0f));
						val2.uvs.Add(new Vector3(0f, 1f, 0f));
						val2.uvs.Add(new Vector3(1f, 1f, 0f));
						val2.uvs.Add(new Vector3(1f, 0f, 0f));
						val2.tris.Add(count);
						val2.tris.Add(count + 1);
						val2.tris.Add(count + 2);
						val2.tris.Add(count);
						val2.tris.Add(count + 2);
						val2.tris.Add(count + 3);
					}
				}
			}
			finally
			{
				((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
			}
			if (val2 != null)
			{
				((MapDrawLayer)this).FinalizeMesh((MeshParts)63);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Rendering, e, "ceiling hint layer");
		}
	}
}

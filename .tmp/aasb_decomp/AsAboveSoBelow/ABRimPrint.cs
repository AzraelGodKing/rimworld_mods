using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

internal static class ABRimPrint
{
	internal static bool QualifiesAsSupport(Building ed)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Invalid comparison between Unknown and I4
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Invalid comparison between Unknown and I4
		if (ed == null || ed is Building_ABStairs)
		{
			return false;
		}
		ThingDef def = ((Thing)ed).def;
		if ((int)((BuildableDef)def).passability != 2 || def.mineable)
		{
			return false;
		}
		if (def.building != null && def.building.isNaturalRock)
		{
			return false;
		}
		DrawerType drawerType = def.drawerType;
		return (int)drawerType == 2 || (int)drawerType == 3;
	}

	internal static void Snapshot(List<LayerSubMesh> subs, List<int> vertCounts, List<int> triCounts)
	{
		vertCounts.Clear();
		triCounts.Clear();
		for (int i = 0; i < subs.Count; i++)
		{
			vertCounts.Add(subs[i].verts.Count);
			triCounts.Add(subs[i].tris.Count);
		}
	}

	internal static void Rollback(List<LayerSubMesh> subs, List<int> vertCounts, List<int> triCounts)
	{
		for (int i = 0; i < subs.Count; i++)
		{
			Truncate(subs[i], (i < vertCounts.Count) ? vertCounts[i] : 0, (i < triCounts.Count) ? triCounts[i] : 0);
		}
	}

	internal static bool IsShadowMaterial(Material m)
	{
		return (Object)(object)m != (Object)null && (Object)(object)m == (Object)(object)MatBases.SunShadowFade;
	}

	internal static void DropUnsafeNewGeometry(List<LayerSubMesh> subs, List<int> vertCounts, List<int> triCounts)
	{
		for (int i = 0; i < subs.Count; i++)
		{
			LayerSubMesh val = subs[i];
			int num = ((i < vertCounts.Count) ? vertCounts[i] : 0);
			int triCount = ((i < triCounts.Count) ? triCounts[i] : 0);
			if (val.verts.Count > num && (IsShadowMaterial(val.material) || val.uvs.Count != val.verts.Count || val.colors.Count != val.verts.Count))
			{
				Truncate(val, num, triCount);
			}
		}
	}

	internal static void Truncate(LayerSubMesh sub, int vertCount, int triCount)
	{
		if (sub.verts.Count > vertCount)
		{
			sub.verts.RemoveRange(vertCount, sub.verts.Count - vertCount);
		}
		if (sub.uvs.Count > vertCount)
		{
			sub.uvs.RemoveRange(vertCount, sub.uvs.Count - vertCount);
		}
		if (sub.colors.Count > vertCount)
		{
			sub.colors.RemoveRange(vertCount, sub.colors.Count - vertCount);
		}
		if (sub.tris.Count > triCount)
		{
			sub.tris.RemoveRange(triCount, sub.tris.Count - triCount);
		}
	}
}

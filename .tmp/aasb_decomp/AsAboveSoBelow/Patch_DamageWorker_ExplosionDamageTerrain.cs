using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(DamageWorker), "ExplosionDamageTerrain")]
internal static class Patch_DamageWorker_ExplosionDamageTerrain
{
	private const float RoofPunchThreshold = 40f;

	private static void Postfix(Explosion explosion, IntVec3 c)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.RoofSync) || explosion == null || explosion.damType != DamageDefOf.Bomb)
		{
			return;
		}
		try
		{
			Map map = ((Thing)explosion).Map;
			if (map != null && !map.Disposed && map.Level() == 1 && GenGrid.InBounds(c, map) && map.terrainGrid.TerrainAt(c) == ABDefOf.AB_RoofSurface && !((float)explosion.GetDamageAmountAt(c) < 40f))
			{
				Map val = map.LowerMap();
				if (val != null && !val.Disposed && GenGrid.InBounds(c, val) && val.roofGrid.RoofAt(c) != null)
				{
					val.roofGrid.SetRoof(c, (RoofDef)null);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.RoofSync, e, "bomb rooftop punch");
		}
	}
}

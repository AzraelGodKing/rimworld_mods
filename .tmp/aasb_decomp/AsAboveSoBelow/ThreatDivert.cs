using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

internal static class ThreatDivert
{
	public static bool BasementInfestationEligible(IncidentParms parms, out Map basement, out Map surface)
	{
		basement = null;
		surface = null;
		if (!ABGuard.On(ABGuard.Threats))
		{
			return false;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.threatBasementInfest)
		{
			return false;
		}
		IIncidentTarget target = parms.target;
		Map val = (Map)(object)((target is Map) ? target : null);
		if (val == null || val.Disposed || !val.IsPlayerHome)
		{
			return false;
		}
		LevelComp levelComp = val.Levels();
		if (levelComp == null || levelComp.level != 0)
		{
			return false;
		}
		Map lowerMap = levelComp.lowerMap;
		if (lowerMap == null || lowerMap.Disposed)
		{
			return false;
		}
		if (Faction.OfInsects == null || HiveUtility.TotalSpawnedHivesCount(lowerMap, false) >= 30)
		{
			return false;
		}
		IntVec3 best = default(IntVec3);
		if (!InfestationCellFinder.TryFindCell(ref best, lowerMap) && !TryFindBasementInfestationCell(lowerMap, out best))
		{
			return false;
		}
		surface = val;
		basement = lowerMap;
		return true;
	}

	public static bool TryFindBasementInfestationCell(Map basement, out IntVec3 best)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		best = IntVec3.Invalid;
		List<Building> allBuildingsColonist = basement.listerBuildings.allBuildingsColonist;
		float num = -1f;
		for (int i = 0; i < 220; i++)
		{
			IntVec3 val = CellFinder.RandomCell(basement);
			if (!GenGrid.Standable(val, basement) || GridsUtility.Fogged(val, basement))
			{
				continue;
			}
			RoofDef val2 = basement.roofGrid.RoofAt(val);
			if (val2 == null || !val2.isThickRoof)
			{
				continue;
			}
			float num2 = float.MaxValue;
			for (int j = 0; j < allBuildingsColonist.Count; j++)
			{
				IntVec3 val3 = ((Thing)allBuildingsColonist[j]).Position - val;
				float num3 = ((IntVec3)(ref val3)).LengthHorizontalSquared;
				if (num3 < num2)
				{
					num2 = num3;
				}
			}
			if (allBuildingsColonist.Count <= 0 || !(num2 < 36f))
			{
				float num4 = ((allBuildingsColonist.Count == 0) ? 1f : ((num2 < 2500f) ? num2 : 2500f));
				if (num4 > num)
				{
					num = num4;
					best = val;
				}
			}
		}
		return ((IntVec3)(ref best)).IsValid;
	}

	public static Map SkyDropTarget(IncidentParms parms)
	{
		if (!ABGuard.On(ABGuard.Threats))
		{
			return null;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.threatSkyDrops)
		{
			return null;
		}
		if (parms.faction == null || !FactionUtility.HostileTo(parms.faction, Faction.OfPlayer))
		{
			return null;
		}
		if (!GenText.NullOrEmpty(parms.questTag) || parms.quest != null)
		{
			return null;
		}
		if (((IntVec3)(ref parms.spawnCenter)).IsValid)
		{
			return null;
		}
		RaidStrategyDef raidStrategy = parms.raidStrategy;
		if (((raidStrategy != null) ? raidStrategy.Worker : null) is RaidStrategyWorker_Siege)
		{
			return null;
		}
		IIncidentTarget target = parms.target;
		Map val = (Map)(object)((target is Map) ? target : null);
		if (val == null || val.Disposed || !val.IsPlayerHome)
		{
			return null;
		}
		LevelComp levelComp = val.Levels();
		if (levelComp == null || levelComp.level != 0)
		{
			return null;
		}
		Map upperMap = levelComp.upperMap;
		if (upperMap == null || upperMap.Disposed)
		{
			return null;
		}
		if (!Rand.Chance(settings.threatDivertChance))
		{
			return null;
		}
		IntVec3 val2 = default(IntVec3);
		if (!DropCellFinder.TryFindRaidDropCenterClose(ref val2, upperMap, true, true, true, -1))
		{
			return null;
		}
		return upperMap;
	}
}

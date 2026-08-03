using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

public static class LevelWorkSummary
{
	private sealed class Entry
	{
		public int tick = -99999;

		public bool[] plausible;
	}

	private const int TtlTicks = 600;

	private static readonly Dictionary<int, Entry> cache = new Dictionary<int, Entry>();

	private static int workVersion = 1;

	private static WorkTypeDef wtFirefighter;

	private static WorkTypeDef wtDoctor;

	private static WorkTypeDef wtWarden;

	private static WorkTypeDef wtHandling;

	private static WorkTypeDef wtCooking;

	private static WorkTypeDef wtHunting;

	private static WorkTypeDef wtConstruction;

	private static WorkTypeDef wtGrowing;

	private static WorkTypeDef wtMining;

	private static WorkTypeDef wtPlantCutting;

	private static WorkTypeDef wtHauling;

	private static WorkTypeDef wtCleaning;

	private static WorkTypeDef wtResearch;

	private static WorkTypeDef wtSmithing;

	private static WorkTypeDef wtTailoring;

	private static WorkTypeDef wtCrafting;

	private static WorkTypeDef wtArt;

	private static DesignationDef[] miningDesigs;

	private static DesignationDef[] plantDesigs;

	private static DesignationDef[] constructionDesigs;

	private static DesignationDef[] huntDesigs;

	private static DesignationDef[] tameDesigs;

	private static bool defsResolved;

	public static int WorkVersion => workVersion;

	public static void Notify_WorkChanged(Map map)
	{
		workVersion++;
		if (map != null)
		{
			cache.Remove(map.uniqueID);
		}
	}

	public static bool AnyPlausibleBefore(Map target, List<WorkGiver> order, int stopIndex)
	{
		if (target == null || target.Disposed)
		{
			return false;
		}
		bool[] plausible = GetEntry(target).plausible;
		for (int i = 0; i < stopIndex; i++)
		{
			WorkGiverDef def = order[i].def;
			if (def?.workType != null && !IsOwnCrossLevelGiver(def))
			{
				int index = ((Def)def.workType).index;
				if (index >= plausible.Length || plausible[index])
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool Plausible(Map target, WorkTypeDef workType)
	{
		if (workType == null)
		{
			return true;
		}
		bool[] plausible = GetEntry(target).plausible;
		int index = ((Def)workType).index;
		return index >= plausible.Length || plausible[index];
	}

	public static bool IsOwnCrossLevelGiver(WorkGiverDef def)
	{
		return def.giverClass != null && def.giverClass.Namespace == "AsAboveSoBelow";
	}

	private static void ResolveDefs()
	{
		defsResolved = true;
		wtFirefighter = W("Firefighter");
		wtDoctor = W("Doctor");
		wtWarden = W("Warden");
		wtHandling = W("Handling");
		wtCooking = W("Cooking");
		wtHunting = W("Hunting");
		wtConstruction = W("Construction");
		wtGrowing = W("Growing");
		wtMining = W("Mining");
		wtPlantCutting = W("PlantCutting");
		wtHauling = W("Hauling");
		wtCleaning = W("Cleaning");
		wtResearch = W("Research");
		wtSmithing = W("Smithing");
		wtTailoring = W("Tailoring");
		wtCrafting = W("Crafting");
		wtArt = W("Art");
		miningDesigs = Compact(D("Mine"), D("MineVein"));
		plantDesigs = Compact(D("CutPlant"), D("HarvestPlant"), D("ExtractTree"));
		constructionDesigs = Compact(D("Deconstruct"), D("Uninstall"), D("SmoothWall"), D("SmoothFloor"), D("RemoveFloor"));
		huntDesigs = Compact(D("Hunt"));
		tameDesigs = Compact(D("Tame"), D("Slaughter"), D("ReleaseAnimalToWild"));
		static DesignationDef D(string name)
		{
			return DefDatabase<DesignationDef>.GetNamedSilentFail(name);
		}
		static WorkTypeDef W(string name)
		{
			return DefDatabase<WorkTypeDef>.GetNamedSilentFail(name);
		}
	}

	private static DesignationDef[] Compact(params DesignationDef[] defs)
	{
		int num = 0;
		for (int i = 0; i < defs.Length; i++)
		{
			if (defs[i] != null)
			{
				defs[num++] = defs[i];
			}
		}
		DesignationDef[] array = (DesignationDef[])(object)new DesignationDef[num];
		for (int j = 0; j < num; j++)
		{
			array[j] = defs[j];
		}
		return array;
	}

	private static Entry GetEntry(Map map)
	{
		int ticksGame = Find.TickManager.TicksGame;
		if (cache.TryGetValue(map.uniqueID, out var value) && ticksGame - value.tick < 600)
		{
			return value;
		}
		if (!defsResolved)
		{
			ResolveDefs();
		}
		if (value == null)
		{
			if (cache.Count > 64)
			{
				cache.Clear();
			}
			value = new Entry();
			cache[map.uniqueID] = value;
		}
		Build(map, value);
		value.tick = ticksGame;
		return value;
	}

	private static void Set(bool[] plausible, WorkTypeDef wt, bool value)
	{
		if (wt != null && ((Def)wt).index < plausible.Length)
		{
			plausible[((Def)wt).index] = value;
		}
	}

	private static bool AnyDesig(Map map, DesignationDef[] defs)
	{
		for (int i = 0; i < defs.Length; i++)
		{
			if (map.designationManager.AnySpawnedDesignationOfDef(defs[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static void Build(Map map, Entry entry)
	{
		int defCount = DefDatabase<WorkTypeDef>.DefCount;
		bool[] array = entry.plausible;
		if (array == null || array.Length != defCount)
		{
			array = (entry.plausible = new bool[defCount]);
		}
		for (int i = 0; i < defCount; i++)
		{
			array[i] = true;
		}
		Set(array, wtFirefighter, map.listerThings.ThingsOfDef(ThingDefOf.Fire).Count > 0);
		Set(array, wtMining, AnyDesig(map, miningDesigs));
		Set(array, wtPlantCutting, AnyDesig(map, plantDesigs));
		Set(array, wtHunting, AnyDesig(map, huntDesigs));
		Set(array, wtHauling, map.listerHaulables.ThingsPotentiallyNeedingHauling().Count > 0);
		Set(array, wtCleaning, map.listerFilthInHomeArea.FilthInHomeArea.Count > 0);
		bool flag = false;
		bool flag2 = map.mapPawns.PrisonersOfColonySpawned.Count > 0;
		if (flag2)
		{
			flag = AnyPatient(map.mapPawns.PrisonersOfColonySpawned);
		}
		bool flag3 = false;
		List<Pawn> list = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
		for (int j = 0; j < list.Count; j++)
		{
			Pawn val = list[j];
			if (!flag && IsPatient(val))
			{
				flag = true;
			}
			if (!flag3 && val.RaceProps != null && val.RaceProps.Animal)
			{
				flag3 = true;
			}
			if (flag && flag3)
			{
				break;
			}
		}
		Set(array, wtDoctor, flag);
		Set(array, wtWarden, flag2);
		Set(array, wtHandling, flag3 || AnyDesig(map, tameDesigs));
		bool value = map.listerThings.ThingsInGroup((ThingRequestGroup)9).Count > 0 || map.listerThings.ThingsInGroup((ThingRequestGroup)11).Count > 0 || map.listerBuildingsRepairable.RepairableBuildings(Faction.OfPlayer).Count > 0 || AnyDesig(map, constructionDesigs);
		Set(array, wtConstruction, value);
		bool flag4 = false;
		List<Zone> allZones = map.zoneManager.AllZones;
		for (int k = 0; k < allZones.Count; k++)
		{
			if (allZones[k] is Zone_Growing)
			{
				flag4 = true;
				break;
			}
		}
		bool flag5 = false;
		bool flag6 = false;
		List<Building> allBuildingsColonist = map.listerBuildings.allBuildingsColonist;
		for (int l = 0; l < allBuildingsColonist.Count; l++)
		{
			Building val2 = allBuildingsColonist[l];
			if (!flag4 && val2 is Building_PlantGrower)
			{
				flag4 = true;
			}
			else if (!flag6 && val2 is Building_ResearchBench)
			{
				flag6 = true;
			}
			else if (!flag5)
			{
				Building_WorkTable val3 = (Building_WorkTable)(object)((val2 is Building_WorkTable) ? val2 : null);
				if (val3 != null && val3.CurrentlyUsableForBills() && AnyActiveBill(val3))
				{
					flag5 = true;
				}
			}
			if (flag4 && flag5 && flag6)
			{
				break;
			}
		}
		Set(array, wtGrowing, flag4);
		Set(array, wtResearch, flag6 && Find.ResearchManager.GetProject((KnowledgeCategoryDef)null) != null);
		Set(array, wtCooking, flag5);
		Set(array, wtSmithing, flag5);
		Set(array, wtTailoring, flag5);
		Set(array, wtCrafting, flag5);
		Set(array, wtArt, flag5);
	}

	private static bool AnyActiveBill(Building_WorkTable table)
	{
		BillStack billStack = table.BillStack;
		if (billStack == null)
		{
			return false;
		}
		for (int i = 0; i < billStack.Count; i++)
		{
			if (billStack[i].ShouldDoNow())
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsPatient(Pawn p)
	{
		if (p.health == null)
		{
			return false;
		}
		if (p.Downed && !RestUtility.InBed(p))
		{
			return true;
		}
		if (p.health.HasHediffsNeedingTendByPlayer(false))
		{
			return true;
		}
		return RestUtility.InBed(p) && HealthAIUtility.ShouldSeekMedicalRest(p);
	}

	private static bool AnyPatient(List<Pawn> list)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (IsPatient(list[i]))
			{
				return true;
			}
		}
		return false;
	}
}

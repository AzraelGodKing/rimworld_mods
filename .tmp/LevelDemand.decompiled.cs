using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata;

public static class LevelDemand
{
	internal class Entry
	{
		public int tick;

		public readonly Dictionary<ThingDef, int> missing = new Dictionary<ThingDef, int>();

		public readonly Dictionary<ThingDef, List<IntVec3>> sites = new Dictionary<ThingDef, List<IntVec3>>();

		public void AddShortfall(ThingDef def, int amount, IntVec3 site)
		{
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			missing.TryGetValue(def, out var value);
			missing[def] = value + amount;
			if (!sites.TryGetValue(def, out var value2))
			{
				value2 = (sites[def] = new List<IntVec3>());
			}
			value2.Add(site);
		}
	}

	private const int CacheTicks = 250;

	private static readonly Dictionary<Map, Entry> cache = new Dictionary<Map, Entry>();

	public static int MissingOn(Map map, ThingDef def)
	{
		if (!Get(map).missing.TryGetValue(def, out var value))
		{
			return 0;
		}
		return value;
	}

	public static bool AnySiteReachable(Map map, ThingDef def, IntVec3 from)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		if (!((IntVec3)(ref from)).IsValid || !GenGrid.InBounds(from, map) || !Get(map).sites.TryGetValue(def, out var value))
		{
			return false;
		}
		for (int i = 0; i < value.Count; i++)
		{
			if (map.reachability.CanReach(from, LocalTargetInfo.op_Implicit(value[i]), (PathEndMode)2, TraverseParms.For((TraverseMode)1, (Danger)3, false, false, false, true, false)))
			{
				return true;
			}
		}
		return false;
	}

	public static HashSet<ThingDef> DefsWantedByLinkedLevels(Map from)
	{
		HashSet<ThingDef> hashSet = new HashSet<ThingDef>();
		List<LevelGraph.LevelLink> list = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(from));
		for (int i = 0; i < list.Count; i++)
		{
			foreach (KeyValuePair<ThingDef, int> item in Get(list[i].map).missing)
			{
				hashSet.Add(item.Key);
			}
		}
		return hashSet;
	}

	private static Entry Get(Map map)
	{
		if (cache.TryGetValue(map, out var value) && Find.TickManager.TicksGame - value.tick < 250)
		{
			return value;
		}
		value = Build(map);
		cache[map] = value;
		PruneDeadMaps();
		return value;
	}

	private static Entry Build(Map map)
	{
		Entry entry = new Entry
		{
			tick = Find.TickManager.TicksGame
		};
		AddConstructibles(entry, map, map.listerThings.ThingsInGroup((ThingRequestGroup)9));
		AddConstructibles(entry, map, map.listerThings.ThingsInGroup((ThingRequestGroup)11));
		BillIngredientUtility.AddShortfalls(entry, map);
		if (entry.missing.Count > 0)
		{
			foreach (ThingDef item in new List<ThingDef>(entry.missing.Keys))
			{
				int num = (item.CountAsResource ? StrataResources.RawGetCount(map, item) : CountOnMap(map, item));
				int num2 = entry.missing[item] - num;
				if (num2 > 0)
				{
					entry.missing[item] = num2;
					continue;
				}
				entry.missing.Remove(item);
				entry.sites.Remove(item);
			}
		}
		AddRefuelShortfalls(entry, map);
		AddStorageUpgradePulls(entry, map);
		return entry;
	}

	private static void AddRefuelShortfalls(Entry entry, Map map)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		List<Thing> list = map.listerThings.ThingsInGroup((ThingRequestGroup)10);
		for (int i = 0; i < list.Count; i++)
		{
			Thing val = list[i];
			if (val.Faction != Faction.OfPlayer)
			{
				continue;
			}
			ThingWithComps val2 = (ThingWithComps)(object)((val is ThingWithComps) ? val : null);
			if (val2 == null)
			{
				continue;
			}
			CompRefuelable comp = val2.GetComp<CompRefuelable>();
			if (comp == null || !comp.ShouldAutoRefuelNow)
			{
				continue;
			}
			int fuelCountToFullyRefuel = comp.GetFuelCountToFullyRefuel();
			if (fuelCountToFullyRefuel <= 0)
			{
				continue;
			}
			ThingFilter val3 = comp.Props?.fuelFilter;
			if (val3 == null)
			{
				continue;
			}
			foreach (ThingDef allowedThingDef in val3.AllowedThingDefs)
			{
				if (allowedThingDef != null && !allowedThingDef.IsCorpse)
				{
					entry.AddShortfall(allowedThingDef, fuelCountToFullyRefuel, val.Position);
				}
			}
		}
	}

	private static void AddStorageUpgradePulls(Entry entry, Map map)
	{
		List<SlotGroup> allGroupsListForReading = map.haulDestinationManager.AllGroupsListForReading;
		if (allGroupsListForReading.Count == 0)
		{
			return;
		}
		List<LevelGraph.LevelLink> list = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(map));
		for (int i = 0; i < list.Count; i++)
		{
			Map map2 = list[i].map;
			if (map2 == null || map2 == map)
			{
				continue;
			}
			int num = 0;
			foreach (Thing item in map2.listerHaulables.ThingsPotentiallyNeedingHauling())
			{
				if (num >= 100)
				{
					break;
				}
				num++;
				ConsiderOneStorageCandidate(entry, map, allGroupsListForReading, item);
			}
			List<SlotGroup> allGroupsListForReading2 = map2.haulDestinationManager.AllGroupsListForReading;
			for (int j = 0; j < allGroupsListForReading2.Count; j++)
			{
				if (num >= 100)
				{
					break;
				}
				foreach (Thing heldThing in allGroupsListForReading2[j].HeldThings)
				{
					if (num >= 100)
					{
						break;
					}
					num++;
					ConsiderOneStorageCandidate(entry, map, allGroupsListForReading, heldThing);
				}
			}
		}
	}

	private static void ConsiderOneStorageCandidate(Entry entry, Map destMap, List<SlotGroup> destGroups, Thing t)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if (t != null && t.Spawned && (int)t.def.category == 2)
		{
			StoragePriority above = StoreUtility.CurrentStoragePriorityOf(t, false);
			if (TryBestAcceptingCell(destMap, destGroups, t, above, out var cell, out var _))
			{
				entry.AddShortfall(t.def, t.stackCount, cell);
			}
		}
	}

	private static bool TryBestAcceptingCell(Map map, List<SlotGroup> groups, Thing t, StoragePriority above, out IntVec3 cell, out StoragePriority priority)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected I4, but got Unknown
		cell = IntVec3.Invalid;
		priority = (StoragePriority)0;
		for (int i = 0; i < groups.Count; i++)
		{
			SlotGroup val = groups[i];
			StoragePriority priority2 = val.Settings.Priority;
			if (priority2 <= above || (int)priority2 <= (int)priority || !val.Settings.AllowedToAccept(t))
			{
				continue;
			}
			List<IntVec3> cellsList = val.CellsList;
			int num = Math.Min(cellsList.Count, 120);
			for (int j = 0; j < num; j++)
			{
				IntVec3 val2 = cellsList[j];
				if (CellHasStackRoom(val2, map, t))
				{
					cell = val2;
					priority = (StoragePriority)(int)priority2;
					break;
				}
			}
		}
		return ((IntVec3)(ref cell)).IsValid;
	}

	private static bool CellHasStackRoom(IntVec3 cell, Map map, Thing t)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		List<Thing> thingList = GridsUtility.GetThingList(cell, map);
		for (int i = 0; i < thingList.Count; i++)
		{
			Thing val = thingList[i];
			if (val.def.EverStorable(false))
			{
				if (val.CanStackWith(t))
				{
					return val.stackCount < val.def.stackLimit;
				}
				return false;
			}
		}
		return true;
	}

	private static void AddConstructibles(Entry entry, Map map, List<Thing> things)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < things.Count; i++)
		{
			Thing val = things[i];
			if (val.Faction != Faction.OfPlayer || val is Blueprint_Install)
			{
				continue;
			}
			IConstructible val2 = (IConstructible)(object)((val is IConstructible) ? val : null);
			if (val2 == null)
			{
				continue;
			}
			List<ThingDefCountClass> list = val2.TotalMaterialCost();
			for (int j = 0; j < list.Count; j++)
			{
				ThingDef thingDef = list[j].thingDef;
				int num = val2.ThingCountNeeded(thingDef);
				if (num > 0)
				{
					entry.missing.TryGetValue(thingDef, out var value);
					entry.missing[thingDef] = value + num;
					if (!entry.sites.TryGetValue(thingDef, out var value2))
					{
						value2 = (entry.sites[thingDef] = new List<IntVec3>());
					}
					value2.Add(val.Position);
				}
			}
		}
	}

	private static int CountOnMap(Map map, ThingDef def)
	{
		List<Thing> list = map.listerThings.ThingsOfDef(def);
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			num += list[i].stackCount;
		}
		return num;
	}

	private static void PruneDeadMaps()
	{
		if (cache.Count <= Find.Maps.Count)
		{
			return;
		}
		List<Map> list = new List<Map>();
		foreach (Map key in cache.Keys)
		{
			if (!Find.Maps.Contains(key))
			{
				list.Add(key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			cache.Remove(list[i]);
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '9.1.0.7988')

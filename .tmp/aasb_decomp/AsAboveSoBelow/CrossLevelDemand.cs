using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public static class CrossLevelDemand
{
	private class CacheEntry
	{
		public int tick;

		public readonly Dictionary<ThingDef, int> need = new Dictionary<ThingDef, int>();

		public readonly Dictionary<ThingDef, int> available = new Dictionary<ThingDef, int>();
	}

	private const int CacheTtlTicks = 600;

	private const int MaxBillsPerMap = 30;

	private const int MaxDefsPerBill = 40;

	private static readonly Dictionary<int, CacheEntry> cache = new Dictionary<int, CacheEntry>();

	private const int MealsPerMouth = 2;

	private static ThingDef[] mealDefs;

	public static bool Demands(Map map, ThingDef def)
	{
		if (map == null || map.Disposed || def == null)
		{
			return false;
		}
		CacheEntry entry = GetEntry(map);
		int value;
		return entry.need.TryGetValue(def, out value) && value > 0;
	}

	public static Thing FindFetchableDemand(Map demandMap, Map sourceMap, Pawn pawn, bool requireReachable)
	{
		if (demandMap == null || demandMap.Disposed || sourceMap == null || sourceMap.Disposed || pawn == null)
		{
			return null;
		}
		CacheEntry entry = GetEntry(demandMap);
		foreach (KeyValuePair<ThingDef, int> item in entry.need)
		{
			if (item.Value <= 0)
			{
				continue;
			}
			List<Thing> list = sourceMap.listerThings.ThingsOfDef(item.Key);
			for (int i = 0; i < list.Count; i++)
			{
				Thing val = list[i];
				if (val != null && val.Spawned && val.Map == sourceMap && !ForbidUtility.IsForbidden(val, pawn) && ExportAllowed(sourceMap, val) && (!requireReachable || HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, val, false)))
				{
					return val;
				}
			}
		}
		return null;
	}

	public static bool HasFetchableDemand(Map demandMap, Map sourceMap, Pawn pawn)
	{
		return FindFetchableDemand(demandMap, sourceMap, pawn, requireReachable: false) != null;
	}

	public static bool ExportAllowed(Map map, Thing t)
	{
		if (map == null || map.Disposed || t?.def == null)
		{
			return true;
		}
		CacheEntry entry = GetEntry(map);
		if (!entry.need.TryGetValue(t.def, out var value) || value <= 0)
		{
			return true;
		}
		entry.available.TryGetValue(t.def, out var value2);
		return value2 - t.stackCount >= value;
	}

	private static CacheEntry GetEntry(Map map)
	{
		int ticksGame = Find.TickManager.TicksGame;
		if (cache.TryGetValue(map.uniqueID, out var value) && ticksGame - value.tick < 600)
		{
			return value;
		}
		if (cache.Count > 64)
		{
			cache.Clear();
		}
		value = new CacheEntry
		{
			tick = ticksGame
		};
		AddFrom(map.listerThings.ThingsInGroup((ThingRequestGroup)9), value.need);
		AddFrom(map.listerThings.ThingsInGroup((ThingRequestGroup)11), value.need);
		AddBillNeeds(map, value);
		AddMealNeeds(map, value);
		foreach (KeyValuePair<ThingDef, int> item in value.need)
		{
			if (!value.available.ContainsKey(item.Key))
			{
				value.available[item.Key] = CountOnMap(map, item.Key);
			}
		}
		cache[map.uniqueID] = value;
		return value;
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

	private static void AddBillNeeds(Map map, CacheEntry entry)
	{
		List<Building> allBuildingsColonist = map.listerBuildings.allBuildingsColonist;
		int num = 0;
		for (int i = 0; i < allBuildingsColonist.Count; i++)
		{
			Building obj = allBuildingsColonist[i];
			Building_WorkTable val = (Building_WorkTable)(object)((obj is Building_WorkTable) ? obj : null);
			if (val == null || !val.CurrentlyUsableForBills())
			{
				continue;
			}
			BillStack billStack = val.BillStack;
			if (billStack == null)
			{
				continue;
			}
			for (int j = 0; j < billStack.Count; j++)
			{
				Bill obj2 = billStack[j];
				Bill_Production val2 = (Bill_Production)(object)((obj2 is Bill_Production) ? obj2 : null);
				if (val2 == null || !((Bill)val2).ShouldDoNow())
				{
					continue;
				}
				if (++num > 30)
				{
					return;
				}
				List<IngredientCount> ingredients = ((Bill)val2).recipe.ingredients;
				for (int k = 0; k < ingredients.Count; k++)
				{
					IngredientCount val3 = ingredients[k];
					if (val3.IsFixedIngredient)
					{
						ThingDef fixedIngredient = val3.FixedIngredient;
						if (fixedIngredient != null)
						{
							int num2 = val3.CountRequiredOfFor(fixedIngredient, ((Bill)val2).recipe, (Bill)(object)val2);
							int num3 = num2 - Available(map, entry, fixedIngredient);
							if (num3 > 0)
							{
								entry.need.TryGetValue(fixedIngredient, out var value);
								entry.need[fixedIngredient] = value + num3;
							}
						}
						continue;
					}
					int num4 = 0;
					int num5 = 0;
					int num6 = 0;
					foreach (ThingDef allowedThingDef in val3.filter.AllowedThingDefs)
					{
						if (((Bill)val2).ingredientFilter.Allows(allowedThingDef))
						{
							if (++num4 > 40)
							{
								break;
							}
							num5 += Available(map, entry, allowedThingDef);
							if (num6 == 0)
							{
								num6 = val3.CountRequiredOfFor(allowedThingDef, ((Bill)val2).recipe, (Bill)(object)val2);
							}
						}
					}
					int num7 = num6 - num5;
					if (num7 <= 0)
					{
						continue;
					}
					num4 = 0;
					foreach (ThingDef allowedThingDef2 in val3.filter.AllowedThingDefs)
					{
						if (((Bill)val2).ingredientFilter.Allows(allowedThingDef2))
						{
							if (++num4 > 40)
							{
								break;
							}
							entry.need.TryGetValue(allowedThingDef2, out var value2);
							entry.need[allowedThingDef2] = value2 + num7;
						}
					}
				}
			}
		}
	}

	private static void AddMealNeeds(Map map, CacheEntry entry)
	{
		int num = 0;
		IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
		for (int i = 0; i < allPawnsSpawned.Count; i++)
		{
			Pawn val = allPawnsSpawned[i];
			if (val.IsPrisonerOfColony || (((Thing)val).Faction == Faction.OfPlayer && RestUtility.InBed(val) && HealthAIUtility.ShouldSeekMedicalRest(val)))
			{
				num++;
			}
		}
		if (num == 0)
		{
			return;
		}
		int num2 = num * 2;
		object obj = mealDefs;
		if (obj == null)
		{
			obj = new ThingDef[5]
			{
				ThingDefOf.MealSimple,
				ThingDefOf.MealFine,
				ThingDefOf.MealNutrientPaste,
				ThingDefOf.MealSurvivalPack,
				ThingDefOf.Pemmican
			};
			mealDefs = (ThingDef[])obj;
		}
		ThingDef[] array = (ThingDef[])obj;
		int num3 = 0;
		for (int j = 0; j < array.Length; j++)
		{
			if (array[j] != null)
			{
				num3 += Available(map, entry, array[j]);
			}
		}
		int num4 = num2 - num3;
		if (num4 <= 0)
		{
			return;
		}
		for (int k = 0; k < array.Length; k++)
		{
			if (array[k] != null)
			{
				entry.need.TryGetValue(array[k], out var value);
				entry.need[array[k]] = value + num4;
			}
		}
	}

	private static int Available(Map map, CacheEntry entry, ThingDef def)
	{
		if (entry.available.TryGetValue(def, out var value))
		{
			return value;
		}
		value = CountOnMap(map, def);
		entry.available[def] = value;
		return value;
	}

	private static void AddFrom(List<Thing> things, Dictionary<ThingDef, int> need)
	{
		for (int i = 0; i < things.Count; i++)
		{
			if (things[i] is Blueprint_Install)
			{
				continue;
			}
			Thing obj = things[i];
			IConstructible val = (IConstructible)(object)((obj is IConstructible) ? obj : null);
			if (val == null || things[i].Faction != Faction.OfPlayer)
			{
				continue;
			}
			List<ThingDefCountClass> list = val.TotalMaterialCost();
			for (int j = 0; j < list.Count; j++)
			{
				ThingDef thingDef = list[j].thingDef;
				if (thingDef != null)
				{
					int num = val.ThingCountNeeded(thingDef);
					if (num > 0)
					{
						need.TryGetValue(thingDef, out var value);
						need[thingDef] = value + num;
					}
				}
			}
		}
	}
}

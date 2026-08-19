using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Nudge CompSpawner timers once per 250 ticks so bloom / rain / coop settings
    /// scale output instead of stalling the countdown every CompTick.
    /// </summary>
    public static class CompSpawnerBias
    {
        public const int Pulse = 250;

        private static readonly FieldInfo TicksUntilSpawn =
            AccessTools.Field(typeof(CompSpawner), "ticksUntilSpawn");

        public static void ApplyFactor(ThingWithComps parent, float factor)
        {
            if (parent == null || TicksUntilSpawn == null || Mathf.Abs(factor - 1f) < 0.02f)
            {
                return;
            }

            factor = Mathf.Clamp(factor, 0.35f, 3f);
            int extra = Mathf.RoundToInt(Pulse * (1f - (1f / factor)));
            if (extra == 0)
            {
                return;
            }

            List<ThingComp> comps = parent.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (!(comps[i] is CompSpawner spawner))
                {
                    continue;
                }

                int current = (int)TicksUntilSpawn.GetValue(spawner);
                TicksUntilSpawn.SetValue(spawner, Mathf.Max(1, current + extra));
            }
        }
    }

    public static class BloomUtility
    {
        public const int Radius = 8;

        public static bool HasBloom(Thing hive)
        {
            if (hive?.Map == null)
            {
                return false;
            }

            Map map = hive.Map;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(hive.Position, Radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                Plant plant = cell.GetPlant(map);
                if (plant?.def?.plant == null || plant.LifeStage != PlantLifeStage.Mature)
                {
                    continue;
                }

                if (plant.def.plant.harvestedThingDef != null || plant.def.plant.purpose == PlantPurpose.Beauty)
                {
                    return true;
                }
            }

            return false;
        }

        public static string Inspect(Thing hive)
        {
            return HasBloom(hive)
                ? "Homesteader_BeesInBloom".Translate()
                : "Homesteader_BeesNothingInBloom".Translate();
        }
    }

    public static class RainAware
    {
        public static float FillFactor(Thing catcher)
        {
            if (catcher?.Map == null)
            {
                return 1f;
            }

            Map map = catcher.Map;
            if (StormproofSoftCompat.IsDrought(map) && !StormproofSoftCompat.DroughtProtected(map))
            {
                MaybeEmptyOutdoorWater(catcher);
                return 2.2f;
            }

            float rain = map.weatherManager.RainRate;
            if (rain >= 0.7f)
            {
                return 0.45f;
            }

            if (rain >= 0.15f)
            {
                return 0.7f;
            }

            return 1.25f;
        }

        public static string Inspect(Thing catcher)
        {
            if (catcher?.Map == null)
            {
                return null;
            }

            if (StormproofSoftCompat.IsDrought(catcher.Map))
            {
                return StormproofSoftCompat.DroughtProtected(catcher.Map)
                    ? "Homesteader_WaterDroughtCondenser".Translate()
                    : "Homesteader_WaterDrought".Translate();
            }

            float rain = catcher.Map.weatherManager.RainRate;
            if (rain >= 0.7f)
            {
                return "Homesteader_WaterStormCatch".Translate();
            }

            if (rain >= 0.15f)
            {
                return "Homesteader_WaterRaining".Translate();
            }

            return "Homesteader_WaterDryCatch".Translate();
        }

        private static void MaybeEmptyOutdoorWater(Thing catcher)
        {
            if (Find.TickManager.TicksGame % 2500 != 0 || catcher.Map == null)
            {
                return;
            }

            Room room = catcher.GetRoom();
            if (room != null && !room.PsychologicallyOutdoors)
            {
                return;
            }

            ThingDef water = DefDatabase<ThingDef>.GetNamedSilentFail("Wellspring_Water");
            if (water == null)
            {
                return;
            }

            foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(catcher))
            {
                if (!cell.InBounds(catcher.Map))
                {
                    continue;
                }

                List<Thing> things = catcher.Map.thingGrid.ThingsListAt(cell);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t.def != water || t.stackCount < 1)
                    {
                        continue;
                    }

                    t.SplitOff(1).Destroy(DestroyMode.Vanish);
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.YieldNow))]
    public static class Patch_MapleSugaringSeason
    {
        public static void Postfix(Plant __instance, ref int __result)
        {
            if (__instance?.def?.defName != "Homesteader_Plant_MapleTree" || __result <= 0)
            {
                return;
            }

            Vector2 longlat = Find.WorldGrid.LongLatOf(__instance.Map.Tile);
            Season season = GenDate.Season(Find.TickManager.TicksAbs, longlat);
            float temp = __instance.Map.mapTemperature.OutdoorTemp;
            float mul = 1f;
            if (season == Season.Spring || temp < 8f)
            {
                mul = 2.2f;
            }
            else if (season == Season.Winter)
            {
                mul = 1.5f;
            }
            else if (season == Season.Fall)
            {
                mul = 0.6f;
            }
            else if (season == Season.Summer || temp > 20f)
            {
                mul = 0.12f;
            }

            __result = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(__result * mul));
        }
    }
}

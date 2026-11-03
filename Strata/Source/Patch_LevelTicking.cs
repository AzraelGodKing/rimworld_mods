using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // A deep base is many full maps, and every one runs the ambient sims
    // (temperature, gas, weather upkeep, wildlife) each tick. Two tiers:
    //   Throttled   - vacant A+/B+ pocket level: most PreTick work runs 1 tick in 4.
    //   Lightweight - any A+/B+ pocket level: vanilla systems that do nothing
    //                 useful on a Strata level (wild spawners, underground
    //                 weather/wind/snow) are skipped. Everything Strata reads
    //                 (gasGrid, lords, MapComponents, power, temperature,
    //                 resource counter, fire watcher) keeps running.
    public static class LevelTicking
    {
        public static bool ShouldThrottle(Map map)
        {
            return StrataLevelPerfUtility.ShouldThrottleAmbient(map);
        }

        public static bool SkipWildSpawners(Map map)
        {
            return StrataLevelPerfUtility.ShouldTickLightweight(map);
        }

        // Underground only: no sky, rock roof everywhere, biome pins Clear and
        // noAmbientWind. A+ decks keep weather (WeatherSync transitions need
        // WeatherManagerTick), wind (turbines) and snow/deterioration.
        public static bool SkipUndergroundAmbient(Map map)
        {
            return StrataLevelPerfUtility.ShouldTickLightweight(map) && StrataMapUtility.IsUnderground(map);
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.MapPreTick))]
    public static class Patch_MapPreTick
    {
        // 1.6 ticks power nets in MapPostTick, which always runs. Only tick them
        // here when vanilla PreTick is the one that does it, or vacant levels
        // would charge and drain batteries twice per tick.
        private static bool? preTickOwnsPowerNets;

        public static bool Prefix(Map __instance)
        {
            if (!LevelTicking.ShouldThrottle(__instance))
            {
                return true;
            }
            // Shaft power sets CompPowerShaft.PowerOutput from the host side, so
            // vacant pockets still need their power nets ticked.
            if (PreTickOwnsPowerNets())
            {
                __instance.powerNetManager?.PowerNetsTick();
            }
            return false;
        }

        private static bool PreTickOwnsPowerNets()
        {
            if (preTickOwnsPowerNets.HasValue)
            {
                return preTickOwnsPowerNets.Value;
            }
            bool owns = true;
            try
            {
                MethodInfo powerTick = AccessTools.Method(typeof(PowerNetManager), nameof(PowerNetManager.PowerNetsTick));
                MethodInfo preTick = AccessTools.Method(typeof(Map), nameof(Map.MapPreTick));
                owns = PatchProcessor.GetOriginalInstructions(preTick)
                    .Any(ins => ins.operand is MethodInfo m && m == powerTick);
            }
            catch (System.Exception e)
            {
                StrataLog.Verbose("[Strata] Could not read MapPreTick IL, keeping power tick: " + e.Message);
            }
            preTickOwnsPowerNets = owns;
            return owns;
        }
    }

    // MapPostTick always runs so smoke, raid pursuit, and other MapComponents
    // stay live on vacant underground levels. The patches below trim single
    // vanilla systems instead of replacing PostTick, so systems a game update
    // adds to PostTick keep running untouched.

    [HarmonyPatch(typeof(WildAnimalSpawner), nameof(WildAnimalSpawner.WildAnimalSpawnerTick))]
    public static class Patch_WildAnimalSpawnerTick_Lightweight
    {
        public static bool Prefix(Map ___map)
        {
            return !LevelTicking.SkipWildSpawners(___map);
        }
    }

    [HarmonyPatch(typeof(WildPlantSpawner), nameof(WildPlantSpawner.WildPlantSpawnerTick))]
    public static class Patch_WildPlantSpawnerTick_Lightweight
    {
        public static bool Prefix(Map ___map)
        {
            return !LevelTicking.SkipWildSpawners(___map);
        }
    }

    [HarmonyPatch(typeof(SteadyEnvironmentEffects), nameof(SteadyEnvironmentEffects.SteadyEnvironmentEffectsTick))]
    public static class Patch_SteadyEnvironmentEffectsTick_Lightweight
    {
        public static bool Prefix(Map ___map)
        {
            return !LevelTicking.SkipUndergroundAmbient(___map);
        }
    }

    [HarmonyPatch(typeof(WeatherManager), nameof(WeatherManager.WeatherManagerTick))]
    public static class Patch_WeatherManagerTick_Lightweight
    {
        public static bool Prefix(WeatherManager __instance)
        {
            return !LevelTicking.SkipUndergroundAmbient(__instance.map);
        }
    }

    [HarmonyPatch(typeof(WindManager), nameof(WindManager.WindManagerTick))]
    public static class Patch_WindManagerTick_Lightweight
    {
        public static bool Prefix(Map ___map)
        {
            return !LevelTicking.SkipUndergroundAmbient(___map);
        }
    }
}

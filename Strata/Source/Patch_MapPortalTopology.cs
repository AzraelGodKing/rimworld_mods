using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // ReachableLevels and route caches assume portal topology is stable between
    // invalidations; bust them when a portal spawns or is removed.
    [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
    public static class Patch_MapPortalSpawnTopology
    {
        public static void Postfix(Thing __instance, bool respawningAfterLoad)
        {
            if (!respawningAfterLoad && __instance is MapPortal)
            {
                LevelGraph.InvalidateCache();
            }
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
    public static class Patch_MapPortalDeSpawnTopology
    {
        public static void Postfix(Thing __instance)
        {
            if (__instance is MapPortal)
            {
                LevelGraph.InvalidateCache();
            }
        }
    }
}

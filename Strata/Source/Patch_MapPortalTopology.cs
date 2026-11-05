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
            if (!respawningAfterLoad && __instance is MapPortal portal)
            {
                LevelGraph.InvalidateCache();
                // A level still generating reports its portals once it is done.
                if (portal.Map != MapGenerator.mapBeingGenerated)
                {
                    StrataLevelEvents.AnnounceLink(portal);
                }
            }
        }
    }

    // Fires the StrataModderApi level events.
    [HarmonyPatch(typeof(PocketMapUtility), nameof(PocketMapUtility.GeneratePocketMap))]
    public static class Patch_GeneratePocketMap_LevelCreated
    {
        public static void Postfix(Map __result)
        {
            if (__result == null || !StrataModderApi.IsLevel(__result))
            {
                return;
            }
            StrataModderApi.NotifyLevelCreated(__result);
            foreach (Thing thing in __result.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                StrataLevelEvents.AnnounceLink(thing as MapPortal);
            }
        }
    }

    internal static class StrataLevelEvents
    {
        internal static void AnnounceLink(MapPortal portal)
        {
            if (portal == null || !LevelGraph.IsLevelPortal(portal))
            {
                return;
            }
            Map other = LevelGraph.OtherMapSafe(portal);
            if (other != null)
            {
                StrataModderApi.NotifyVerticalLink(portal.Map, other, portal);
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

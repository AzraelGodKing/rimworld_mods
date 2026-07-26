using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Force-build / deliver-resources when materials sit on another linked floor.
    public static class StrataConstructAcrossLevels
    {
        private struct PendingFetch
        {
            public int matThingId;
            public int matMapId;
            public int siteMapId;
            public int count;
            public bool forced;
        }

        private static readonly Dictionary<int, PendingFetch> pending = new Dictionary<int, PendingFetch>();
        private static readonly AccessTools.FieldRef<ItemAvailability, Map> itemAvailMap =
            AccessTools.FieldRefAccess<ItemAvailability, Map>("map");

        public static void ResetSession() => pending.Clear();

        public static Map MapOf(ItemAvailability availability) => itemAvailMap(availability);

        public static bool ThingsAvailableOnLinkedLevels(Map map, ThingDef need, int amount, Pawn pawn)
        {
            if (map == null || need == null || amount <= 0) return false;
            int have = 0;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                Map other = link.map;
                if (other == null || other.Disposed) continue;
                have += CountAvailableOnMap(other, need, pawn);
                if (have >= amount) return true;
            }
            return false;
        }

        public static int CountAvailableOnMap(Map map, ThingDef need, Pawn pawn)
        {
            if (map == null || need == null) return 0;
            List<Thing> list = map.listerThings.ThingsOfDef(need);
            int n = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (t == null || t.Destroyed || !t.Spawned) continue;
                if (pawn != null && t.IsForbidden(pawn)) continue;
                n += t.stackCount;
            }
            return n;
        }

        public static bool ColumnCanSupply(Thing site, Pawn pawn, bool forced)
        {
            if (site is not IConstructible constructible || pawn?.Map == null || site.Map == null)
            {
                return false;
            }
            if (site is Blueprint_Install) return true;
            if (site.Map != pawn.Map && !ColonyBedUtility.MapsLinked(site.Map, pawn.Map))
            {
                return false;
            }

            List<ThingDefCountClass> costs = constructible.TotalMaterialCost();
            if (costs.NullOrEmpty()) return true;

            bool anyNeeded = false;
            for (int i = 0; i < costs.Count; i++)
            {
                ThingDefCountClass need = costs[i];
                int want = ThingCountNeeded(constructible, need.thingDef, pawn, forced);
                if (want <= 0) continue;
                anyNeeded = true;

                int have = CountAvailableOnMap(site.Map, need.thingDef, pawn);
                if (have < want)
                {
                    have += CountAvailableOnLinked(site.Map, need.thingDef, pawn);
                }
                // Pawn may be standing on the material floor already.
                if (have < want && pawn.Map != site.Map)
                {
                    have += CountAvailableOnMap(pawn.Map, need.thingDef, pawn);
                }
                if (have < want) return false;
            }
            return anyNeeded;
        }

        private static int CountAvailableOnLinked(Map map, ThingDef need, Pawn pawn)
        {
            int n = 0;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                n += CountAvailableOnMap(link.map, need, pawn);
            }
            return n;
        }

        private static int ThingCountNeeded(IConstructible c, ThingDef need, Pawn pawn, bool forced)
        {
            if (!forced && c is IHaulEnroute enroute)
            {
                return enroute.GetSpaceRemainingWithEnroute(need, pawn);
            }
            return c.ThingCountNeeded(need);
        }

        public static Job TryMakeFetchJob(Pawn pawn, IConstructible c, bool forced)
        {
            try
            {
                if (pawn?.Map == null || c is not Thing site || site.Map == null) return null;
                if (site is Blueprint_Install) return null;
                if (!StrataPawnUtility.CanUseLevelPortals(pawn)) return null;
                if (site.Map != pawn.Map && !ColonyBedUtility.MapsLinked(site.Map, pawn.Map))
                {
                    return null;
                }

                List<ThingDefCountClass> costs = c.TotalMaterialCost();
                if (costs.NullOrEmpty()) return null;

                for (int i = 0; i < costs.Count; i++)
                {
                    ThingDefCountClass need = costs[i];
                    int want = ThingCountNeeded(c, need.thingDef, pawn, forced);
                    if (want <= 0) continue;

                    // Local reachable resources → leave to vanilla.
                    if (CountAvailableOnMap(site.Map, need.thingDef, pawn) >= want
                        && ClosestOnMap(pawn, site.Map, need.thingDef, forced) != null)
                    {
                        continue;
                    }

                    Thing mat = FindBestMaterial(pawn, need.thingDef, site.Map, excludeMap: null);
                    if (mat == null) continue;

                    int take = Math.Min(mat.stackCount, want);
                    Job job = MakeFetchOrCommute(pawn, mat, site.Map, take, forced);
                    if (job != null) return job;
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Cross-level construct fetch failed: " + e.Message, 0x5B7100D);
            }
            return null;
        }

        private static Thing ClosestOnMap(Pawn pawn, Map map, ThingDef def, bool forced)
        {
            if (pawn.Map != map) return null;
            return GenClosest.ClosestThingReachable(
                pawn.Position,
                map,
                ThingRequest.ForDef(def),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn, forced ? Danger.Deadly : pawn.NormalMaxDanger()),
                9999f,
                t => t != null && !t.IsForbidden(pawn)
                    && HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced));
        }

        private static Thing FindBestMaterial(Pawn pawn, ThingDef def, Map siteMap, Map excludeMap)
        {
            Thing best = null;
            int bestDist = int.MaxValue;

            void ConsiderMap(Map map)
            {
                if (map == null || map.Disposed || map == excludeMap) return;
                List<Thing> list = map.listerThings.ThingsOfDef(def);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing t = list[i];
                    if (t == null || !t.Spawned || t.IsForbidden(pawn)) continue;
                    int dist = (t.Position - (map == pawn.Map ? pawn.Position : t.Position)).LengthHorizontalSquared;
                    // Prefer same map as pawn, then nearer stacks.
                    if (map == pawn.Map) dist -= 1_000_000;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = t;
                    }
                }
            }

            ConsiderMap(pawn.Map);
            if (siteMap != pawn.Map) ConsiderMap(siteMap);
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(siteMap ?? pawn.Map))
            {
                ConsiderMap(link.map);
            }
            return best;
        }

        private static Job MakeFetchOrCommute(Pawn pawn, Thing mat, Map siteMap, int count, bool forced)
        {
            Map matMap = mat.Map;
            if (matMap == null || siteMap == null) return null;

            if (pawn.Map == matMap)
            {
                MapPortal portal = LevelGraph.BestFirstStep(matMap, siteMap, pawn.Position, pawn);
                if (portal == null) return null;
                HaulToLevelTargets.Remember(pawn, siteMap, matMap);
                Job haul = JobMaker.MakeJob(StrataDefOf.Strata_HaulToLevel, mat, portal);
                haul.count = count;
                haul.playerForced = forced;
                return haul;
            }

            // Commute to the material floor, then haul back to the site.
            if (LevelGraph.BestFirstStep(pawn.Map, matMap, pawn.Position, pawn) == null)
            {
                return null;
            }

            pending[pawn.thingIDNumber] = new PendingFetch
            {
                matThingId = mat.thingIDNumber,
                matMapId = matMap.uniqueID,
                siteMapId = siteMap.uniqueID,
                count = count,
                forced = forced,
            };

            Job hop = PawnRelay.TryRelayToMap(
                pawn,
                matMap,
                touchCooldown: false,
                RelayPurpose.ForcedOrder,
                preferArrivalNear: mat.Position);
            if (hop == null)
            {
                pending.Remove(pawn.thingIDNumber);
                return null;
            }
            hop.playerForced = forced;
            return hop;
        }

        /// <summary>
        /// After stair arrival for a construct-material commute, start HaulToLevel.
        /// </summary>
        public static bool TryFinishFetch(Pawn pawn)
        {
            if (pawn?.Map == null || !pending.TryGetValue(pawn.thingIDNumber, out PendingFetch fetch))
            {
                return false;
            }
            pending.Remove(pawn.thingIDNumber);

            Map matMap = FindMap(fetch.matMapId);
            Map siteMap = FindMap(fetch.siteMapId);
            if (matMap == null || siteMap == null || pawn.Map != matMap) return false;

            Thing mat = FindThing(matMap, fetch.matThingId);
            if (mat == null || !mat.Spawned)
            {
                // Stack moved/merged — pick any matching def still needed on site.
                mat = FindBestMaterial(pawn, null, siteMap, excludeMap: null);
                // Without def we can't recover easily; scan site needs would need re-query.
                return false;
            }

            MapPortal portal = LevelGraph.BestFirstStep(matMap, siteMap, pawn.Position, pawn);
            if (portal == null) return false;

            HaulToLevelTargets.Remember(pawn, siteMap, matMap);
            Job haul = JobMaker.MakeJob(StrataDefOf.Strata_HaulToLevel, mat, portal);
            haul.count = Math.Min(mat.stackCount, Math.Max(1, fetch.count));
            haul.playerForced = fetch.forced;
            return pawn.jobs.TryTakeOrderedJob(haul, JobTag.MiscWork);
        }

        private static Map FindMap(int id)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].uniqueID == id) return maps[i];
            }
            return null;
        }

        private static Thing FindThing(Map map, int id)
        {
            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].thingIDNumber == id) return things[i];
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(ItemAvailability), nameof(ItemAvailability.ThingsAvailableAnywhere))]
    public static class Patch_ItemAvailability_LinkedLevels
    {
        public static void Postfix(ItemAvailability __instance, ThingDef need, int amount, Pawn pawn, ref bool __result)
        {
            if (__result || need == null || amount <= 0) return;
            Map map = StrataConstructAcrossLevels.MapOf(__instance);
            if (map == null) return;
            if (StrataConstructAcrossLevels.ThingsAvailableOnLinkedLevels(map, need, amount, pawn))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.CanGetResources_NewTemp))]
    public static class Patch_GenConstruct_CanGetResources_Linked
    {
        public static void Postfix(Thing thing, Pawn pawn, bool forced, ref bool __result)
        {
            if (__result || thing == null || pawn == null) return;
            if (StrataConstructAcrossLevels.ColumnCanSupply(thing, pawn, forced))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ResourceDeliverJobFor")]
    public static class Patch_ConstructDeliver_AcrossLevels
    {
        public static void Postfix(
            Pawn pawn,
            IConstructible c,
            bool canRemoveExistingFloorUnderNearbyNeeders,
            bool forced,
            ref Job __result)
        {
            if (__result != null || pawn == null || c == null) return;
            Job fetch = StrataConstructAcrossLevels.TryMakeFetchJob(pawn, c, forced);
            if (fetch != null)
            {
                __result = fetch;
            }
        }
    }
}

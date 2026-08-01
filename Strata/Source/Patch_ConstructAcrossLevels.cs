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
            public int siteThingId;
            public ThingDef matDef;
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
                // Reserved stacks are not available — counting them made fetch jobs
                // target wood another builder already claimed (StartJob reserve spam).
                if (pawn != null && !pawn.CanReserve(t)) continue;
                n += t.stackCount;
            }
            return n;
        }

        /// <summary>
        /// Pawn can walk stairs to the constructible's floor (float-menu force-build).
        /// </summary>
        public static bool CanCommuteToSite(Pawn pawn, Thing site)
        {
            if (pawn?.Map == null || site?.Map == null || !pawn.Spawned)
            {
                return false;
            }
            if (site.Map == pawn.Map)
            {
                return true;
            }
            if (!StrataPawnUtility.CanUseLevelPortals(pawn))
            {
                return false;
            }
            if (!ColonyBedUtility.MapsLinked(pawn.Map, site.Map))
            {
                return false;
            }
            return LevelGraph.BestFirstStep(pawn.Map, site.Map, pawn.Position, pawn) != null;
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

                    Thing mat = FindBestMaterial(pawn, need.thingDef, site.Map, excludeMap: null, forced);
                    if (mat == null) continue;

                    int take = Math.Min(mat.stackCount, want);
                    Job job = MakeFetchOrCommute(pawn, mat, site, take, forced);
                    if (job != null) return job;
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Cross-level construct fetch failed: " + e.Message, 0x5B7100D);
            }
            return null;
        }

        /// <summary>
        /// Vanilla HaulToContainer with materials on the pawn's map and the
        /// constructible on another linked floor — rewrite to stair haul / commute.
        /// </summary>
        public static Job TryRewriteCrossMapDeliver(Pawn pawn, IConstructible c, Job job, bool forced)
        {
            if (pawn?.Map == null || c is not Thing site || site.Map == null || job == null)
            {
                return null;
            }
            if (job.def != JobDefOf.HaulToContainer)
            {
                return null;
            }
            if (site.Map == pawn.Map || !ColonyBedUtility.MapsLinked(pawn.Map, site.Map))
            {
                return null;
            }
            if (!StrataPawnUtility.CanUseLevelPortals(pawn))
            {
                return null;
            }

            Thing mat = job.targetA.Thing;
            if (mat == null || !mat.Spawned)
            {
                return null;
            }

            int count = job.count > 0 ? job.count : Math.Min(mat.stackCount, 1);
            bool playerForced = forced || job.playerForced;
            try
            {
                return MakeFetchOrCommute(pawn, mat, site, count, playerForced);
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Cross-map deliver rewrite failed: " + e.Message, 0x5B7100E);
                return null;
            }
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

        private static Thing FindBestMaterial(
            Pawn pawn,
            ThingDef def,
            Map siteMap,
            Map excludeMap,
            bool forced)
        {
            if (pawn == null || def == null) return null;
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
                    if (!pawn.CanReserve(t)) continue;
                    if (map == pawn.Map
                        && !HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced))
                    {
                        continue;
                    }
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

        private static Job MakeFetchOrCommute(Pawn pawn, Thing mat, Thing site, int count, bool forced)
        {
            Map siteMap = site?.Map;
            Map matMap = mat.Map;
            if (matMap == null || siteMap == null || site == null) return null;
            if (mat.Spawned && !pawn.CanReserve(mat)) return null;

            if (pawn.Map == matMap)
            {
                // Already on the material floor — haul through stairs to the site,
                // or deliver straight into the frame when material is on-site.
                if (matMap == siteMap)
                {
                    if (!pawn.CanReserveAndReach(site, PathEndMode.Touch, Danger.Deadly))
                    {
                        return null;
                    }
                    Job local = JobMaker.MakeJob(JobDefOf.HaulToContainer, mat, site);
                    local.count = count;
                    local.haulMode = HaulMode.ToContainer;
                    local.playerForced = forced;
                    return local;
                }
                MapPortal portal = LevelGraph.BestFirstStep(matMap, siteMap, pawn.Position, pawn);
                if (portal == null) return null;
                HaulToLevelTargets.Remember(pawn, siteMap, matMap, site);
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
                matDef = mat.def,
                count = count,
                forced = forced,
                siteThingId = site.thingIDNumber,
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
            if (mat == null || !mat.Spawned || (mat.Spawned && !pawn.CanReserve(mat)))
            {
                // Stack moved/merged/claimed — recover another free stack of the same def.
                if (fetch.matDef == null) return false;
                mat = FindBestMaterial(pawn, fetch.matDef, siteMap, excludeMap: null, fetch.forced);
                if (mat == null || !mat.Spawned) return false;
                matMap = mat.Map;
                if (matMap == null) return false;

                // Replacement is on another linked floor — start a fresh commute
                // (pending was cleared above; MakeFetchOrCommute re-arms it).
                if (pawn.Map != matMap)
                {
                    Thing siteForHop = fetch.siteThingId > 0
                        ? StrataPortalUtility.FindThingByIdAcrossMaps(fetch.siteThingId)
                        : null;
                    if (siteForHop == null || siteForHop.Map == null) return false;
                    Job hop = MakeFetchOrCommute(
                        pawn,
                        mat,
                        siteForHop,
                        Math.Min(mat.stackCount, Math.Max(1, fetch.count)),
                        fetch.forced);
                    return hop != null && pawn.jobs.TryTakeOrderedJob(hop, JobTag.MiscWork);
                }
            }

            Thing site = fetch.siteThingId > 0
                ? StrataPortalUtility.FindThingByIdAcrossMaps(fetch.siteThingId)
                : null;

            // Materials landed on the build floor — deliver into the frame directly.
            if (matMap == siteMap && site != null && site.Map == matMap)
            {
                if (!pawn.CanReserve(mat)
                    || !pawn.CanReserveAndReach(site, PathEndMode.Touch, Danger.Deadly))
                {
                    return false;
                }
                Job deliver = JobMaker.MakeJob(JobDefOf.HaulToContainer, mat, site);
                deliver.count = Math.Min(mat.stackCount, Math.Max(1, fetch.count));
                deliver.haulMode = HaulMode.ToContainer;
                deliver.playerForced = fetch.forced;
                return pawn.jobs.TryTakeOrderedJob(deliver, JobTag.MiscWork);
            }

            if (!pawn.CanReserve(mat)) return false;

            MapPortal portal = LevelGraph.BestFirstStep(matMap, siteMap, pawn.Position, pawn);
            if (portal == null) return false;

            HaulToLevelTargets.Remember(pawn, siteMap, matMap, site);
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

    // Vanilla RCellFinder requires the worker on the same map as the blueprint/frame,
    // so prioritize-build never appears when the pawn is on another linked floor.
    [HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.CanTouchTargetFromValidCell))]
    public static class Patch_GenConstruct_CanTouch_Linked
    {
        public static void Postfix(Thing constructible, Pawn worker, ref bool __result)
        {
            if (__result || constructible == null || worker == null) return;
            if (constructible.Map == null || constructible.Map == worker.Map) return;
            if (StrataConstructAcrossLevels.CanCommuteToSite(worker, constructible))
            {
                __result = true;
            }
        }
    }

    // Same-map CanReserveAndReach / wrong-map reservationManager fail for off-floor
    // pawns. Re-validate non-path checks and allow the commute + CrossLevelOrderedJobs.
    [HarmonyPatch(
        typeof(GenConstruct),
        nameof(GenConstruct.CanConstruct),
        new[] { typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef) })]
    public static class Patch_GenConstruct_CanConstruct_Linked
    {
        public static void Postfix(
            Thing t,
            Pawn p,
            bool checkSkills,
            bool forced,
            JobDef jobForReservation,
            ref bool __result)
        {
            if (__result || t?.Map == null || p?.Map == null || t.Map == p.Map) return;
            if (!StrataConstructAcrossLevels.CanCommuteToSite(p, t)) return;
            if (GenConstruct.FirstBlockingThing(t, p) != null) return;
            if (t.IsBurning()) return;

            if (jobForReservation != null)
            {
                if (!t.Map.reservationManager.OnlyReservationsForJobDef(t, jobForReservation))
                {
                    Pawn reserver = t.Map.reservationManager.FirstRespectedReserver(t, p);
                    if (reserver != null)
                    {
                        JobFailReason.Is("ReservedBy".Translate(reserver.LabelShort, reserver));
                    }
                    return;
                }
            }
            else if (!forced)
            {
                Pawn reserver = t.Map.reservationManager.FirstRespectedReserver(t, p);
                if (reserver != null && reserver != p)
                {
                    JobFailReason.Is("ReservedBy".Translate(reserver.LabelShort, reserver));
                    return;
                }
            }

            if (checkSkills)
            {
                if (p.skills != null)
                {
                    if (p.skills.GetSkill(SkillDefOf.Construction).Level < t.def.constructionSkillPrerequisite)
                    {
                        return;
                    }
                    if (p.skills.GetSkill(SkillDefOf.Artistic).Level < t.def.artisticSkillPrerequisite)
                    {
                        return;
                    }
                }
                if (p.IsColonyMech)
                {
                    if (p.RaceProps.mechFixedSkillLevel < t.def.constructionSkillPrerequisite)
                    {
                        return;
                    }
                    if (p.RaceProps.mechFixedSkillLevel < t.def.artisticSkillPrerequisite)
                    {
                        return;
                    }
                }
            }

            // Match vanilla Ideology / history / attachment gates (skipped by early
            // CanTouch / CanReserveAndReach failure on other floors).
            bool install = t is Blueprint_Install;
            if (p.Ideo != null && !p.Ideo.MembersCanBuild(t) && !install)
            {
                return;
            }
            if ((t.def.IsBlueprint || t.def.IsFrame) && t.def.entityDefToBuild is ThingDef thingDef)
            {
                if (thingDef.building != null && thingDef.building.isAttachment)
                {
                    Thing wall = GenConstruct.GetWallAttachedTo(t);
                    if (wall == null || wall.def.IsBlueprint || wall.def.IsFrame)
                    {
                        return;
                    }
                }
                NamedArgument doer = p.Named(HistoryEventArgsNames.Doer);
                if (!new HistoryEvent(
                        HistoryEventDefOf.BuildSpecificDef,
                        doer,
                        NamedArgumentUtility.Named(thingDef, HistoryEventArgsNames.Building))
                    .Notify_PawnAboutToDo_Job())
                {
                    return;
                }
                if (thingDef.building != null
                    && thingDef.building.IsTurret
                    && !thingDef.HasComp(typeof(CompMannable))
                    && !new HistoryEvent(HistoryEventDefOf.BuiltAutomatedTurret, doer)
                        .Notify_PawnAboutToDo_Job())
                {
                    return;
                }
            }

            __result = true;
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
            if (pawn == null || c == null) return;

            // Vanilla often emits HaulToContainer with mat on the pawn's floor and
            // the frame/blueprint elsewhere — that job cannot path. Rewrite.
            if (__result != null)
            {
                Job rewritten = StrataConstructAcrossLevels.TryRewriteCrossMapDeliver(
                    pawn, c, __result, forced);
                if (rewritten != null)
                {
                    __result = rewritten;
                    return;
                }

                // Same-map race: another pawn already claimed the stack between
                // WorkGiver scan and StartJob. Drop the job so we don't Warning-spam
                // TryMakePreToilReservations / Could not reserve.
                if (__result.def == JobDefOf.HaulToContainer)
                {
                    Thing mat = __result.targetA.Thing;
                    if (mat != null && mat.Spawned && !pawn.CanReserve(mat))
                    {
                        __result = null;
                    }
                }

                if (__result != null)
                {
                    return;
                }
            }

            Job fetch = StrataConstructAcrossLevels.TryMakeFetchJob(pawn, c, forced);
            if (fetch != null)
            {
                __result = fetch;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // A generator, torch or reactor whose floor has no fuel left: the refueller
    // walks to the floor that has it, carries a load back through the stairs
    // and refuels that building. LevelDemand already stocks fuel ahead of time;
    // this covers the floor that ran dry before the haulers caught up.
    public static class RefuelAcrossLevels
    {
        private struct PendingFetch
        {
            public int fuelThingId;
            public int fuelMapId;
            public int siteThingId;
            public int count;
            public bool forced;
        }

        // A refuelable whose fetch fell through waits this long before another try.
        private const int FailCooldownTicks = 2500;
        private const int LookupCacheTicks = 250;

        private static readonly Dictionary<int, PendingFetch> pending = new Dictionary<int, PendingFetch>();
        private static readonly Dictionary<int, int> deliverTo = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> cooldownUntil = new Dictionary<int, int>();
        private static readonly Dictionary<int, KeyValuePair<int, Thing>> lookupCache =
            new Dictionary<int, KeyValuePair<int, Thing>>();

        // Vanilla's own private fuel searches, so "has fuel here" matches what
        // CanRefuel saw. If a game update renames them, the feature stays off.
        private static readonly Func<Pawn, Thing, Thing> FindBestFuelLocal =
            MakeDelegate<Func<Pawn, Thing, Thing>>("FindBestFuel");

        private static readonly Func<Pawn, Thing, List<Thing>> FindAllFuelLocal =
            MakeDelegate<Func<Pawn, Thing, List<Thing>>>("FindAllFuel");

        private static T MakeDelegate<T>(string name) where T : Delegate
        {
            try
            {
                System.Reflection.MethodInfo method = AccessTools.Method(typeof(RefuelWorkGiverUtility), name);
                return method == null ? null : AccessTools.MethodDelegate<T>(method);
            }
            catch (Exception e)
            {
                Log.Warning("[Strata] Cross-level refuel is off: RefuelWorkGiverUtility." + name + " not found (" + e.Message + ").");
                return null;
            }
        }

        internal static void ResetSession()
        {
            pending.Clear();
            deliverTo.Clear();
            cooldownUntil.Clear();
            lookupCache.Clear();
        }

        private static bool Enabled =>
            FindBestFuelLocal != null && FindAllFuelLocal != null
            && (StrataMod.Settings == null || StrataMod.Settings.crossLevelRefuelEnabled);

        private static int Now => Find.TickManager?.TicksGame ?? 0;

        // Vanilla CanRefuel said no. True when the only thing missing is fuel on
        // this floor and a linked floor has some.
        public static bool CanFetchElsewhere(Pawn pawn, Thing t, bool forced)
        {
            if (!Enabled || pawn?.Map == null || t?.Map == null || pawn.Map != t.Map
                || CrossFloorJobScan.IsScanning
                || !StrataPawnUtility.CanUseLevelPortals(pawn)
                || !LevelGraph.AnyLinkFrom(t.Map))
            {
                return false;
            }
            if (cooldownUntil.TryGetValue(t.thingIDNumber, out int until) && Now < until)
            {
                return false;
            }
            if (!PassesNonFuelChecks(pawn, t, forced) || HasLocalFuel(pawn, t))
            {
                return false;
            }
            return FuelElsewhere(pawn, t) != null;
        }

        // Same gates as vanilla CanRefuel, short of the fuel search.
        private static bool PassesNonFuelChecks(Pawn pawn, Thing t, bool forced)
        {
            CompRefuelable refuel = t.TryGetComp<CompRefuelable>();
            if (refuel == null || t.Fogged() || refuel.IsFull || (!forced && !refuel.allowAutoRefuel))
            {
                return false;
            }
            if (refuel.FuelPercentOfMax > 0f && !refuel.Props.allowRefuelIfNotEmpty)
            {
                return false;
            }
            if (!forced && !refuel.ShouldAutoRefuelNow)
            {
                return false;
            }
            if (t.Faction != pawn.Faction || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            CompInteractable interactable = t.TryGetComp<CompInteractable>();
            return interactable == null || !interactable.Props.cooldownPreventsRefuel || !interactable.OnCooldown;
        }

        private static bool HasLocalFuel(Pawn pawn, Thing t)
        {
            if (FindBestFuelLocal(pawn, t) == null)
            {
                return false;
            }
            return !t.TryGetComp<CompRefuelable>().Props.atomicFueling || FindAllFuelLocal(pawn, t) != null;
        }

        public static bool NeedsFetch(Pawn pawn, Thing t)
        {
            return Enabled && pawn?.Map != null && t?.Map == pawn.Map
                && !CrossFloorJobScan.IsScanning && !HasLocalFuel(pawn, t);
        }

        // Nearest usable stack on a linked floor, cached briefly per building:
        // the work scan asks for every refuelable each pass.
        private static Thing FuelElsewhere(Pawn pawn, Thing t)
        {
            int now = Now;
            if (lookupCache.TryGetValue(t.thingIDNumber, out KeyValuePair<int, Thing> hit)
                && now - hit.Key < LookupCacheTicks
                && (hit.Value == null || (hit.Value.Spawned && CanTake(pawn, hit.Value))))
            {
                return hit.Value;
            }

            ThingFilter filter = t.TryGetComp<CompRefuelable>()?.Props?.fuelFilter;
            Thing best = null;
            if (filter != null)
            {
                List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(t.Map);
                float bestCost = float.MaxValue;
                for (int l = 0; l < links.Count && best == null; l++)
                {
                    LevelGraph.LevelLink link = links[l];
                    Map map = link.map;
                    if (map == null || map == t.Map || map.Disposed)
                    {
                        continue;
                    }
                    // Links come cheapest floor first; take the nearest stack on
                    // the first floor that has one.
                    List<Thing> things = map.listerThings.ThingsMatching(filter.BestThingRequest);
                    for (int i = 0; i < things.Count; i++)
                    {
                        Thing fuel = things[i];
                        if (!filter.Allows(fuel) || !CanTake(pawn, fuel))
                        {
                            continue;
                        }
                        if (link.arrivalCell.IsValid && link.arrivalCell.InBounds(map)
                            && !map.reachability.CanReach(link.arrivalCell, fuel, PathEndMode.ClosestTouch,
                                TraverseParms.For(TraverseMode.PassDoors)))
                        {
                            continue;
                        }
                        float cost = link.arrivalCell.IsValid
                            ? link.arrivalCell.DistanceToSquared(fuel.Position)
                            : 0f;
                        if (cost < bestCost)
                        {
                            bestCost = cost;
                            best = fuel;
                        }
                    }
                }
            }
            lookupCache[t.thingIDNumber] = new KeyValuePair<int, Thing>(now, best);
            return best;
        }

        private static bool CanTake(Pawn pawn, Thing fuel)
        {
            return fuel.Spawned && !fuel.Fogged() && !fuel.IsForbidden(pawn)
                && StrataConstructAcrossLevels.CanReserveFromHere(pawn, fuel);
        }

        // Walk to the fuel's floor. TryFinishFetch picks it up on arrival.
        public static Job MakeFetchJob(Pawn pawn, Thing t, bool forced)
        {
            Thing fuel = FuelElsewhere(pawn, t);
            CompRefuelable refuel = t.TryGetComp<CompRefuelable>();
            if (fuel == null || refuel == null)
            {
                return null;
            }

            pending[pawn.thingIDNumber] = new PendingFetch
            {
                fuelThingId = fuel.thingIDNumber,
                fuelMapId = fuel.Map.uniqueID,
                siteThingId = t.thingIDNumber,
                count = Math.Max(1, Math.Min(fuel.stackCount, refuel.GetFuelCountToFullyRefuel())),
                forced = forced,
            };
            Job hop = PawnRelay.TryRelayToMap(
                pawn,
                fuel.Map,
                touchCooldown: false,
                RelayPurpose.ForcedOrder,
                preferArrivalNear: fuel.Position);
            if (hop == null)
            {
                pending.Remove(pawn.thingIDNumber);
                Cooldown(t);
                return null;
            }
            hop.playerForced = forced;
            StrataLog.Verbose("[Strata] Refuel fetch: " + pawn.LabelShort + " goes to " + fuel.Map
                + " for " + fuel.LabelShort + " to refuel " + t.LabelShort + ".");
            return hop;
        }

        // On the fuel's floor: carry a load back through the stairs.
        public static bool TryFinishFetch(Pawn pawn)
        {
            if (pawn?.Map == null || !pending.TryGetValue(pawn.thingIDNumber, out PendingFetch fetch))
            {
                return false;
            }
            pending.Remove(pawn.thingIDNumber);

            Thing site = StrataThingLookup.FindAcrossMaps(fetch.siteThingId);
            CompRefuelable refuel = site?.TryGetComp<CompRefuelable>();
            if (site?.Map == null || refuel == null || refuel.IsFull || pawn.Map.uniqueID != fetch.fuelMapId)
            {
                return false;
            }

            Thing fuel = StrataThingLookup.FindOnMap(pawn.Map, fetch.fuelThingId);
            if (fuel == null || !CanTake(pawn, fuel) || !pawn.CanReach(fuel, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                ThingFilter filter = refuel.Props.fuelFilter;
                fuel = GenClosest.ClosestThingReachable(
                    pawn.Position,
                    pawn.Map,
                    filter.BestThingRequest,
                    PathEndMode.ClosestTouch,
                    TraverseParms.For(pawn),
                    9999f,
                    x => filter.Allows(x) && CanTake(pawn, x));
            }
            if (fuel == null)
            {
                Cooldown(site);
                StrataLog.Verbose("[Strata] Refuel fetch: " + pawn.LabelShort + " found no fuel left on " + pawn.Map + ".");
                return false;
            }

            MapPortal portal = LevelGraph.BestFirstStep(pawn.Map, site.Map, pawn.Position, pawn, site.Position);
            if (portal == null)
            {
                Cooldown(site);
                return false;
            }

            HaulToLevelTargets.Remember(pawn, site.Map, returnMap: null, preferArrivalNear: site.Position);
            Job haul = JobMaker.MakeJob(StrataDefOf.Strata_HaulToLevel, fuel, portal);
            haul.count = Math.Max(1, Math.Min(fuel.stackCount,
                Math.Max(fetch.count, refuel.GetFuelCountToFullyRefuel())));
            haul.playerForced = fetch.forced;
            if (!pawn.jobs.TryTakeOrderedJob(haul, JobTag.MiscWork))
            {
                Cooldown(site);
                return false;
            }
            deliverTo[pawn.thingIDNumber] = site.thingIDNumber;
            return true;
        }

        // Back on the building's floor with fuel in hand: refuel that building
        // first, before any other delivery claims the load.
        internal static Job TryMakeDeliveryJob(Pawn pawn, Thing cargo)
        {
            if (pawn?.Map == null || cargo == null || !deliverTo.TryGetValue(pawn.thingIDNumber, out int siteId))
            {
                return null;
            }
            deliverTo.Remove(pawn.thingIDNumber);

            Thing site = StrataThingLookup.FindOnMap(pawn.Map, siteId);
            CompRefuelable refuel = site?.TryGetComp<CompRefuelable>();
            if (refuel?.Props == null || refuel.IsFull || !refuel.Props.fuelFilter.Allows(cargo)
                || !pawn.CanReserveAndReach(site, PathEndMode.Touch, Danger.Deadly))
            {
                return null;
            }
            if (refuel.Props.atomicFueling)
            {
                Job atomic = JobMaker.MakeJob(JobDefOf.RefuelAtomic, site);
                atomic.targetQueueB = new List<LocalTargetInfo> { cargo };
                atomic.count = cargo.stackCount;
                return atomic;
            }
            Job job = JobMaker.MakeJob(JobDefOf.Refuel, site, cargo);
            job.count = Math.Min(cargo.stackCount, refuel.GetFuelCountToFullyRefuel());
            return job;
        }

        private static void Cooldown(Thing site)
        {
            if (site != null)
            {
                cooldownUntil[site.thingIDNumber] = Now + FailCooldownTicks;
            }
        }
    }

    [HarmonyPatch(typeof(RefuelWorkGiverUtility), nameof(RefuelWorkGiverUtility.CanRefuel))]
    public static class Patch_CanRefuel_AcrossLevels
    {
        public static void Postfix(Pawn pawn, Thing t, bool forced, ref bool __result)
        {
            if (__result)
            {
                return;
            }
            try
            {
                if (RefuelAcrossLevels.CanFetchElsewhere(pawn, t, forced))
                {
                    __result = true;
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Cross-level refuel check failed: " + e.Message, 0x5B71046);
            }
        }
    }

    [HarmonyPatch(typeof(RefuelWorkGiverUtility), nameof(RefuelWorkGiverUtility.RefuelJob))]
    public static class Patch_RefuelJob_AcrossLevels
    {
        public static bool Prefix(Pawn pawn, Thing t, bool forced, ref Job __result)
        {
            try
            {
                if (!RefuelAcrossLevels.NeedsFetch(pawn, t))
                {
                    return true;
                }
                // CanRefuel only said yes because a linked floor has fuel.
                __result = RefuelAcrossLevels.MakeFetchJob(pawn, t, forced);
                return false;
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Cross-level refuel job failed: " + e.Message, 0x5B71047);
                return true;
            }
        }
    }
}

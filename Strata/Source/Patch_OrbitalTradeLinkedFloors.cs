using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Orbital trade is per-map. Beacons underground are invisible from a
    // surface console, and pods aimed at a roofed floor vanish on arrival.
    public static class StrataOrbitalTrade
    {
        private static bool aggregating;
        private static bool redirecting;

        public static bool Aggregating => aggregating;

        public static IEnumerable<Thing> LaunchablesOn(Map map, ITrader trader)
        {
            if (map == null)
            {
                yield break;
            }

            foreach (Building_OrbitalTradeBeacon beacon in Building_OrbitalTradeBeacon.AllPowered(map))
            {
                foreach (IntVec3 cell in beacon.TradeableCells)
                {
                    List<Thing> things = cell.GetThingList(map);
                    for (int i = 0; i < things.Count; i++)
                    {
                        Thing thing = things[i];
                        if (thing.def.category == ThingCategory.Item
                            && TradeUtility.PlayerSellableNow(thing, trader)
                            && !cell.Fogged(map))
                        {
                            yield return thing;
                        }
                    }
                }
            }
        }

        public static IEnumerable<Thing> LaunchablesAcrossLinkedFloors(Map map, ITrader trader)
        {
            if (map == null)
            {
                yield break;
            }

            foreach (Thing thing in LaunchablesOn(map, trader))
            {
                yield return thing;
            }

            if (!LevelGraph.AnyLinkFrom(map))
            {
                yield break;
            }

            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                Map other = link.map;
                if (other == null || other == map)
                {
                    continue;
                }

                foreach (Thing thing in LaunchablesOn(other, trader))
                {
                    yield return thing;
                }
            }
        }

        public static Map LandingMap(Map requested)
        {
            if (CanReceiveDropPods(requested))
            {
                return requested;
            }

            Map surface = ColonyBedUtility.FindSurfacePlayerHome(requested);
            if (CanReceiveDropPods(surface))
            {
                return surface;
            }

            if (requested != null && LevelGraph.AnyLinkFrom(requested))
            {
                foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(requested))
                {
                    if (CanReceiveDropPods(link.map))
                    {
                        return link.map;
                    }
                }
            }

            return requested;
        }

        public static bool CanReceiveDropPods(Map map)
        {
            if (map == null || map.Disposed)
            {
                return false;
            }

            // Underground levels are mountain-roofed. Drop pods spawn and
            // immediately get eaten by the roof.
            if (StrataMapUtility.IsUnderground(map))
            {
                return false;
            }

            return true;
        }

        public static bool TryLaunchFromLinkedFloors(ThingDef resDef, int countNeeded, Map map, TradeShip trader)
        {
            if (map == null || resDef == null || countNeeded <= 0 || !LevelGraph.AnyLinkFrom(map))
            {
                return false;
            }

            aggregating = true;
            try
            {
                while (countNeeded > 0)
                {
                    Thing found = null;
                    foreach (Thing thing in LaunchablesAcrossLinkedFloors(map, trader))
                    {
                        if (thing.def == resDef && thing.stackCount > 0)
                        {
                            found = thing;
                            break;
                        }
                    }

                    if (found == null)
                    {
                        Log.Error("[Strata] Did not find any " + resDef + " to launch for orbital trade.");
                        return true;
                    }

                    int take = countNeeded < found.stackCount ? countNeeded : found.stackCount;
                    countNeeded -= take;
                    Thing launched = found.SplitOff(take);
                    launched.Destroy();
                }
            }
            finally
            {
                aggregating = false;
            }

            return true;
        }

        public static void RedirectDrop(ref IntVec3 dropCenter, ref Map map)
        {
            if (redirecting || map == null)
            {
                return;
            }

            Map landing = LandingMap(map);
            if (landing == null || landing == map)
            {
                return;
            }

            redirecting = true;
            try
            {
                IntVec3 spot = DropCellFinder.TradeDropSpot(landing);
                if (!spot.IsValid)
                {
                    spot = DropCellFinder.RandomDropSpot(landing);
                }
                if (spot.IsValid)
                {
                    map = landing;
                    dropCenter = spot;
                }
            }
            finally
            {
                redirecting = false;
            }
        }
    }

    [HarmonyPatch(typeof(TradeUtility), nameof(TradeUtility.AllLaunchableThingsForTrade))]
    public static class Patch_TradeUtility_AllLaunchableThings_Linked
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, Map map, ITrader trader)
        {
            if (StrataOrbitalTrade.Aggregating || map == null || !LevelGraph.AnyLinkFrom(map))
            {
                if (__result == null)
                {
                    yield break;
                }
                foreach (Thing thing in __result)
                {
                    yield return thing;
                }
                yield break;
            }

            if (__result != null)
            {
                foreach (Thing thing in __result)
                {
                    yield return thing;
                }
            }

            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                Map other = link.map;
                if (other == null || other == map)
                {
                    continue;
                }

                foreach (Thing thing in StrataOrbitalTrade.LaunchablesOn(other, trader))
                {
                    yield return thing;
                }
            }
        }
    }

    [HarmonyPatch(typeof(TradeUtility), nameof(TradeUtility.LaunchThingsOfType))]
    public static class Patch_TradeUtility_LaunchThingsOfType_Linked
    {
        public static bool Prefix(ThingDef resDef, int countNeeded, Map map, TradeShip trader)
        {
            return !StrataOrbitalTrade.TryLaunchFromLinkedFloors(resDef, countNeeded, map, trader);
        }
    }

    [HarmonyPatch(typeof(TradeUtility), nameof(TradeUtility.SpawnDropPod))]
    public static class Patch_TradeUtility_SpawnDropPod_Linked
    {
        public static void Prefix(ref IntVec3 dropCenter, ref Map map)
        {
            StrataOrbitalTrade.RedirectDrop(ref dropCenter, ref map);
        }
    }
}

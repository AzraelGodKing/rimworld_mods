using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public static class SmokeRiseUtility
    {
        // Fraction of a stairwell room's buoyant gas that convects up each
        // cycle (unsealed shaft). Heavy gases sink down at the same rate.
        // Scaled by shaft aperture vs room size so opening a door into a
        // large hall does not flush the whole volume in one cycle (AZR-353).
        public const float NaturalShaftRise = 0.15f;
        public const float NaturalShaftSink = 0.15f;
        // One portal ≈ this many cells of effective stack aperture.
        private const float ShaftApertureCells = 8f;

        public static bool RoomContainsLevelExit(Room room, Map map)
        {
            if (room == null)
            {
                return false;
            }
            foreach (Region region in room.Regions)
            {
                foreach (IntVec3 cell in region.Cells)
                {
                    List<Thing> things = cell.GetThingList(map);
                    for (int i = 0; i < things.Count; i++)
                    {
                        if (things[i] is PocketMapExit)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static MapPortal GetUpperEntrance(PocketMapExit lowerExit)
        {
            return lowerExit?.entrance;
        }

        public static void ProcessMap(AtmosphereMapComponent atmosphere)
        {
            Map map = atmosphere.map;

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                // Dig landings only: buoyant gas rises toward the entrance above.
                // Tower landings are handled in ProcessTowerShafts (opposite stack).
                if (thing is not PocketMapExit lowerExit
                    || thing is Building_StairsDown
                    || thing is Building_BuildUpLanding)
                {
                    continue;
                }
                if (StrataPortalUtility.IsSealedPortal(lowerExit.entrance ?? lowerExit))
                {
                    continue;
                }
                MapPortal upperEntrance = GetUpperEntrance(lowerExit);
                if (upperEntrance == null || !upperEntrance.Spawned)
                {
                    continue;
                }
                Room lowerRoom = lowerExit.Position.GetRoom(map);
                if (lowerRoom == null || lowerRoom.UsesOutdoorTemperature)
                {
                    continue;
                }
                Room upperRoom = upperEntrance.Position.GetRoom(upperEntrance.Map);
                float rate = ScaledShaftRate(NaturalShaftRise, lowerRoom, map);
                if (RoomContainsLevelExit(lowerRoom, map))
                {
                    rate = Mathf.Clamp01(rate + atmosphere.ShaftRiseBoostForRoom(lowerRoom)
                        * ShaftApertureFactor(lowerRoom, map));
                }
                // Sample must be an UPPER-map cell (it anchors the receiving
                // cloud there); the entrance's own cell resolves to its room.
                atmosphere.TransferGasUp(lowerRoom, upperRoom, upperEntrance.Map, rate, upperEntrance.Position);
            }
        }

        // Heavy gases (CO₂, deep gas) sink through unsealed dig stairwells /
        // elevators into the level below — never through tower shafts (those open up).
        public static void ProcessGasSink(AtmosphereMapComponent atmosphere)
        {
            Map map = atmosphere.map;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is Building_StairsBuildUp
                    || thing is not Building_StairsDown stairsDown
                    || !stairsDown.Spawned || !stairsDown.PocketMapExists)
                {
                    continue;
                }
                if (StrataPortalUtility.IsSealedPortal(stairsDown))
                {
                    continue;
                }
                Map lowerMap = stairsDown.PocketMap;
                if (lowerMap == null || StrataMapUtility.IsUpperLevel(lowerMap))
                {
                    continue;
                }
                MapPortal lowerLanding = FindLinkedLanding(stairsDown, lowerMap);
                if (lowerLanding == null || !lowerLanding.Spawned)
                {
                    continue;
                }
                Room upperRoom = stairsDown.Position.GetRoom(map);
                if (upperRoom == null || upperRoom.UsesOutdoorTemperature)
                {
                    continue;
                }
                Room lowerRoom = lowerLanding.Position.GetRoom(lowerMap);
                float rate = ScaledShaftRate(NaturalShaftSink, upperRoom, map);
                rate = Mathf.Clamp01(rate + atmosphere.ShaftSinkBoostForRoom(upperRoom)
                    * ShaftApertureFactor(upperRoom, map));
                atmosphere.TransferGasDown(upperRoom, lowerRoom, lowerMap, rate, lowerLanding.Position);
            }
        }

        // Tower shafts: pocket is ABOVE this portal. Buoyant gas rises into A+;
        // heavy gas on the upper floor sinks back down to this floor.
        public static void ProcessTowerShafts(AtmosphereMapComponent atmosphere)
        {
            Map map = atmosphere.map;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not Building_StairsBuildUp tower || !tower.Spawned || !tower.PocketMapExists)
                {
                    continue;
                }
                if (StrataPortalUtility.IsSealedPortal(tower))
                {
                    continue;
                }
                Map upperMap = tower.PocketMap;
                if (upperMap == null || !StrataMapUtility.IsUpperLevel(upperMap))
                {
                    continue;
                }
                MapPortal landing = FindLinkedLanding(tower, upperMap);
                if (landing == null || !landing.Spawned)
                {
                    continue;
                }

                Room lowerRoom = tower.Position.GetRoom(map);
                Room upperRoom = landing.Position.GetRoom(upperMap);

                if (lowerRoom != null && !lowerRoom.UsesOutdoorTemperature)
                {
                    float rise = ScaledShaftRate(NaturalShaftRise, lowerRoom, map);
                    if (RoomContainsLevelExit(lowerRoom, map)
                        || (upperRoom != null && RoomContainsLevelExit(upperRoom, upperMap)))
                    {
                        rise = Mathf.Clamp01(rise + atmosphere.ShaftRiseBoostForRoom(lowerRoom)
                            * ShaftApertureFactor(lowerRoom, map));
                    }
                    atmosphere.TransferGasUp(lowerRoom, upperRoom, upperMap, rise, landing.Position);
                }

                if (upperRoom != null && !upperRoom.UsesOutdoorTemperature)
                {
                    AtmosphereMapComponent upperAtmo = upperMap.GetComponent<AtmosphereMapComponent>();
                    if (upperAtmo == null)
                    {
                        continue;
                    }
                    float sink = ScaledShaftRate(NaturalShaftSink, upperRoom, upperMap);
                    sink = Mathf.Clamp01(sink + upperAtmo.ShaftSinkBoostForRoom(upperRoom)
                        * ShaftApertureFactor(upperRoom, upperMap));
                    upperAtmo.TransferGasDown(upperRoom, lowerRoom, map, sink, tower.Position);
                }
            }
        }

        // Small shaft rooms keep full NaturalShaftRise; merging into a large
        // hall (open door) throttles mass flow by aperture / room size.
        internal static float ShaftApertureFactor(Room room, Map map)
        {
            if (room == null || map == null)
            {
                return 1f;
            }
            int portals = CountPortalsInRoom(room, map);
            float roomCells = Mathf.Max(room.CellCount, 1);
            return Mathf.Clamp(portals * ShaftApertureCells / roomCells, 0.02f, 1f);
        }

        internal static float ScaledShaftRate(float natural, Room room, Map map)
        {
            return Mathf.Clamp01(natural * ShaftApertureFactor(room, map));
        }

        private static int CountPortalsInRoom(Room room, Map map)
        {
            int count = 0;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing == null || !thing.Spawned || thing.Position.GetRoom(map) != room)
                {
                    continue;
                }
                if (thing is PocketMapExit || thing is Building_StairsDown || thing is Building_StairsBuildUp)
                {
                    count++;
                }
            }
            return Mathf.Max(count, 1);
        }

        private static MapPortal FindLinkedLanding(Building_StairsDown entrance, Map otherMap)
        {
            foreach (Thing thing in otherMap.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is PocketMapExit exit && exit.entrance == entrance)
                {
                    return exit;
                }
            }
            return null;
        }
    }
}

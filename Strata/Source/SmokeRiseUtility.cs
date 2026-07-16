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
        public const float NaturalShaftRise = 0.15f;
        public const float NaturalShaftSink = 0.15f;

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
                float rate = NaturalShaftRise;
                if (RoomContainsLevelExit(lowerRoom, map))
                {
                    rate = Mathf.Clamp01(rate + atmosphere.ShaftRiseBoostForRoom(lowerRoom));
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
                float rate = NaturalShaftSink;
                rate = Mathf.Clamp01(rate + atmosphere.ShaftSinkBoostForRoom(upperRoom));
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
                    float rise = NaturalShaftRise;
                    if (RoomContainsLevelExit(lowerRoom, map)
                        || (upperRoom != null && RoomContainsLevelExit(upperRoom, upperMap)))
                    {
                        rise = Mathf.Clamp01(rise + atmosphere.ShaftRiseBoostForRoom(lowerRoom));
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
                    float sink = NaturalShaftSink;
                    sink = Mathf.Clamp01(sink + upperAtmo.ShaftSinkBoostForRoom(upperRoom));
                    upperAtmo.TransferGasDown(upperRoom, lowerRoom, map, sink, tower.Position);
                }
            }
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

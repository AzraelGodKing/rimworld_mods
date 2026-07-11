using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public static class SmokeRiseUtility
    {
        // Fraction of stairwell-room smoke that convects up each cycle (unsealed shaft).
        public const float NaturalShaftRise = 0.15f;

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

        public static void ProcessMap(SmokeMapComponent smoke)
        {
            Map map = smoke.map;
            float bonusRate = 0f;
            foreach (CompSmokeUpdraft updraft in smoke.Updrafts)
            {
                if (updraft.parent.Spawned && updraft.Active)
                {
                    bonusRate += updraft.Props.risePower;
                }
            }

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not PocketMapExit lowerExit || thing is Building_StairsDown or Building_ElevatorDown)
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
                if (bonusRate > 0f && RoomContainsLevelExit(lowerRoom, map))
                {
                    rate = Mathf.Clamp01(rate + bonusRate);
                }
                smoke.TransferSmokeUp(lowerRoom, upperRoom, upperEntrance.Map, rate, lowerExit.Position);
            }
        }
    }
}

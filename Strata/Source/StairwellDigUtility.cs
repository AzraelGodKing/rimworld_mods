using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Carve a downward stairwell from an underground landing. Spawns a
    // power-less dig shaft beside the landing so only one transmitter sits
    // on the landing cells.
    public static class StairwellDigUtility
    {
        private const float NearbyShaftRadius = 8f;

        public static bool CanDigDownFromLanding(Building_StairsUp landing, out string reason)
        {
            reason = null;
            if (landing == null || !landing.Spawned)
            {
                reason = "Invalid stairwell.";
                return false;
            }
            if (!StrataMapUtility.IsUnderground(landing.Map))
            {
                reason = "Dig down is only available below the surface.";
                return false;
            }
            if (landing.Faction != Faction.OfPlayer)
            {
                reason = "Must belong to the colony.";
                return false;
            }
            if (!LevelExcavationUtility.CanOpenNewLevelBelow(landing.Map, out reason))
            {
                return false;
            }
            if (LandingHasDownwardShaft(landing))
            {
                reason = "This stairwell already reaches a level below.";
                return false;
            }
            if (FindNearbyDigShaftConstruction(landing, out _))
            {
                reason = "A dig shaft is still under construction.";
                return false;
            }
            Building_StairsDown existing = landing.DownEntrance ?? FindNearbyDigShaft(landing);
            if (existing != null)
            {
                return true;
            }
            if (!TryFindDigShaftSite(landing, out _))
            {
                reason = "No room nearby to carve a downward stairwell.";
                return false;
            }
            return true;
        }

        public static bool CanDigDownFromEntrance(Building_StairsDown entrance, out string reason)
        {
            reason = null;
            if (entrance == null || !entrance.Spawned)
            {
                reason = "Invalid stairwell.";
                return false;
            }
            if (!StrataMapUtility.IsUnderground(entrance.Map))
            {
                reason = "Dig down is only available below the surface.";
                return false;
            }
            if (entrance.Faction != Faction.OfPlayer)
            {
                reason = "Must belong to the colony.";
                return false;
            }
            if (entrance.PocketMapExists)
            {
                reason = "The level below is already open.";
                return false;
            }
            if (entrance.Sealed)
            {
                reason = "Unseal the stairwell first.";
                return false;
            }
            if (!LevelExcavationUtility.CanOpenNewLevelBelow(entrance.Map, out reason))
            {
                return false;
            }
            return true;
        }

        public static bool TryDigDownFromLanding(Building_StairsUp landing, out string message)
        {
            message = null;
            if (!CanDigDownFromLanding(landing, out message))
            {
                return false;
            }
            Building_StairsDown down = landing.DownEntrance ?? FindNearbyDigShaft(landing);
            if (down == null)
            {
                if (!TryFindDigShaftSite(landing, out IntVec3 center))
                {
                    message = "No room nearby to carve a downward stairwell.";
                    return false;
                }
                if (!TryPlaceDigShaftBlueprint(center, landing.Map, out message))
                {
                    return false;
                }
                Messages.Message(message, landing, MessageTypeDefOf.NeutralEvent);
                return true;
            }
            if (!down.PocketMapExists)
            {
                if (!OpenLevelBelow(down, landing, out message))
                {
                    return false;
                }
                StairwellPowerUtility.MaintainVerticalTie(landing);
                return true;
            }
            message = "The level below is already open.";
            return false;
        }

        public static bool TryDigDownFromEntrance(Building_StairsDown entrance, out string message)
        {
            message = null;
            if (!CanDigDownFromEntrance(entrance, out message))
            {
                return false;
            }
            return OpenLevelBelow(entrance, null, out message);
        }

        private static bool OpenLevelBelow(Building_StairsDown entrance, Building_StairsUp landing, out string message)
        {
            entrance.OpenLevelBelow();
            if (!entrance.PocketMapExists)
            {
                message = "Failed to open the level below.";
                return false;
            }
            message = "Broke through to a new level below.";
            Messages.Message(message, landing ?? (Thing)entrance, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        private static bool TryPlaceDigShaftBlueprint(IntVec3 center, Map map, out string message)
        {
            message = null;
            ThingDef def = StrataThingDefOf.Strata_DigDownShaft ?? StrataThingDefOf.Strata_StairsDown;
            if (def == null)
            {
                message = "Could not place a dig shaft.";
                return false;
            }
            PrepareSite(center, def, map);
            AcceptanceReport report = GenConstruct.CanPlaceBlueprintAt(def, center, Rot4.North, map);
            if (!report.Accepted)
            {
                message = report.Reason.CapitalizeFirst();
                return false;
            }
            GenConstruct.PlaceBlueprintForBuild(def, center, map, Rot4.North, Faction.OfPlayer, null, null, null, true);
            message = "Designated a dig shaft — colonists must finish carving it before the level below opens.";
            return true;
        }

        private static void PrepareSite(IntVec3 center, ThingDef def, Map map)
        {
            foreach (IntVec3 cell in GenAdj.OccupiedRect(center, Rot4.North, def.size))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
            }
        }

        public static bool LandingHasDownwardShaft(Building_StairsUp landing)
        {
            if (landing?.DownEntrance != null && landing.DownEntrance.Spawned && landing.DownEntrance.PocketMapExists)
            {
                return true;
            }
            if (FindNearbyOpenDownPortal(landing) != null)
            {
                return true;
            }
            return FindNearbyDigShaftConstruction(landing, out _);
        }

        public static Building_StairsDown FindNearbyDigShaft(Building_StairsUp landing)
        {
            if (landing == null)
            {
                return null;
            }
            Map map = landing.Map;
            float radiusSq = NearbyShaftRadius * NearbyShaftRadius;
            Building_StairsDown best = null;
            float bestDist = float.MaxValue;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not Building_StairsDown down || !down.Spawned || down.def != StrataThingDefOf.Strata_DigDownShaft)
                {
                    continue;
                }
                float dist = down.Position.DistanceToSquared(landing.Position);
                if (dist <= radiusSq && dist < bestDist)
                {
                    best = down;
                    bestDist = dist;
                }
            }
            return best;
        }

        private static bool FindNearbyDigShaftConstruction(Building_StairsUp landing, out IntVec3 center)
        {
            center = IntVec3.Invalid;
            if (landing == null)
            {
                return false;
            }
            Map map = landing.Map;
            ThingDef digDef = StrataThingDefOf.Strata_DigDownShaft;
            if (digDef == null)
            {
                return false;
            }
            float radiusSq = NearbyShaftRadius * NearbyShaftRadius;
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing.def != digDef)
                {
                    continue;
                }
                if (thing is not Blueprint_Build && thing is not Frame)
                {
                    continue;
                }
                float dist = thing.Position.DistanceToSquared(landing.Position);
                if (dist <= radiusSq)
                {
                    center = thing.Position;
                    return true;
                }
            }
            return false;
        }

        public static void ResolveDownEntrance(Building_StairsUp landing)
        {
            if (landing == null || landing.DownEntrance is { Spawned: true })
            {
                return;
            }
            landing.SetDownEntrance(FindNearbyDigShaft(landing));
        }

        private static Building_StairsDown FindNearbyOpenDownPortal(Building_StairsUp landing)
        {
            Map map = landing?.Map;
            if (map == null)
            {
                return null;
            }
            float radiusSq = NearbyShaftRadius * NearbyShaftRadius;
            Building_StairsDown best = null;
            float bestDist = float.MaxValue;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not Building_StairsDown down || !down.Spawned || !down.PocketMapExists)
                {
                    continue;
                }
                float dist = down.Position.DistanceToSquared(landing.Position);
                if (dist <= radiusSq && dist < bestDist)
                {
                    best = down;
                    bestDist = dist;
                }
            }
            return best;
        }

        internal static bool TryFindDigShaftSite(Building_StairsUp landing, out IntVec3 center)
        {
            center = IntVec3.Invalid;
            Map map = landing.Map;
            ThingDef downDef = StrataThingDefOf.Strata_DigDownShaft ?? StrataThingDefOf.Strata_StairsDown;
            if (downDef == null)
            {
                return false;
            }
            List<IntVec3> candidates = GenRadial.RadialCellsAround(landing.Position, NearbyShaftRadius, useCenter: false)
                .Where(c => c.InBounds(map))
                .OrderBy(c => c.DistanceToSquared(landing.Position))
                .ToList();
            foreach (IntVec3 candidate in candidates)
            {
                if (SiteFitsDigShaft(candidate, downDef, map, landing))
                {
                    center = candidate;
                    return true;
                }
            }
            return false;
        }

        private static bool SiteFitsDigShaft(IntVec3 center, ThingDef downDef, Map map, Building_StairsUp landing)
        {
            CellRect landingRect = landing.OccupiedRect();
            CellRect downRect = GenAdj.OccupiedRect(center, Rot4.North, downDef.size);
            if (SharesAnyCell(landingRect, downRect))
            {
                return false;
            }
            foreach (IntVec3 cell in downRect)
            {
                if (!cell.InBounds(map) || cell.DistanceToEdge(map) < 8)
                {
                    return false;
                }
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == landing)
                    {
                        return false;
                    }
                    if (thing is MapPortal)
                    {
                        return false;
                    }
                    if (thing.def.IsEdifice() && thing.def.building?.isNaturalRock != true)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool SharesAnyCell(CellRect a, CellRect b)
        {
            foreach (IntVec3 cell in a)
            {
                if (b.Contains(cell))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

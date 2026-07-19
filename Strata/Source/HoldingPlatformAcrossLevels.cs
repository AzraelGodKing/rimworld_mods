using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    // Vanilla CaptureEntity / HoldingPlatformAvailableOnCurrentMap only scan the
    // current map. CompHoldingPlatformTarget also clears targetHolder when the
    // holder is on another map — so cross-level crypts need stair commute +
    // assign-on-arrival (see PortalRelayChain.FinishContainment).
    public static class HoldingPlatformAcrossLevels
    {
        public static bool TryFind(
            Pawn carrier,
            Thing entity,
            out Building_HoldingPlatform platform,
            out MapPortal portal,
            bool ignoreReservations = false)
        {
            platform = null;
            portal = null;
            if (!ModsConfig.AnomalyActive
                || carrier?.Map == null
                || entity == null
                || !LevelGraph.AnyLinkFrom(carrier.Map))
            {
                return false;
            }

            float minStrength = entity.GetStatValue(StatDefOf.MinimumContainmentStrength);
            Building_HoldingPlatform best = null;
            Map bestMap = null;
            float bestScore = 0f;

            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(carrier.Map))
            {
                if (!link.arrivalCell.IsValid || !link.arrivalCell.InBounds(link.map))
                {
                    continue;
                }

                List<Building> holders = link.map.listerBuildings.AllBuildingsColonistOfGroup(
                    ThingRequestGroup.EntityHolder);
                for (int i = 0; i < holders.Count; i++)
                {
                    if (holders[i] is not Building_HoldingPlatform candidate)
                    {
                        continue;
                    }

                    if (!IsUsable(candidate, entity, link.arrivalCell, minStrength, ignoreReservations))
                    {
                        continue;
                    }

                    CompEntityHolder holder = candidate.TryGetComp<CompEntityHolder>();
                    float dist = Mathf.Max(link.arrivalCell.DistanceTo(candidate.Position), 1f);
                    float score = holder.ContainmentStrength / dist;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                        bestMap = link.map;
                    }
                }
            }

            if (best == null || bestMap == null)
            {
                return false;
            }

            MapPortal step = LevelGraph.BestFirstStep(
                carrier.Map,
                bestMap,
                carrier.Position,
                carrier);
            if (step == null || !step.Spawned || !step.IsEnterable(out _)
                || !carrier.CanReach(step, PathEndMode.Touch, Danger.Deadly))
            {
                return false;
            }

            platform = best;
            portal = step;
            return true;
        }

        public static bool HasAny(Pawn carrier, Thing entity)
        {
            return TryFind(carrier, entity, out _, out _, ignoreReservations: true)
                || TryFind(carrier, entity, out _, out _, ignoreReservations: false);
        }

        public static bool AnyAvailableOnLinkedMaps(Map fromMap)
        {
            if (!ModsConfig.AnomalyActive || fromMap == null || !LevelGraph.AnyLinkFrom(fromMap))
            {
                return false;
            }

            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(fromMap))
            {
                foreach (Building building in link.map.listerBuildings.allBuildingsColonist)
                {
                    if (building.TryGetComp(out CompEntityHolder comp)
                        && comp.Available
                        && !StudyUtility.AlreadyReserved(building, out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsUsable(
            Building_HoldingPlatform platform,
            Thing entity,
            IntVec3 arrivalCell,
            float minStrength,
            bool ignoreReservations)
        {
            if (platform == null || !platform.Spawned || platform.Occupied || platform.IsBurning())
            {
                return false;
            }

            CompEntityHolder holder = platform.TryGetComp<CompEntityHolder>();
            if (holder == null || !holder.Available || holder.ContainmentStrength < minStrength)
            {
                return false;
            }

            if (!ignoreReservations && StudyUtility.AlreadyReserved(platform, out _))
            {
                return false;
            }

            if (!platform.Map.reachability.CanReach(
                    arrivalCell,
                    platform,
                    PathEndMode.Touch,
                    TraverseParms.For(TraverseMode.PassDoors)))
            {
                return false;
            }

            return true;
        }
    }
}

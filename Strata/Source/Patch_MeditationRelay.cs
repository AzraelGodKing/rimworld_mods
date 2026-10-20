using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Vanilla meditation only lists spots on the current map. An assigned
    // spot or a free colonist spot on a linked floor still counts.
    [HarmonyPatch(typeof(JobGiver_Meditate), "TryGiveJob")]
    public static class Patch_MeditationRelay
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (!ModsConfig.RoyaltyActive || pawn?.Map == null)
            {
                return;
            }
            if (!MeditationUtility.CanMeditateNow(pawn))
            {
                return;
            }

            Building assigned = pawn.ownership?.AssignedMeditationSpot;
            if (assigned != null && assigned.Spawned && assigned.Map == pawn.Map)
            {
                return;
            }

            if (assigned != null && assigned.Spawned && assigned.Map != pawn.Map)
            {
                Job hop = RelayToSpot(pawn, assigned);
                if (hop != null)
                {
                    __result = hop;
                }
                return;
            }

            if (__result != null)
            {
                return;
            }

            Building spot = FindSpotOnLinkedFloor(pawn);
            if (spot == null)
            {
                return;
            }

            Job job = RelayToSpot(pawn, spot);
            if (job != null)
            {
                __result = job;
            }
        }

        private static Job RelayToSpot(Pawn pawn, Building spot)
        {
            if (spot?.Map == null || pawn.Map == null || spot.Map == pawn.Map)
            {
                return null;
            }
            if (!ColonyBedUtility.MapsLinked(pawn.Map, spot.Map))
            {
                return null;
            }
            if (!PawnRelay.CanLeaveWorkForNeed(pawn))
            {
                return null;
            }

            return PawnRelay.TryRelayToMap(
                pawn,
                spot.Map,
                touchCooldown: false,
                RelayPurpose.ForcedOrder,
                preferArrivalNear: spot.Position);
        }

        private static Building FindSpotOnLinkedFloor(Pawn pawn)
        {
            ThingDef def = ThingDefOf.MeditationSpot;
            if (def == null)
            {
                return null;
            }

            Building best = null;
            int bestDepth = int.MaxValue;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                Map map = link.map;
                if (map == null)
                {
                    continue;
                }

                foreach (Building building in map.listerBuildings.AllBuildingsColonistOfDef(def))
                {
                    if (building == null || !building.Spawned)
                    {
                        continue;
                    }
                    Pawn owner = building.GetAssignedPawn();
                    if (owner != null && owner != pawn)
                    {
                        continue;
                    }
                    if (link.depth < bestDepth || (owner == pawn && best != null))
                    {
                        bestDepth = link.depth;
                        best = building;
                        if (owner == pawn)
                        {
                            return best;
                        }
                    }
                }
            }

            return best;
        }
    }
}

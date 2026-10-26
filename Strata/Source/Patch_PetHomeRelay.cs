using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Colony pets do not take work relay. They still walk stairs to an
    // assigned bed or an allowed area on another linked floor. Animal think
    // trees wander; they do not run SeekAllowedArea, so wander is the daily path.
    public static class PetHomeRelay
    {
        public static void Apply(Pawn pawn, ref Job result)
        {
            if (result != null || pawn?.Map == null || !StrataPawnUtility.IsColonyPet(pawn))
            {
                return;
            }
            if (!LevelGraph.AnyLinkFrom(pawn.Map) || PawnRelay.IsOnRelayCooldown(pawn))
            {
                return;
            }

            Map dest = DestForPet(pawn);
            if (dest == null || dest == pawn.Map)
            {
                return;
            }

            Building_Bed bed = pawn.ownership?.OwnedBed;
            if (bed != null && bed.Spawned && bed.Map == dest)
            {
                Job rest = PawnRelay.TryRelayToMap(
                    pawn, dest, touchCooldown: true, RelayPurpose.Rest, bed);
                if (rest != null)
                {
                    result = rest;
                }
                return;
            }

            Job hop = PawnRelay.TryRelayToMap(
                pawn, dest, touchCooldown: true, RelayPurpose.ForcedOrder);
            if (hop != null)
            {
                result = hop;
            }
        }

        private static Map DestForPet(Pawn pawn)
        {
            Building_Bed bed = pawn.ownership?.OwnedBed;
            if (bed != null && bed.Spawned && bed.Map != pawn.Map
                && ColonyBedUtility.MapsLinked(pawn.Map, bed.Map))
            {
                return bed.Map;
            }

            if (!pawn.Position.IsForbidden(pawn))
            {
                return null;
            }

            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(pawn.Map);
            for (int i = 0; i < links.Count; i++)
            {
                Map map = links[i].map;
                if (map != null && HasAllowedZone(pawn, map))
                {
                    return map;
                }
            }

            return null;
        }

        private static readonly AccessTools.FieldRef<Pawn_PlayerSettings, Dictionary<Map, Area>> allowedAreas =
            AccessTools.FieldRefAccess<Pawn_PlayerSettings, Dictionary<Map, Area>>("allowedAreas");

        private static bool HasAllowedZone(Pawn pawn, Map map)
        {
            if (pawn.playerSettings == null || map == null)
            {
                return false;
            }

            Dictionary<Map, Area> areas = allowedAreas(pawn.playerSettings);
            if (areas == null || !areas.TryGetValue(map, out Area area) || area == null)
            {
                return false;
            }

            return area.TrueCount > 0;
        }
    }

    [HarmonyPatch(typeof(JobGiver_SeekAllowedArea), "TryGiveJob")]
    public static class Patch_PetSeekAllowedArea
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            PetHomeRelay.Apply(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_Wander), "TryGiveJob")]
    public static class Patch_PetWanderHome
    {
        public static void Postfix(JobGiver_Wander __instance, Pawn pawn, ref Job __result)
        {
            if (__instance is JobGiver_WanderColony || __instance is JobGiver_WanderHerd)
            {
                PetHomeRelay.Apply(pawn, ref __result);
            }
        }
    }
}

using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Colony pets do not take work relay. They still walk stairs to an
    // assigned bed or an allowed area on another linked floor.
    [HarmonyPatch(typeof(JobGiver_SeekAllowedArea), "TryGiveJob")]
    public static class Patch_PetSeekAllowedArea
    {
        private static readonly AccessTools.FieldRef<Pawn_PlayerSettings, Dictionary<Map, Area>> allowedAreas =
            AccessTools.FieldRefAccess<Pawn_PlayerSettings, Dictionary<Map, Area>>("allowedAreas");

        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null || pawn?.Map == null || !StrataPawnUtility.IsColonyPet(pawn))
            {
                return;
            }
            if (!pawn.Position.IsForbidden(pawn) || !LevelGraph.AnyLinkFrom(pawn.Map))
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
                    pawn, dest, touchCooldown: false, RelayPurpose.Rest, bed);
                if (rest != null)
                {
                    __result = rest;
                }
                return;
            }

            Job hop = PawnRelay.TryRelayToMap(
                pawn, dest, touchCooldown: false, RelayPurpose.ForcedOrder);
            if (hop != null)
            {
                __result = hop;
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
}

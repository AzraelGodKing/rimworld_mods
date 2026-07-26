using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Strata
{
    // Soft-compat for Hospitality guests, vanilla lodgers, and traders stuck on
    // a linked floor: let them walk shafts toward beds / the surface without
    // opening colony work/food relays (CanUseLevelPortals stays player-only).
    public static class VisitorPortalUtility
    {
        public static bool IsVisitor(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed || pawn.IsPrisoner)
            {
                return false;
            }
            if (pawn.IsColonist || pawn.IsSlave || pawn.IsColonyMech)
            {
                return false;
            }
            // Traders, temporary allies, Hospitality guests, quest lodgers.
            if (pawn.Faction == null || pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }
            return pawn.RaceProps.Humanlike;
        }

        public static bool HasGuestBedFor(Pawn pawn, Map map)
        {
            if (pawn == null || map == null)
            {
                return false;
            }
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is not Building_Bed bed
                    || !bed.def.building.bed_humanlike
                    || bed.ForPrisoners
                    || bed.Medical
                    || !bed.AnyUnoccupiedSleepingSlot
                    || bed.IsForbidden(pawn)
                    || bed.IsBurning())
                {
                    continue;
                }
                // Guest/lodger beds are usually non-colonist assignable; also
                // accept unowned colonist beds Hospitality may use.
                if (!bed.ForColonists || bed.OwnersForReading.Count == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    public static class Patch_VisitorRestAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null || !VisitorPortalUtility.IsVisitor(pawn))
            {
                return;
            }
            if (pawn.needs?.rest == null || pawn.needs.rest.CurLevel > 0.3f)
            {
                return;
            }
            if (VisitorPortalUtility.HasGuestBedFor(pawn, pawn.Map))
            {
                return;
            }
            if (!LevelGraph.AnyLinkFrom(pawn.Map) || PortalRelayChain.HasIntent(pawn))
            {
                return;
            }

            // Prefer Hospital role, then any linked floor with a free guest bed,
            // then climb toward the surface.
            var links = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(pawn.Map));
            LevelRoleUtility.SortLinksByRole(links, LevelRole.Barracks);
            for (int i = 0; i < links.Count; i++)
            {
                LevelGraph.LevelLink link = links[i];
                if (!VisitorPortalUtility.HasGuestBedFor(pawn, link.map))
                {
                    continue;
                }
                Job hop = PawnRelay.TryRelayToMap(
                    pawn, link.map, touchCooldown: false, RelayPurpose.Rest);
                if (hop != null)
                {
                    __result = hop;
                    return;
                }
            }

            Job escape = EscapePortalUtility.MakeEscapeJob(pawn);
            if (escape != null)
            {
                __result = escape;
            }
        }
    }

    // Traders / guests whose lord is on another linked map walk shafts toward it
    // when idle (e.g. Hospitality guest following host downstairs, then back).
    [HarmonyPatch(typeof(JobGiver_IdleError), "TryGiveJob")]
    public static class Patch_VisitorFollowLordAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null || !VisitorPortalUtility.IsVisitor(pawn))
            {
                return;
            }
            Lord lord = pawn.GetLord();
            Map lordMap = lord?.Map;
            if (lordMap == null || lordMap == pawn.Map || !ColonyBedUtility.MapsLinked(pawn.Map, lordMap))
            {
                return;
            }
            if (PortalRelayChain.HasIntent(pawn))
            {
                return;
            }

            Job hop = PawnRelay.TryRelayToMap(
                pawn, lordMap, touchCooldown: false, RelayPurpose.Work);
            if (hop != null)
            {
                __result = hop;
            }
        }
    }
}

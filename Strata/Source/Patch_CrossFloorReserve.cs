using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Vanilla reserve checks demand the pawn already be on the thing's map.
    // A linked floor is the same colony: claim the stack, bed, or bench there,
    // then walk the stairs and finish the job.
    [HarmonyPatch(typeof(ReservationManager), nameof(ReservationManager.CanReserve))]
    public static class Patch_ReservationManager_CanReserve_Linked
    {
        public static bool Prefix(
            ReservationManager __instance,
            Pawn claimant,
            LocalTargetInfo target,
            int maxPawns,
            int stackCount,
            ReservationLayerDef layer,
            bool ignoreOtherReservations,
            ref bool __result)
        {
            if (!CrossFloorReserve.IsOffMapClaim(claimant, __instance, target))
            {
                return true;
            }

            __result = CrossFloorReserve.CanReserve(
                __instance, claimant, target, maxPawns, stackCount, layer, ignoreOtherReservations);
            return false;
        }
    }

    [HarmonyPatch(typeof(ReservationUtility), nameof(ReservationUtility.CanReserve))]
    public static class Patch_ReservationUtility_CanReserve_Linked
    {
        public static bool Prefix(
            Pawn p,
            LocalTargetInfo target,
            int maxPawns,
            int stackCount,
            ReservationLayerDef layer,
            bool ignoreOtherReservations,
            ref bool __result)
        {
            Map dest = CrossFloorReserve.ThingMap(p, target);
            if (dest == null)
            {
                return true;
            }

            __result = CrossFloorReserve.CanReserve(
                dest.reservationManager, p, target, maxPawns, stackCount, layer, ignoreOtherReservations);
            return false;
        }
    }

    [HarmonyPatch(typeof(ReservationUtility), nameof(ReservationUtility.Reserve))]
    public static class Patch_ReservationUtility_Reserve_Linked
    {
        public static bool Prefix(
            Pawn p,
            LocalTargetInfo target,
            Job job,
            int maxPawns,
            int stackCount,
            ReservationLayerDef layer,
            bool errorOnFailed,
            bool ignoreOtherReservations,
            ref bool __result)
        {
            Map dest = CrossFloorReserve.ThingMap(p, target);
            if (dest == null)
            {
                return true;
            }

            __result = dest.reservationManager.Reserve(
                p, job, target, maxPawns, stackCount, layer, errorOnFailed, ignoreOtherReservations);
            return false;
        }
    }

    [HarmonyPatch(typeof(ReservationUtility), nameof(ReservationUtility.HasReserved), typeof(Pawn), typeof(LocalTargetInfo), typeof(Job))]
    public static class Patch_ReservationUtility_HasReserved_Linked
    {
        public static bool Prefix(Pawn p, LocalTargetInfo target, Job job, ref bool __result)
        {
            Map dest = CrossFloorReserve.ThingMap(p, target);
            if (dest == null)
            {
                return true;
            }

            __result = dest.reservationManager.ReservedBy(target, p, job);
            return false;
        }
    }

    public static class CrossFloorReserve
    {
        private static readonly AccessTools.FieldRef<ReservationManager, Map> mapOf =
            AccessTools.FieldRefAccess<ReservationManager, Map>("map");

        private static readonly AccessTools.FieldRef<ReservationManager, List<ReservationManager.Reservation>> reservationList =
            AccessTools.FieldRefAccess<ReservationManager, List<ReservationManager.Reservation>>("reservations");

        public static Map ThingMap(Pawn pawn, LocalTargetInfo target)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return null;
            }

            Thing thing = target.Thing;
            Map dest = thing?.MapHeld;
            if (dest == null || dest == pawn.Map || !ColonyBedUtility.MapsLinked(pawn.Map, dest))
            {
                return null;
            }

            return dest;
        }

        public static bool IsOffMapClaim(Pawn claimant, ReservationManager manager, LocalTargetInfo target)
        {
            if (claimant == null || !claimant.Spawned || manager == null)
            {
                return false;
            }

            Map map = mapOf(manager);
            Thing thing = target.Thing;
            if (map == null || claimant.Map == map || thing?.MapHeld != map)
            {
                return false;
            }

            return ColonyBedUtility.MapsLinked(claimant.Map, map);
        }

        // Same rules as ReservationManager.CanReserve, without "pawn.Map must be this map".
        public static bool CanReserve(
            ReservationManager manager,
            Pawn claimant,
            LocalTargetInfo target,
            int maxPawns,
            int stackCount,
            ReservationLayerDef layer,
            bool ignoreOtherReservations)
        {
            if (claimant == null || !claimant.Spawned || manager == null)
            {
                return false;
            }

            Map map = mapOf(manager);
            if (map == null || !target.IsValid || target.ThingDestroyed)
            {
                return false;
            }

            if (target.HasThing && target.Thing.SpawnedOrAnyParentSpawned && target.Thing.MapHeld != map)
            {
                return false;
            }

            int have = target.HasThing ? target.Thing.stackCount : 1;
            int want = stackCount == -1 ? have : stackCount;
            if (want > have)
            {
                return false;
            }

            if (ignoreOtherReservations)
            {
                return true;
            }

            if (map.physicalInteractionReservationManager.IsReserved(target)
                && !map.physicalInteractionReservationManager.IsReservedBy(claimant, target))
            {
                return false;
            }

            List<ReservationManager.Reservation> reservations = reservationList(manager);
            if (AlreadyReserved(reservations, claimant, target, layer, want))
            {
                return true;
            }

            var holders = new List<Pawn>();
            int pawns = 0;
            int stacks = 0;
            for (int i = 0; i < reservations.Count; i++)
            {
                ReservationManager.Reservation reservation = reservations[i];
                if (reservation.Target != target
                    || reservation.Layer != layer
                    || reservation.Claimant == claimant
                    || holders.Contains(reservation.Claimant)
                    || !Respects(claimant, reservation.Claimant))
                {
                    continue;
                }

                if (reservation.MaxPawns != maxPawns)
                {
                    return false;
                }

                holders.Add(reservation.Claimant);
                pawns++;
                stacks += reservation.StackCount == -1 ? have : reservation.StackCount;
                if (pawns >= maxPawns || want + stacks > have)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AlreadyReserved(
            List<ReservationManager.Reservation> reservations,
            Pawn claimant,
            LocalTargetInfo target,
            ReservationLayerDef layer,
            int want)
        {
            for (int i = 0; i < reservations.Count; i++)
            {
                ReservationManager.Reservation reservation = reservations[i];
                if (reservation.Target == target
                    && reservation.Layer == layer
                    && reservation.Claimant == claimant
                    && (reservation.StackCount == -1 || reservation.StackCount >= want))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Respects(Pawn claimant, Pawn other)
        {
            if (claimant == other)
            {
                return true;
            }

            if (claimant.Faction == null || other.Faction == null)
            {
                return false;
            }

            if (claimant.Faction == other.Faction || !claimant.Faction.HostileTo(other.Faction))
            {
                return true;
            }

            return other.HostFaction != null && other.HostFaction == claimant.HostFaction;
        }
    }
}

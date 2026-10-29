using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Vanilla JobDriver_EnterPortal drops carried things when the walker is not
    // drafted, then clears the job queue. That dumps infants/prisoners on the
    // landing and aborts Strata's Warden/Childcare relay finish. Keep pawns in
    // arms for those intents. For Haul / ForcedOrder, ONLY block drops while
    // the current job is EnterPortal — blocking for the whole Haul intent left
    // colonists/mechs stuck holding steel forever (Wait spam, can't force-drop)
    // when post-stair delivery failed to find a cell/frame.
    [HarmonyPatch]
    public static class Patch_EnterPortalKeepCarriedPawn
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(Pawn_CarryTracker),
                nameof(Pawn_CarryTracker.TryDropCarriedThing),
                new[]
                {
                    typeof(IntVec3),
                    typeof(ThingPlaceMode),
                    typeof(Thing).MakeByRefType(),
                    typeof(Action<Thing, int>),
                });
        }

        public static bool Prefix(Pawn_CarryTracker __instance, ref bool __result)
        {
            Pawn pawn = __instance.pawn;
            Thing carried = __instance.CarriedThing;
            if (pawn == null || carried == null)
            {
                return true;
            }

            if (carried is Pawn
                && (PortalRelayChain.HasIntent(pawn, RelayPurpose.Warden)
                    || PortalRelayChain.HasIntent(pawn, RelayPurpose.Childcare)
                    || PortalRelayChain.HasIntent(pawn, RelayPurpose.Containment)
                    || PortalRelayChain.HasIntent(pawn, RelayPurpose.Rescue)))
            {
                __result = false;
                return false;
            }

            if (pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                if (PortalRelayChain.HasIntent(pawn, RelayPurpose.Haul)
                    || PortalRelayChain.HasIntent(pawn, RelayPurpose.ForcedOrder))
                {
                    __result = false;
                    return false;
                }

                // Corpses / always-haulable cargo must not vanish at the shaft
                // when haul intent was cleared mid-hop (cross-level slaughter).
                if (carried is Corpse || carried.def.alwaysHaulable)
                {
                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }

    // 1.7 JobDriver_EnterPortal sets UnloadEverything when the dest is not a
    // pocket map. Stairs down onto the surface therefore dump inventory (spare
    // weapons, loot, packed gear) every trip. Colony floors are one base —
    // do not unpack at the landing.
    [HarmonyPatch(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.UnloadEverything), MethodType.Setter)]
    public static class Patch_EnterPortalKeepInventory
    {
        public static bool Prefix(Pawn_InventoryTracker __instance, bool value)
        {
            if (!value)
            {
                return true;
            }

            Pawn pawn = __instance.pawn;
            if (pawn?.CurJobDef != JobDefOf.EnterPortal)
            {
                return true;
            }

            Map map = pawn.Map;
            if (map == null || !ColonyBedUtility.MapInStrataColony(map))
            {
                return true;
            }

            return false;
        }
    }
}

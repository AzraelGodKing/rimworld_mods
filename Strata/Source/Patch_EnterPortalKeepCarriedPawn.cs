using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Vanilla JobDriver_EnterPortal drops carried things when the walker is not
    // drafted, then clears the job queue. That dumps infants/prisoners on the
    // landing and aborts Strata's Warden/Childcare relay finish. Keep the pawn
    // in arms while those intents are active. Haul / force-build also keep
    // materials (and PUAH single-carry stacks) so cargo survives the stair.
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

            if (PortalRelayChain.HasIntent(pawn, RelayPurpose.Haul)
                || PortalRelayChain.HasIntent(pawn, RelayPurpose.ForcedOrder))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}

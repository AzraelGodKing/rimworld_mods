using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Vanilla JobDriver_EnterPortal drops carried things when the walker is not
    // drafted, then clears the job queue. That dumps infants/prisoners on the
    // landing and aborts Strata's Warden/Childcare relay finish. Keep the pawn
    // in arms while those intents are active.
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
            if (pawn == null || __instance.CarriedThing is not Pawn)
            {
                return true;
            }

            if (PortalRelayChain.HasIntent(pawn, RelayPurpose.Warden)
                || PortalRelayChain.HasIntent(pawn, RelayPurpose.Childcare))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}

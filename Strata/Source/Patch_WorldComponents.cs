using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    [HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
    public static class Patch_World_FinalizeInit
    {
        public static void Postfix(World __instance)
        {
            StrataVgeCompat.ResetCaches();
            StrataGravshipHeat.Clear();
            if (__instance.GetComponent<StrataLevelLabels>() == null)
            {
                __instance.components.Add(new StrataLevelLabels(__instance));
            }
            if (__instance.GetComponent<StrataRitualTravel>() == null)
            {
                __instance.components.Add(new StrataRitualTravel(__instance));
            }
            if (__instance.GetComponent<StrataCaravanTravel>() == null)
            {
                __instance.components.Add(new StrataCaravanTravel(__instance));
            }
            if (__instance.GetComponent<WorldComponent_StrataLevelRoles>() == null)
            {
                __instance.components.Add(new WorldComponent_StrataLevelRoles(__instance));
            }
            if (__instance.GetComponent<WorldComponent_StrataGravshipStacks>() == null)
            {
                __instance.components.Add(new WorldComponent_StrataGravshipStacks(__instance));
            }
        }
    }

    [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    public static class Patch_GameTick_CaravanHaul
    {
        public static void Postfix()
        {
            // Fast path when nothing is mid-portal / haul / draft-route.
            bool haulPending = StrataPortalUtility.HasPendingHaulDeliveries;
            bool relayPending = PortalRelayChain.HasPendingContinues;
            bool draftPending = DraftedPortalPathing.HasPendingContinues;
            if (!haulPending && !relayPending && !draftPending)
            {
                StrataRobotDiagnostics.Tick();
                if (Find.TickManager.TicksGame % 250 == 0)
                {
                    StrataCaravanUtility.TickCaravanPull();
                }
                return;
            }

            StrataRobotDiagnostics.Tick();
            DraftedPortalPathing.Tick();
            // Before PortalRelayChain: haul-intent arrivals skip here and finish
            // in FinishHaul; construction-only arrivals deliver immediately.
            StrataPortalUtility.TickHaulDeliveries();
            PortalRelayChain.Tick();
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                StrataCaravanUtility.TickCaravanPull();
            }
        }
    }
}

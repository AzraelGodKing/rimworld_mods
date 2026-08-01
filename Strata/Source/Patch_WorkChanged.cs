using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Wake idle work-relay scans when designations or buildables appear/leave.
    [HarmonyPatch(typeof(DesignationManager), nameof(DesignationManager.AddDesignation))]
    public static class Patch_DesignationManager_AddDesignation_WorkRelay
    {
        public static void Postfix(DesignationManager __instance)
        {
            WorkRelaySignals.Notify_WorkChanged(__instance.map);
        }
    }

    [HarmonyPatch(typeof(DesignationManager), nameof(DesignationManager.RemoveDesignation))]
    public static class Patch_DesignationManager_RemoveDesignation_WorkRelay
    {
        public static void Postfix(DesignationManager __instance)
        {
            WorkRelaySignals.Notify_WorkChanged(__instance.map);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
    public static class Patch_Thing_SpawnSetup_WorkRelay
    {
        public static void Postfix(Thing __instance, Map map)
        {
            if (map == null || __instance == null)
            {
                return;
            }
            if (__instance is Blueprint || __instance is Frame)
            {
                WorkRelaySignals.Notify_WorkChanged(map);
            }
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
    public static class Patch_Thing_DeSpawn_WorkRelay
    {
        public static void Prefix(Thing __instance)
        {
            if (__instance?.Map == null)
            {
                return;
            }
            if (__instance is Blueprint || __instance is Frame)
            {
                WorkRelaySignals.Notify_WorkChanged(__instance.Map);
            }
        }
    }
}

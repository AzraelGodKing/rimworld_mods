using HarmonyLib;
using RimWorld;
using Verse;

namespace Niceties
{
    [HarmonyPatch(typeof(RoomRoleWorker_Bedroom), nameof(RoomRoleWorker.GetScore))]
    internal static class Patch_BedroomScore_Shared
    {
        private static void Postfix(Room room, ref float __result)
        {
            if (__result > 0f || !SharedRooms.IsMarked(room))
            {
                return;
            }

            __result = 100000f;
        }
    }

    [HarmonyPatch(typeof(RoomRoleWorker_Barracks), nameof(RoomRoleWorker.GetScore))]
    internal static class Patch_BarracksScore_Shared
    {
        private static void Postfix(Room room, ref float __result)
        {
            if (__result <= 0f || !SharedRooms.IsMarked(room))
            {
                return;
            }

            __result = 0f;
        }
    }

    [HarmonyPatch(typeof(Pawn), "CheckForDisturbedSleep")]
    internal static class Patch_CheckForDisturbedSleep
    {
        private static bool Prefix(Pawn __instance)
        {
            return !SharedRooms.ShouldSkipDisturbedSleep(__instance);
        }
    }
}

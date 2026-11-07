#nullable enable
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
        private static bool Prepare()
        {
            return PatchTargets.Exists(typeof(Pawn), "CheckForDisturbedSleep", new[] { typeof(Pawn) },
                PatchTargets.RoommateSleep);
        }

        private static bool Prefix(Pawn __instance, Pawn source)
        {
            return !SharedRooms.ShouldSkipDisturbedSleep(__instance, source);
        }
    }

    /// <summary>
    /// "Bedroom (shared)" wherever the game shows a room's role, such as the room stats
    /// readout, so the mark is visible without selecting a bed (AZR-703).
    /// </summary>
    [HarmonyPatch(typeof(Room), nameof(Room.GetRoomRoleLabel))]
    internal static class Patch_Room_GetRoomRoleLabel
    {
        private static LoadedLanguage? suffixLanguage;
        private static string suffix = "";

        private static void Postfix(Room __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result) || !SharedRooms.IsMarkedForDisplay(__instance))
            {
                return;
            }

            if (!ReferenceEquals(suffixLanguage, LanguageDatabase.activeLanguage))
            {
                suffixLanguage = LanguageDatabase.activeLanguage;
                suffix = " " + "Niceties_SharedRoleSuffix".Translate();
            }

            __result += suffix;
        }
    }
}

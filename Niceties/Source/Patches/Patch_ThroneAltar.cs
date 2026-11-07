#nullable enable
using HarmonyLib;
using RimWorld;
using Verse;

namespace Niceties
{
    /// <summary>
    /// Throne rooms are Royalty and altars are Ideology. Both room classes live in
    /// Assembly-CSharp, so the types always load, but there is nothing to patch for
    /// a player without both DLCs. Prepare skips both patches then (AZR-699).
    /// </summary>
    internal static class ThroneAltar
    {
        internal static bool DlcsActive => ModsConfig.RoyaltyActive && ModsConfig.IdeologyActive;
    }

    [HarmonyPatch(typeof(RoomRequirement_ForbidAltars), nameof(RoomRequirement.Met))]
    internal static class Patch_ForbidAltars_Met
    {
        private static bool Prepare() => ThroneAltar.DlcsActive;

        private static void Postfix(ref bool __result)
        {
            NicetiesSimFlags settings = NicetiesSim.Settings;
            if (settings.allowThroneAltars)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(RoomRoleWorker_WorshipRoom), nameof(RoomRoleWorker.GetScore))]
    internal static class Patch_WorshipRoom_GetScore
    {
        private static bool Prepare() => ThroneAltar.DlcsActive;

        private static void Postfix(Room room, ref float __result)
        {
            if (__result <= 0f)
            {
                return;
            }

            NicetiesSimFlags settings = NicetiesSim.Settings;
            if (!settings.allowThroneAltars)
            {
                return;
            }

            if (room == null)
            {
                return;
            }

            // The colony's thrones, not every thing in and around the room (AZR-549).
            Map? map = room.Map;
            if (map == null)
            {
                return;
            }

            foreach (Building_Throne throne in map.listerBuildings.AllBuildingsColonistOfClass<Building_Throne>())
            {
                if (throne.Spawned && throne.GetRoom() == room)
                {
                    __result = 0f;
                    return;
                }
            }
        }
    }
}

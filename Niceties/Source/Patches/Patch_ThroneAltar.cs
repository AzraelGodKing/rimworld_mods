using HarmonyLib;
using RimWorld;
using Verse;

namespace Niceties
{
    [HarmonyPatch(typeof(RoomRequirement_ForbidAltars), nameof(RoomRequirement.Met))]
    internal static class Patch_ForbidAltars_Met
    {
        private static void Postfix(ref bool __result)
        {
            if (NicetiesMod.Settings != null && NicetiesMod.Settings.allowThroneAltars)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(RoomRoleWorker_WorshipRoom), nameof(RoomRoleWorker.GetScore))]
    internal static class Patch_WorshipRoom_GetScore
    {
        private static void Postfix(Room room, ref float __result)
        {
            if (__result <= 0f)
            {
                return;
            }

            if (NicetiesMod.Settings == null || !NicetiesMod.Settings.allowThroneAltars)
            {
                return;
            }

            if (room == null)
            {
                return;
            }

            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                if (thing is Building_Throne)
                {
                    __result = 0f;
                    return;
                }
            }
        }
    }
}

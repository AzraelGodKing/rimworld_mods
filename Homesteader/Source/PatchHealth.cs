using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// SafePatchAll only logs a failed patch class. Features that depend on a fragile target
    /// read these flags so they can fall back or tell the player instead of silently doing nothing.
    /// </summary>
    internal static class PatchHealth
    {
        internal const string HarmonyId = "azraelgodking.homesteader";

        internal static bool RoomRebuildHooked { get; private set; } = true;

        internal static bool LandraceFrostHooked { get; private set; } = true;

        internal static bool LandraceDroughtHooked { get; private set; } = true;

        internal static bool SpoilageBillSortHooked { get; private set; } = true;

        internal static void Check()
        {
            RoomRebuildHooked =
                IsPatched(AccessTools.Method(typeof(RegionAndRoomUpdater),
                    nameof(RegionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms)))
                && Patch_PassiveCooling_RebuildAllRegions.MapField != null;
            LandraceFrostHooked = IsPatched(AccessTools.PropertyGetter(typeof(Plant),
                nameof(Plant.GrowthRateFactor_Temperature)));
            LandraceDroughtHooked = IsPatched(AccessTools.PropertyGetter(typeof(Plant),
                nameof(Plant.GrowthRateFactor_Fertility)));
            SpoilageBillSortHooked =
                IsPatched(AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredientsInSet"))
                || IsPatched(AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredientsInSet_AllowMix"));

            if (!RoomRebuildHooked)
            {
                Log.Warning("[Homesteader] Room-rebuild hook unavailable on this RimWorld build; passive cooling falls back to a periodic rescan.");
            }

            if (!LandraceFrostHooked || !LandraceDroughtHooked)
            {
                Log.Warning("[Homesteader] Landrace frost/drought growth hooks unavailable on this RimWorld build; those bonuses are shown as inactive.");
            }

            if (!SpoilageBillSortHooked)
            {
                Log.Warning("[Homesteader] Bill ingredient hooks unavailable on this RimWorld build; preserving bills no longer sort rot-first (eating still does).");
            }
        }

        private static bool IsPatched(MethodBase target)
        {
            if (target == null)
            {
                return false;
            }

            Patches info = Harmony.GetPatchInfo(target);
            return info != null && info.Owners.Contains(HarmonyId);
        }
    }
}

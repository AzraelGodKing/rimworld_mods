using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class Patch_PlaySettingsGlobalControls
    {
        public static void Postfix(WidgetRow row, bool worldView)
        {
            Patch_GasOverlay.DrawToggle(row, worldView);
            Patch_CombinedResourcesToggle.DrawToggle(row, worldView);
            Patch_SupportOverlay.DrawToggle(row, worldView);
        }
    }
}

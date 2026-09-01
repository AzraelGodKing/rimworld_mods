using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Play-settings toggle: green = carried rock ceiling, red = over-span.
    [StaticConstructorOnStartup]
    [HarmonyLib.HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class Patch_ShoringOverlay
    {
        public static bool ShowShoringOverlay;

        private static Texture2D icon;

        private static Texture2D Icon =>
            icon ??= ContentFinder<Texture2D>.Get("Things/Building/Furniture/Column", reportFailure: false)
                ?? BaseContent.WhiteTex;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }
            row.ToggleableIcon(ref ShowShoringOverlay, Icon,
                "Strata_PlaySettings_ShoringOverlayTip".Translate());
        }
    }
}

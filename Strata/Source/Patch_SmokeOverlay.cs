using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Adds a toggle to the bottom-right play-settings row (next to "show roof",
    // etc.) that turns on a per-cell smoke-percentage readout under the cursor.
    // The black smog overlay itself draws whenever there's smoke; this toggle
    // just adds the numeric readout, like a temperature overlay.
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class Patch_SmokeOverlay
    {
        public static bool ShowReadout;

        private static Texture2D icon;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }
            icon ??= ContentFinder<Texture2D>.Get("UI/Strata/SmokeToggle", reportFailure: false) ?? BaseContent.BadTex;
            row.ToggleableIcon(ref ShowReadout, icon, "Strata: show smoke levels");
        }
    }
}

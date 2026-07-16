using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Play-settings toggle for the Strata gas overlay: tinted room fill plus
    // a per-room mix readout (O₂, CO₂, smoke, deep gas, … as ratios).
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class Patch_GasOverlay
    {
        public static bool ShowGasOverlay;

        private static readonly Texture2D icon =
            ContentFinder<Texture2D>.Get("UI/Strata/SmokeToggle", reportFailure: false) ?? BaseContent.BadTex;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }
            row.ToggleableIcon(ref ShowGasOverlay, icon,
                "Strata: show gas overlay\n"
                + "Tints each room by its dominant hazardous gas. "
                + "Move the mouse over a room for a cursor-attached gas mix panel. "
                + "Optional per-room labels: Strata mod settings → Gas overlay room labels.");
        }
    }
}

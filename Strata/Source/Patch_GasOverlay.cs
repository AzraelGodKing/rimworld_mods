using UnityEngine;
using Verse;

namespace Strata
{
    // Play-settings toggle for the Strata gas overlay: tinted room fill plus
    // a per-room mix readout (O₂, CO₂, smoke, deep gas, … as ratios).
    public static class Patch_GasOverlay
    {
        public static bool ShowGasOverlay;

        // Lazy-filled on first UI draw (avoids BadTex if ContentFinder runs too early).
        private static Texture2D icon;

        private static Texture2D Icon =>
            icon ??= ContentFinder<Texture2D>.Get("UI/Strata/SmokeToggle", reportFailure: false)
                ?? BaseContent.WhiteTex;

        public static void DrawToggle(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }
            row.ToggleableIcon(ref ShowGasOverlay, Icon,
                "Strata_PlaySettings_GasOverlayTip".Translate());
        }
    }
}

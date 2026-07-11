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
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class Patch_SmokeOverlay
    {
        public static bool ShowReadout;

        // Loaded in the static constructor (main thread, after content load)
        // thanks to StaticConstructorOnStartup.
        private static readonly Texture2D icon =
            ContentFinder<Texture2D>.Get("UI/Strata/SmokeToggle", reportFailure: false) ?? BaseContent.BadTex;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }
            row.ToggleableIcon(ref ShowReadout, icon, "Strata: show smoke levels");
        }
    }
}

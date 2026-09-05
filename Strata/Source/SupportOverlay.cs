using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class Patch_SupportOverlay
    {
        public static bool ShowSupportOverlay;

        private static Texture2D icon;

        private static Texture2D Icon =>
            icon ??= ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel", reportFailure: false)
                ?? BaseContent.WhiteTex;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }
            row.ToggleableIcon(ref ShowSupportOverlay, Icon,
                "Strata_PlaySettings_SupportOverlayTip".Translate());
        }
    }

    // Visible unsupported span on underground floors (AZR-63).
    public class MapComponent_SupportOverlay : MapComponent
    {
        private readonly List<IntVec3> atRisk = new List<IntVec3>();
        private int lastRebuildTick = -9999;

        public MapComponent_SupportOverlay(Map map) : base(map)
        {
        }

        public IReadOnlyList<IntVec3> AtRiskCells
        {
            get
            {
                EnsureFresh();
                return atRisk;
            }
        }

        public override void MapComponentUpdate()
        {
            if (!Patch_SupportOverlay.ShowSupportOverlay
                || Find.CurrentMap != map
                || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            EnsureFresh();
            if (atRisk.Count > 0)
            {
                GenDraw.DrawFieldEdges(atRisk, new Color(0.92f, 0.35f, 0.12f, 0.85f));
            }
        }

        private void EnsureFresh()
        {
            int tick = Find.TickManager?.TicksGame ?? 0;
            if (tick - lastRebuildTick < 90)
            {
                return;
            }
            lastRebuildTick = tick;
            Rebuild();
        }

        public void Rebuild()
        {
            atRisk.Clear();
            if (!StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            CellRect bounds = CellRect.WholeMap(map);
            foreach (IntVec3 cell in bounds)
            {
                if (IsUnsupportedSpan(cell, map))
                {
                    atRisk.Add(cell);
                }
            }
        }

        public static bool IsUnsupportedSpan(IntVec3 c, Map map)
        {
            if (!c.InBounds(map)
                || !c.Standable(map)
                || c.Fogged(map)
                || map.roofGrid.RoofAt(c) != RoofDefOf.RoofRockThick
                || c.GetEdifice(map) != null)
            {
                return false;
            }

            if (RoofCollapseUtility.WithinRangeOfRoofHolder(c, map, assumeNonNoRoofCellsAreRoofed: false))
            {
                return false;
            }

            ShoringMapComponent shoring = map.GetComponent<ShoringMapComponent>();
            return shoring == null || !shoring.CellIsProtected(c);
        }
    }
}

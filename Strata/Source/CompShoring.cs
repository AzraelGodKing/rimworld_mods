using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_Shoring : CompProperties
    {
        // Cave-in protection radius — twice vanilla column roof support (6.9 → 13.8).
        public float protectionRadius = 13.8f;

        public CompProperties_Shoring()
        {
            compClass = typeof(CompShoring);
        }
    }

    // Reinforcement pillar — nearby excavations resist roof collapse.
    public class CompShoring : ThingComp
    {
        public CompProperties_Shoring Props => (CompProperties_Shoring)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<ShoringMapComponent>()?.Register(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<ShoringMapComponent>()?.Unregister(this);
            base.PostDeSpawn(map, mode);
        }

        public override string CompInspectStringExtra()
        {
            return "Strata_ShoringInspect".Translate(Props.protectionRadius.ToString("0.#"));
        }
    }

    public class ShoringMapComponent : MapComponent
    {
        private const int ScanInterval = 250;

        private const int CaveInGraceTicks = 15000;

        private static readonly Color SupportedColor = new Color(0.25f, 0.75f, 0.35f, 0.85f);

        private static readonly Color AtRiskColor = new Color(0.92f, 0.18f, 0.12f, 0.9f);

        private readonly HashSet<CompShoring> pillars = new HashSet<CompShoring>();

        private readonly List<IntVec3> supportedCells = new List<IntVec3>();

        private readonly List<IntVec3> atRiskCells = new List<IntVec3>();

        private readonly List<IntVec3> drawBuffer = new List<IntVec3>();

        private int lastScanTick = -9999;

        private int firstUnsupportedTick = -1;

        private bool warnedLetter;

        private bool lastScanFull;

        public ShoringMapComponent(Map map) : base(map)
        {
        }

        public void Register(CompShoring pillar)
        {
            pillars.Add(pillar);
            lastScanTick = -9999;
        }

        public void Unregister(CompShoring pillar)
        {
            pillars.Remove(pillar);
            lastScanTick = -9999;
        }

        public IReadOnlyList<IntVec3> AtRiskCells
        {
            get
            {
                EnsureScan();
                return atRiskCells;
            }
        }

        public bool HasUnsupportedSpan
        {
            get
            {
                EnsureScan();
                return atRiskCells.Count > 0;
            }
        }

        public bool CaveInGraceElapsed
        {
            get
            {
                EnsureScan();
                return firstUnsupportedTick >= 0
                    && Find.TickManager.TicksGame - firstUnsupportedTick >= CaveInGraceTicks;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref firstUnsupportedTick, "strataFirstUnsupportedTick", -1);
            Scribe_Values.Look(ref warnedLetter, "strataUnsupportedLetter", false);
        }

        public bool CellIsProtected(IntVec3 cell)
        {
            foreach (CompShoring pillar in pillars)
            {
                if (!pillar.parent.Spawned)
                {
                    continue;
                }
                float r = pillar.Props.protectionRadius;
                if (cell.InHorDistOf(pillar.parent.Position, r))
                {
                    return true;
                }
            }
            return false;
        }

        public int ActivePillarCount
        {
            get
            {
                int n = 0;
                foreach (CompShoring pillar in pillars)
                {
                    if (pillar.parent.Spawned)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        public override void MapComponentTick()
        {
            EnsureScan();
        }

        public override void MapComponentUpdate()
        {
            if (!Patch_ShoringOverlay.ShowShoringOverlay
                || Find.CurrentMap != map
                || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            EnsureScan();
            CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(1);
            DrawClipped(supportedCells, view, SupportedColor);
            DrawClipped(atRiskCells, view, AtRiskColor);
        }

        private void DrawClipped(List<IntVec3> cells, CellRect view, Color color)
        {
            drawBuffer.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                IntVec3 c = cells[i];
                if (view.Contains(c) && !c.Fogged(map))
                {
                    drawBuffer.Add(c);
                }
            }
            if (drawBuffer.Count > 0)
            {
                GenDraw.DrawFieldEdges(drawBuffer, color);
            }
        }

        private void EnsureScan(bool force = false)
        {
            int tick = Find.TickManager.TicksGame;
            bool needAllCells = Patch_ShoringOverlay.ShowShoringOverlay && Find.CurrentMap == map;
            int interval = needAllCells ? ScanInterval : ScanInterval * 6;
            if (!force && tick - lastScanTick < interval && !(needAllCells && !lastScanFull))
            {
                return;
            }
            lastScanTick = tick;
            lastScanFull = needAllCells;
            supportedCells.Clear();
            atRiskCells.Clear();
            if (!StrataMapUtility.IsUnderground(map))
            {
                firstUnsupportedTick = -1;
                warnedLetter = false;
                return;
            }

            foreach (IntVec3 c in map.AllCells)
            {
                if (!IsExcavatedThickRoof(c, map))
                {
                    continue;
                }
                if (CellIsProtected(c) || RoofCollapseUtility.WithinRangeOfRoofHolder(c, map))
                {
                    if (needAllCells)
                    {
                        supportedCells.Add(c);
                    }
                }
                else
                {
                    atRiskCells.Add(c);
                    if (!needAllCells && atRiskCells.Count > 0)
                    {
                        // Letter/alert only need to know a span exists.
                        break;
                    }
                }
            }

            if (atRiskCells.Count == 0)
            {
                firstUnsupportedTick = -1;
                warnedLetter = false;
                return;
            }
            if (firstUnsupportedTick < 0)
            {
                firstUnsupportedTick = tick;
            }
            if (!warnedLetter)
            {
                warnedLetter = true;
                IntVec3 look = atRiskCells[0];
                Find.LetterStack.ReceiveLetter(
                    "Strata_Letter_UnsupportedSpan_Label".Translate(),
                    "Strata_Letter_UnsupportedSpan_Text".Translate(),
                    LetterDefOf.NegativeEvent,
                    new TargetInfo(look, map));
            }
        }

        public static bool IsExcavatedThickRoof(IntVec3 c, Map map)
        {
            if (!c.InBounds(map)
                || c.Fogged(map)
                || map.roofGrid.RoofAt(c) != RoofDefOf.RoofRockThick
                || !c.Standable(map)
                || c.GetFirstMineable(map) != null)
            {
                return false;
            }
            Building edifice = c.GetEdifice(map);
            return edifice == null || edifice.def.Fillage != FillCategory.Full;
        }

        // Twice vanilla RoofCollapseUtility.RoofMaxSupportDistance for shoring pillars.
        public const float RoofSupportRadius = 13.8f;

        public static bool WithinRangeOfShoringRoofHolder(IntVec3 c, Map map, bool assumeNonNoRoofCellsAreRoofed)
        {
            ThingDef shoring = StrataThingDefOf.Strata_ShoringPillar;
            if (map == null || shoring == null)
            {
                return false;
            }
            bool connected = false;
            map.floodFiller.FloodFill(
                c,
                x => (x.Roofed(map) || x == c
                    || (assumeNonNoRoofCellsAreRoofed && !map.areaManager.NoRoof[x]))
                    && x.InHorDistOf(c, RoofSupportRadius),
                x =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        IntVec3 adj = x + GenAdj.CardinalDirectionsAndInside[i];
                        if (!adj.InBounds(map) || !adj.InHorDistOf(c, RoofSupportRadius))
                        {
                            continue;
                        }
                        Building edifice = adj.GetEdifice(map);
                        if (edifice != null && edifice.def == shoring)
                        {
                            connected = true;
                            return true;
                        }
                    }
                    return false;
                });
            return connected;
        }

        public static bool ConnectedToShoringRoofHolder(IntVec3 c, Map map, bool assumeRoofAtRoot)
        {
            ThingDef shoring = StrataThingDefOf.Strata_ShoringPillar;
            if (map == null || shoring == null)
            {
                return false;
            }
            bool connected = false;
            map.floodFiller.FloodFill(
                c,
                x => (x.Roofed(map) || (x == c && assumeRoofAtRoot)) && !connected,
                x =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        IntVec3 adj = x + GenAdj.CardinalDirectionsAndInside[i];
                        if (!adj.InBounds(map))
                        {
                            continue;
                        }
                        Building edifice = adj.GetEdifice(map);
                        if (edifice != null && edifice.def == shoring)
                        {
                            connected = true;
                            break;
                        }
                    }
                });
            return connected;
        }
    }

    [HarmonyPatch(typeof(RoofCollapseUtility), nameof(RoofCollapseUtility.WithinRangeOfRoofHolder))]
    public static class Patch_ShoringWithinRangeOfRoofHolder
    {
        public static void Postfix(IntVec3 c, Map map, bool assumeNonNoRoofCellsAreRoofed, ref bool __result)
        {
            if (!__result)
            {
                __result = ShoringMapComponent.WithinRangeOfShoringRoofHolder(c, map, assumeNonNoRoofCellsAreRoofed);
            }
        }
    }

    [HarmonyPatch(typeof(RoofCollapseUtility), nameof(RoofCollapseUtility.ConnectedToRoofHolder))]
    public static class Patch_ShoringConnectedToRoofHolder
    {
        public static void Postfix(IntVec3 c, Map map, bool assumeRoofAtRoot, ref bool __result)
        {
            if (!__result)
            {
                __result = ShoringMapComponent.ConnectedToShoringRoofHolder(c, map, assumeRoofAtRoot);
            }
        }
    }
}

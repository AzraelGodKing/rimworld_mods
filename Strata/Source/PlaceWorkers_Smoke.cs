using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Ghost overlay for one-way vents (exhaust fan, louver, smoke hole):
    // orange marks the intake room, green marks where the fumes go.
    public class PlaceWorker_OneWayVent : PlaceWorker
    {
        private static readonly Color IntakeColor = new Color(1f, 0.6f, 0.15f);
        private static readonly Color ExhaustColor = new Color(0.3f, 0.9f, 0.35f);

        private static readonly List<IntVec3> cellBuffer = new List<IntVec3>(1);

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }
            IntVec3 intake = center - rot.FacingCell;
            IntVec3 exhaust = center + rot.FacingCell;
            if (intake.InBounds(map))
            {
                cellBuffer.Clear();
                cellBuffer.Add(intake);
                GenDraw.DrawFieldEdges(cellBuffer, IntakeColor);
            }
            if (exhaust.InBounds(map))
            {
                cellBuffer.Clear();
                cellBuffer.Add(exhaust);
                GenDraw.DrawFieldEdges(cellBuffer, ExhaustColor);
            }
        }
    }

    // Ghost overlay for smoke ducts: highlights the duct network this tile
    // would join - green when the run reaches outdoors (it will vent), red
    // when it doesn't (dead pipe).
    public class PlaceWorker_SmokeDuct : PlaceWorker
    {
        private static readonly Color ReachesColor = new Color(0.3f, 0.9f, 0.35f);
        private static readonly Color DeadColor = new Color(0.95f, 0.3f, 0.25f);

        private static readonly HashSet<IntVec3> network = new HashSet<IntVec3>();
        private static readonly List<IntVec3> drawBuffer = new List<IntVec3>();

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;
            if (map == null || !center.InBounds(map))
            {
                return;
            }
            network.Clear();
            network.Add(center);
            foreach (IntVec3 dir in GenAdj.CardinalDirections)
            {
                IntVec3 next = center + dir;
                if (next.InBounds(map) && next.GetFirstBuilding(map)?.TryGetComp<CompSmokeDuct>() != null)
                {
                    SmokeVentUtility.CollectDuctNetwork(map, next, network);
                }
            }
            bool reaches = SmokeVentUtility.DuctNetworkReachesOutdoor(map, network);
            drawBuffer.Clear();
            drawBuffer.AddRange(network);
            GenDraw.DrawFieldEdges(drawBuffer, reaches ? ReachesColor : DeadColor);
        }
    }
}

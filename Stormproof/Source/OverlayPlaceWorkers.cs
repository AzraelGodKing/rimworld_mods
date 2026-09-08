using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Stormproof
{
    // Radius from the live CompProperties so the ring matches gameplay,
    // not a second number in specialDisplayRadius.
    public class PlaceWorker_RadiusOverlay : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            float radius = RadiusOf(def);
            if (radius > 0.1f)
            {
                GenDraw.DrawRadiusRing(center, radius);
            }
        }

        internal static float RadiusOf(ThingDef def)
        {
            if (def?.comps == null)
            {
                return 0f;
            }

            for (int i = 0; i < def.comps.Count; i++)
            {
                CompProperties comp = def.comps[i];
                if (comp is CompProperties_StormSpire spire)
                {
                    return spire.attractRadius;
                }
                if (comp is CompProperties_EmpDampener dampener)
                {
                    return dampener.protectRadius;
                }
                if (comp is CompProperties_StaticPylon pylon)
                {
                    return pylon.dischargeRadius;
                }
                if (comp is CompProperties_FireSuppressor suppressor)
                {
                    return suppressor.suppressRadius;
                }
            }

            return 0f;
        }
    }

    // Fallout scrubber is room-scoped. Highlight the enclosed room it would
    // actually scrub — on the blueprint and on select.
    public class PlaceWorker_EnclosedRoom : PlaceWorker
    {
        private static readonly Color RoomColor = new Color(0.35f, 0.78f, 1f);
        private static readonly List<IntVec3> cells = new List<IntVec3>(64);

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = thing?.Map ?? Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            Room room = thing != null && thing.Spawned ? thing.GetRoom() : center.GetRoom(map);
            if (room == null || room.PsychologicallyOutdoors || room.Dereferenced)
            {
                return;
            }

            cells.Clear();
            cells.AddRange(room.Cells);
            if (cells.Count > 0)
            {
                GenDraw.DrawFieldEdges(cells, RoomColor);
            }
        }
    }
}

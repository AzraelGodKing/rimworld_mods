using UnityEngine;
using Verse;

namespace NanoHiveReforged
{
    // The placement ghost shows the radius a new hive will get: the colony
    // default from the save, not the def's display radius.
    public class PlaceWorker_NanoHiveRange : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            float range = thing is Building_NanoHiveReforged hive
                ? hive.Config.range
                : GameComponent_NanoHive.Instance?.Defaults.range ?? NanoHiveSettings.DefaultRange;
            GenDraw.DrawRadiusRing(center, range);
        }
    }
}

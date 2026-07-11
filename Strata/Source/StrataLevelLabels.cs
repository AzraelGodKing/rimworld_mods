using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Player-assigned names for colony levels (surface and underground).
    public class StrataLevelLabels : WorldComponent
    {
        public Dictionary<int, string> labels = new Dictionary<int, string>();

        public StrataLevelLabels(World world) : base(world)
        {
        }

        public static StrataLevelLabels Get => Find.World?.GetComponent<StrataLevelLabels>();

        public string GetLabel(Map map)
        {
            return map != null && labels.TryGetValue(map.uniqueID, out string label) ? label : null;
        }

        public void SetLabel(Map map, string label)
        {
            if (map == null)
            {
                return;
            }
            if (label.NullOrEmpty())
            {
                labels.Remove(map.uniqueID);
            }
            else
            {
                labels[map.uniqueID] = label.Trim();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref labels, "labels", LookMode.Value, LookMode.Value);
            labels ??= new Dictionary<int, string>();
        }
    }
}

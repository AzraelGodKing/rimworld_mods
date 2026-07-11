using RimWorld;
using Verse;

namespace Strata
{
    // A wandering prospector reads the rock and points out a rich seam hidden
    // in the colony's own mountain. The surface-level cousin of a deep vein -
    // and a nudge to start digging. Fires on ordinary player home maps that
    // actually have rock to prospect.
    public class IncidentWorker_ProspectorTip : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && StrataMapUtility.IsSurfacePlayerHome(map);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!OreReveal.TryRevealVein(map, out IntVec3 location))
            {
                return false;
            }
            SendStandardLetter(parms, new TargetInfo(location, map));
            return true;
        }
    }
}

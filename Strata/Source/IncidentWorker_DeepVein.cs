using RimWorld;
using Verse;

namespace Strata
{
    // Excavation exposes an unexpectedly rich seam of ore in the surrounding
    // rock. A welcome reward for digging deep. Underground levels only.
    public class IncidentWorker_DeepVein : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && StrataMapUtility.IsUnderground(map);
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

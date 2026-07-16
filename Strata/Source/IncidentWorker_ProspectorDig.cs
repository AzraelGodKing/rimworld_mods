using RimWorld;
using Verse;

namespace Strata
{
    // Early-game positive: after digging-down research, a rich pocket appears on
    // a home map — the nudge before deep excavation.
    public class IncidentWorker_ProspectorDig : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            ResearchProjectDef digging = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Strata_DiggingDown");
            if (digging != null && !digging.IsFinished)
            {
                return false;
            }
            return parms.target is Map map && map.IsPlayerHome;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!OreReveal.TryRevealVein(map, out IntVec3 location, minCells: 4, maxCells: 10))
            {
                return false;
            }
            SendStandardLetter(parms, new TargetInfo(location, map));
            return true;
        }
    }
}

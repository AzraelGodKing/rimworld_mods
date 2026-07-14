using RimWorld;
using Verse;

namespace Strata
{
    public class IncidentWorker_GasPocket : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map
                && StrataMapUtility.IsUnderground(map)
                && DeepPocketUtility.TryGetUnbreached(map, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!DeepPocketUtility.TryGetUnbreached(map, out DeepPocketRecord pocket))
            {
                return false;
            }

            DeepPocketUtility.Breach(map, pocket, sendLetter: false);
            SendStandardLetter(parms, new TargetInfo(pocket.center, map));
            return true;
        }
    }
}

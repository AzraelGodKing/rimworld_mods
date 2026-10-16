using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    public class IncidentWorker_SunkenRuin : IncidentWorker
    {
        private const int TimeoutDays = 20;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return QuestSiteUtility.CanFireQuestSite() && Faction.OfInsects != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Site site = QuestSiteUtility.TryMakeSite(SunkenRuinDefOf.Strata_SunkenRuin, Faction.OfInsects, TimeoutDays);
            if (site == null)
            {
                return false;
            }
            SendStandardLetter(parms, site);
            return true;
        }
    }
}

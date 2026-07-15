using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Word reaches the colony of a ruin whose floor has given way onto an
    // ancient stairhead: a world site with an insect-held warren underneath and
    // a hoard at the bottom. Strata's first taste of multi-level exploration
    // away from home. Gated behind excavation research so the tip lands on
    // players already living the underground life.
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

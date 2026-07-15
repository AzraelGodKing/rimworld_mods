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
            if (StrataMod.Settings?.gasEventsEnabled == false)
            {
                return false;
            }
            if (Find.AnyPlayerHomeMap == null || Faction.OfInsects == null)
            {
                return false;
            }
            ResearchProjectDef excavation = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Strata_Excavation");
            return excavation == null || excavation.IsFinished;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TileFinder.TryFindNewSiteTile(out PlanetTile tile))
            {
                return false;
            }
            Site site = SiteMaker.MakeSite(SunkenRuinDefOf.Strata_SunkenRuin, tile, Faction.OfInsects,
                ifHostileThenMustRemainHostile: true,
                threatPoints: StorytellerUtility.DefaultSiteThreatPointsNow());
            if (site == null)
            {
                return false;
            }
            site.sitePartsKnown = true;
            site.GetComponent<TimeoutComp>()?.StartTimeout(TimeoutDays * GenDate.TicksPerDay);
            Find.WorldObjects.Add(site);
            SendStandardLetter(parms, site);
            return true;
        }
    }
}

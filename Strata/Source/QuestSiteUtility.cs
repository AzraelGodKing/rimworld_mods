using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    public static class QuestSiteUtility
    {
        public static bool CanFireQuestSite()
        {
            if (StrataMod.Settings?.explorationSitesEnabled == false)
            {
                return false;
            }
            if (StrataMod.Settings?.gasEventsEnabled == false)
            {
                return false;
            }
            if (Find.AnyPlayerHomeMap == null)
            {
                return false;
            }
            ResearchProjectDef excavation = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Strata_Excavation");
            return excavation == null || excavation.IsFinished;
        }

        public static Site TryMakeSite(SitePartDef part, Faction faction, int timeoutDays)
        {
            if (part == null || faction == null)
            {
                return null;
            }
            if (!TileFinder.TryFindNewSiteTile(out PlanetTile tile))
            {
                return null;
            }
            Site site = SiteMaker.MakeSite(part, tile, faction,
                ifHostileThenMustRemainHostile: true,
                threatPoints: StorytellerUtility.DefaultSiteThreatPointsNow());
            if (site == null)
            {
                return null;
            }
            site.sitePartsKnown = true;
            site.GetComponent<TimeoutComp>()?.StartTimeout(timeoutDays * GenDate.TicksPerDay);
            Find.WorldObjects.Add(site);
            return site;
        }
    }
}

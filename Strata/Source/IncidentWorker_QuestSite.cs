using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // World incident that spawns one of Strata's multi-level quest sites. The
    // target site part is resolved from the incident defName.
    public class IncidentWorker_QuestSite : IncidentWorker
    {
        private const int TimeoutDays = 20;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!QuestSiteUtility.CanFireQuestSite())
            {
                return false;
            }
            if (def == QuestSiteDefOf.Strata_SealedVaultDiscovered)
            {
                return Faction.OfMechanoids != null || Faction.OfInsects != null;
            }
            if (def == QuestSiteDefOf.Strata_GeothermalVentDiscovered)
            {
                return Faction.OfInsects != null;
            }
            // Collapsed mine: insects preferred but scavengers can fill in.
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            SitePartDef part = ResolveSitePart();
            Faction faction = ResolveFaction(part);
            if (part == null || faction == null)
            {
                return false;
            }
            Site site = QuestSiteUtility.TryMakeSite(part, faction, TimeoutDays);
            if (site == null)
            {
                return false;
            }
            SendStandardLetter(parms, site);
            return true;
        }

        private SitePartDef ResolveSitePart()
        {
            if (def == QuestSiteDefOf.Strata_CollapsedMineDiscovered)
            {
                return QuestSiteDefOf.Strata_CollapsedMine;
            }
            if (def == QuestSiteDefOf.Strata_SealedVaultDiscovered)
            {
                return QuestSiteDefOf.Strata_SealedVault;
            }
            if (def == QuestSiteDefOf.Strata_GeothermalVentDiscovered)
            {
                return QuestSiteDefOf.Strata_GeothermalVent;
            }
            return null;
        }

        private static Faction ResolveFaction(SitePartDef part)
        {
            if (part == QuestSiteDefOf.Strata_SealedVault && Faction.OfMechanoids != null)
            {
                return Faction.OfMechanoids;
            }
            if (Faction.OfInsects != null)
            {
                return Faction.OfInsects;
            }
            if (Faction.OfMechanoids != null)
            {
                return Faction.OfMechanoids;
            }
            foreach (Faction faction in Find.FactionManager.AllFactionsVisible)
            {
                if (faction.HostileTo(Faction.OfPlayer))
                {
                    return faction;
                }
            }
            return null;
        }
    }
}

using RimWorld;
using Verse;

namespace Strata
{
    // Landing on an upper level — climb down to the floor below (surface or A-n).
    public class Building_BuildUpLanding : Building_StairsUp
    {
        public override string EnterString => "Strata_Enter_GoDownstairs".Translate();

        public override string EnteringString => "Strata_Entering_GoDownstairs".Translate();

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            if (CanTearDownUnlinkedLanding())
            {
                return true;
            }
            return "Strata_OnlyWayDown".Translate();
        }
    }
}

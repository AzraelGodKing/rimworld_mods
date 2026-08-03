using RimWorld;
using Verse;

namespace Strata
{
    // Landing on an upper level — climb down to the floor below (surface or A-n).
    public class Building_BuildUpLanding : Building_StairsUp
    {
        public override string EnterString => "Go downstairs";

        public override string EnteringString => "going downstairs";

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

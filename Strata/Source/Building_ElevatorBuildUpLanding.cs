using Verse;

namespace Strata
{
    // Upper-floor elevator landing. Riding down is always available even if the
    // car above has lost power, so colonists can never be stranded on A1+.
    public class Building_ElevatorBuildUpLanding : Building_BuildUpLanding
    {
        public override string EnterString => "Strata_Enter_ElevatorDown".Translate();

        public override string EnteringString => "Strata_Entering_ElevatorDown".Translate();
    }
}

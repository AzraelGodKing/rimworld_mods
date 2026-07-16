namespace Strata
{
    // Upper-floor elevator landing. Riding down is always available even if the
    // car above has lost power, so colonists can never be stranded on A1+.
    public class Building_ElevatorBuildUpLanding : Building_BuildUpLanding
    {
        public override string EnterString => "Take elevator down";

        public override string EnteringString => "taking the elevator down";
    }
}

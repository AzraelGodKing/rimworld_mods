using Verse;

namespace Strata
{
    // The bottom landing of an elevator. Ascending is always available - even if
    // the elevator above has lost power - so colonists can never be stranded
    // below. Sealing and the missing-entrance safety net are inherited.
    public class Building_ElevatorUp : Building_StairsUp
    {
        public override string EnterString => "Strata_Enter_ElevatorUp".Translate();

        public override string EnteringString => "Strata_Entering_ElevatorUp".Translate();
    }
}

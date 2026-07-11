using RimWorld;
using Verse;

namespace Strata
{
    // A powered elevator: functionally a stairwell (opens and links a level,
    // carries pawns, exchanges heat, can be sealed), but compact and higher-tech.
    // Descending needs power to run the car; ascending is always allowed from
    // the bottom landing, so a power failure can never trap colonists below.
    public class Building_ElevatorDown : Building_StairsDown
    {
        public override string EnterString => "Take elevator down";

        public override string EnteringString => "taking the elevator down";

        public bool Powered
        {
            get
            {
                CompPowerTrader power = GetComp<CompPowerTrader>();
                return power == null || power.PowerOn;
            }
        }

        public override bool IsEnterable(out string reason)
        {
            if (!Powered)
            {
                reason = "The elevator has no power.";
                return false;
            }
            return base.IsEnterable(out reason);
        }
    }
}

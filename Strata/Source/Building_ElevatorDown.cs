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

        // PowerOn alone can't gate the ride: the shaft comp is a power
        // TRANSMITTER, so it always has a net (its own, if unwired) and
        // CompPowerShaft holds PowerOn true whenever wired so the tie can feed
        // a dark level. "Has power" for the car means the grid is actually
        // sustaining itself: producing at least what it consumes, or holding
        // charge in a battery.
        public bool Powered
        {
            get
            {
                CompPowerTrader power = GetComp<CompPowerTrader>();
                if (power == null)
                {
                    return true;
                }
                if (!power.PowerOn)
                {
                    return false; // flicked off or broken down
                }
                PowerNet net = power.PowerNet;
                if (net == null)
                {
                    return false;
                }
                return net.CurrentEnergyGainRate() >= 0f || net.CurrentStoredEnergy() > 1f;
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

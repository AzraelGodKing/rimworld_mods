using RimWorld;
using Verse;

namespace Strata
{
    // Powered tower elevator: opens/joins an upper outdoor level like the tower
    // stairwell, but compact. Ascending needs a live grid; the upper landing
    // always lets colonists ride back down so a blackout never traps anyone above.
    public class Building_ElevatorBuildUp : Building_StairsBuildUp
    {
        public override string EnterString => "Strata_Enter_ElevatorUp".Translate();

        public override string EnteringString => "Strata_Entering_ElevatorUp".Translate();

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
                    return false;
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

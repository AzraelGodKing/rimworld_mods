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

        protected override void Tick()
        {
            base.Tick();
            // The elevator shaft also carries power: tie the two levels' grids.
            if (Spawned && this.IsHashIntervalTick(60) && PocketMapExists
                && exit != null && exit.Spawned)
            {
                CompPowerShaft top = GetComp<CompPowerShaft>();
                CompPowerShaft bottom = exit.GetComp<CompPowerShaft>();
                if (top != null && bottom != null)
                {
                    top.DriveTie(bottom, 2000f);
                }
            }
        }
    }
}

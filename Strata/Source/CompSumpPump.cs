using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_SumpPump : CompProperties
    {
        public float clearRadius = 6f;

        public int clearIntervalTicks = 60;

        public CompProperties_SumpPump()
        {
            compClass = typeof(CompSumpPump);
        }
    }

    // Powered pump — clears flood water in a radius while running.
    public class CompSumpPump : ThingComp
    {
        public CompProperties_SumpPump Props => (CompProperties_SumpPump)props;

        public bool Active
        {
            get
            {
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power != null && power.PowerOn;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Props.clearIntervalTicks) || !Active)
            {
                return;
            }
            FloodMapComponent flood = parent.Map.GetComponent<FloodMapComponent>();
            flood?.ClearFloodsInRadius(parent.Position, Props.clearRadius);
        }

        public override string CompInspectStringExtra()
        {
            return Active
                ? "Pumping flood water within " + Props.clearRadius.ToString("0.#") + " tiles."
                : "Needs power to pump water.";
        }
    }
}

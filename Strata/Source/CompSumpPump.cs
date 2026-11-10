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
            // Guarded here, not by a Harmony wrapper: this runs every tick for every pump.
            if (!parent.IsHashIntervalTick(Props.clearIntervalTicks)
                || StrataFaultGuard.IsTripped(StrataFaultGuard.System.Flood)
                || !Active)
            {
                return;
            }
            try
            {
                FloodMapComponent flood = parent.Map.GetComponent<FloodMapComponent>();
                flood?.ClearFloodsInRadius(parent.Position, Props.clearRadius);
            }
            catch (System.Exception e)
            {
                StrataFaultGuard.Report(StrataFaultGuard.System.Flood, "running CompSumpPump.CompTick", e);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!Active)
            {
                return "Strata_SumpNeedsPower".Translate();
            }
            FloodMapComponent flood = parent.Map.GetComponent<FloodMapComponent>();
            if (flood == null || !flood.AnyFloodedInRadius(parent.Position, Props.clearRadius))
            {
                return "Strata_SumpNoFlood".Translate();
            }
            return "Strata_SumpPumping".Translate(Props.clearRadius.ToString("0.#"));
        }
    }
}

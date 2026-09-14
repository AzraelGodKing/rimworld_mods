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
            string table = WaterTableUtility.InspectRelative(parent.Map);
            string status;
            if (!Active)
            {
                status = "Strata_SumpNeedsPower".Translate();
            }
            else
            {
                FloodMapComponent flood = parent.Map.GetComponent<FloodMapComponent>();
                if (flood == null || !flood.AnyFloodedInRadius(parent.Position, Props.clearRadius))
                {
                    status = WaterTableUtility.SeepageActive(parent.Map)
                        ? "Strata_SumpHolding".Translate()
                        : "Strata_SumpNoFlood".Translate();
                }
                else
                {
                    status = "Strata_SumpPumping".Translate(Props.clearRadius.ToString("0.#"));
                }
            }
            return table.NullOrEmpty() ? status : status + "\n" + table;
        }
    }
}

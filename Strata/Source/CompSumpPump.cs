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
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            Map map = parent.Map;
            if (map != null && StrataMod.Settings != null && StrataMod.Settings.waterTableSeepageEnabled)
            {
                int below = WorldComponent_StrataStratum.Get?.LevelsBelowTable(map) ?? 0;
                if (below > 0)
                {
                    sb.AppendLine("Strata_SumpBelowTable".Translate(below));
                }
                else
                {
                    sb.AppendLine("Strata_SumpAboveTable".Translate());
                }
            }
            if (!Active)
            {
                sb.Append("Strata_SumpNeedsPower".Translate());
                return sb.ToString().TrimEnd();
            }
            FloodMapComponent flood = parent.Map.GetComponent<FloodMapComponent>();
            if (flood == null || !flood.AnyFloodedInRadius(parent.Position, Props.clearRadius))
            {
                sb.Append("Strata_SumpNoFlood".Translate());
            }
            else
            {
                sb.Append("Strata_SumpPumping".Translate(Props.clearRadius.ToString("0.#")));
            }
            return sb.ToString().TrimEnd();
        }
    }
}

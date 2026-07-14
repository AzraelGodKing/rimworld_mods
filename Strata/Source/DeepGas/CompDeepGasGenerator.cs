using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_DeepGasGenerator : CompProperties
    {
        public float gasPerTick = 0.5f;

        public float searchRadius = 8f;

        public CompProperties_DeepGasGenerator()
        {
            compClass = typeof(CompDeepGasGenerator);
        }
    }

    public class CompDeepGasGenerator : ThingComp
    {
        private CompDeepGasWell linkedWell;

        public CompProperties_DeepGasGenerator Props => (CompProperties_DeepGasGenerator)props;

        public override void CompTick()
        {
            base.CompTick();
            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power == null)
            {
                return;
            }
            if (!parent.Spawned || !parent.IsHashIntervalTick(60))
            {
                return;
            }
            linkedWell = FindWell();
            CompFlickable flick = parent.GetComp<CompFlickable>();
            bool wantsOn = flick == null || flick.SwitchIsOn;
            if (linkedWell == null || linkedWell.Stored < Props.gasPerTick * 60f)
            {
                power.PowerOn = false;
                return;
            }
            linkedWell.DrawGas(Props.gasPerTick * 60f);
            power.PowerOn = wantsOn;
        }

        private CompDeepGasWell FindWell()
        {
            Map map = parent.Map;
            if (map == null)
            {
                return null;
            }
            CompDeepGasWell best = null;
            float bestDist = float.MaxValue;
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing.def.defName != "Strata_GasWell" || !thing.Spawned)
                {
                    continue;
                }
                CompDeepGasWell well = thing.TryGetComp<CompDeepGasWell>();
                if (well == null || well.Stored <= 0f)
                {
                    continue;
                }
                float dist = thing.Position.DistanceTo(parent.Position);
                if (dist <= Props.searchRadius && dist < bestDist)
                {
                    bestDist = dist;
                    best = well;
                }
            }
            return best;
        }

        public override string CompInspectStringExtra()
        {
            if (linkedWell == null)
            {
                return "Needs a gas well with stored fuel nearby.";
            }
            return $"Drawing from gas well ({linkedWell.Stored:F0} stored).";
        }
    }
}

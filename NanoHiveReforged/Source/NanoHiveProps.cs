using Verse;

namespace NanoHiveReforged
{
    /// <summary>
    /// Balance numbers on the hive's ThingDef, so patches can change them.
    /// </summary>
    public class NanoHiveProps : DefModExtension
    {
        // Repair radius a new colony starts with. The selection ring and the
        // placement ghost draw the real radius, not specialDisplayRadius.
        public float defaultRange = 12f;

        // Watts drawn while nothing in range needs repair. The full
        // basePowerConsumption is drawn while repairing.
        public float idlePowerConsumption = 200f;

        // Per repair cycle a hive spends at most rate x budgetTargets HP in
        // total, lowest HP% first.
        public int budgetTargets = 40;

        public static NanoHiveProps For(ThingDef def)
        {
            return def.GetModExtension<NanoHiveProps>() ?? Default;
        }

        private static readonly NanoHiveProps Default = new NanoHiveProps();
    }
}

using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_DeepGasWell : CompProperties
    {
        public float extractPerTick = 0.35f;

        public float maxStorage = 400f;

        public CompProperties_DeepGasWell()
        {
            compClass = typeof(CompDeepGasWell);
        }
    }

    public class CompDeepGasWell : ThingComp
    {
        private float stored;

        public CompProperties_DeepGasWell Props => (CompProperties_DeepGasWell)props;

        public float Stored => stored;

        public bool HasVent =>
            parent.Map?.GetComponent<MapComponent_DeepPockets>()?.IsGasSourceCell(parent.Position) == true;

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned || !parent.IsHashIntervalTick(30))
            {
                return;
            }
            if (!HasVent || stored >= Props.maxStorage)
            {
                return;
            }
            stored = Mathf.Min(Props.maxStorage, stored + Props.extractPerTick * 30f);
        }

        public float DrawGas(float amount)
        {
            float moved = Mathf.Min(amount, stored);
            stored -= moved;
            return moved;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref stored, "storedGas");
        }

        public override string CompInspectStringExtra()
        {
            return $"Stored natural gas: {stored:F0} / {Props.maxStorage:F0}";
        }
    }
}

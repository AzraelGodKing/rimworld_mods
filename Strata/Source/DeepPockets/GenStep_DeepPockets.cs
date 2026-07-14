using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class GenStep_DeepPockets : GenStep
    {
        public override int SeedPart => 384729105;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            MapComponent_DeepPockets comp = map.GetComponent<MapComponent_DeepPockets>();
            if (comp == null)
            {
                comp = new MapComponent_DeepPockets(map);
                map.components.Add(comp);
            }

            int depth = StrataDepth.Of(map);
            int geothermal = Rand.RangeInclusive(0, 1 + depth / 2);
            int gasPockets = Rand.RangeInclusive(1, 2 + depth);

            for (int i = 0; i < geothermal; i++)
            {
                if (DeepPocketUtility.TrySeedPocket(map, DeepPocketKind.Geothermal, 3f, out DeepPocketRecord record))
                {
                    comp.Register(record);
                }
            }

            for (int i = 0; i < gasPockets; i++)
            {
                DeepPocketKind kind = Rand.Value < 0.45f + depth * 0.05f
                    ? DeepPocketKind.NaturalGas
                    : DeepPocketKind.ToxicGas;
                if (DeepPocketUtility.TrySeedPocket(map, kind, 2.5f, out DeepPocketRecord record))
                {
                    comp.Register(record);
                }
            }
        }
    }
}

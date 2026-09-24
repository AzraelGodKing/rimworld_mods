using System.Reflection;
using RimWorld;
using Verse;

namespace Strata
{
    // Fail-open: Stormproof brownout slows the freight lift; no Stormproof → 0.
    internal static class StormproofBrownout
    {
        public static float For(Thing thing)
        {
            if (thing?.Map == null || !SisterModBridges.StormproofLoaded)
            {
                return 0f;
            }
            MapComponent comp = null;
            foreach (MapComponent mc in thing.Map.components)
            {
                if (mc != null && mc.GetType().Name == "MapComponent_Stormproof")
                {
                    comp = mc;
                    break;
                }
            }
            if (comp == null)
            {
                return 0f;
            }
            MethodInfo method = comp.GetType().GetMethod("BrownoutFor", new[] { typeof(Thing) });
            if (method == null)
            {
                return 0f;
            }
            object raw = method.Invoke(comp, new object[] { thing });
            return raw is float f ? f : 0f;
        }
    }
}

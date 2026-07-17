using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    // Food on a root cellar uses the cellar's cool ceiling for AmbientTemperature,
    // so CompRottable (and rot inspect tips) treat pantry stock as chilled.
    [HarmonyPatch(typeof(Thing), nameof(Thing.AmbientTemperature), MethodType.Getter)]
    public static class Patch_RootCellarAmbientTemperature
    {
        public static void Postfix(Thing __instance, ref float __result)
        {
            // Only spoilables — don't chill pawns standing on the cellar.
            if (__instance.TryGetComp<CompRottable>() == null)
            {
                return;
            }

            CompRootCellarCooling cellar = RootCellarUtility.GetCoolingCellar(__instance);
            if (cellar == null)
            {
                return;
            }
            __result = Mathf.Min(__result, cellar.Props.maxTemperature);
        }
    }

    public static class RootCellarUtility
    {
        public static CompRootCellarCooling GetCoolingCellar(Thing thing)
        {
            if (thing?.Map == null || !thing.Spawned)
            {
                return null;
            }

            // Items sit on storage cells; the edifice under them is the cellar.
            Building edifice = thing.Position.GetEdifice(thing.Map);
            CompRootCellarCooling comp = edifice?.TryGetComp<CompRootCellarCooling>();
            if (comp != null)
            {
                return comp;
            }

            // Fallback: multi-cell buildings / odd stacking.
            foreach (Thing t in thing.Position.GetThingList(thing.Map))
            {
                if (t is Building building)
                {
                    CompRootCellarCooling found = building.TryGetComp<CompRootCellarCooling>();
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }
    }
}

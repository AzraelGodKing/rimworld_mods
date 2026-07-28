using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    // A ready surge protector eats the "Zzzt!" short circuit incident:
    // no explosion, no battery drain, just sparks at the protector.
    [HarmonyPatch(typeof(IncidentWorker_ShortCircuit), "TryExecuteWorker")]
    public static class Patch_ShortCircuit
    {
        public static bool Prefix(IncidentWorker_ShortCircuit __instance, IncidentParms parms, ref bool __result)
        {
            Map map = (Map)parms.target;
            if (map == null)
            {
                return true;
            }
            // Never Absorb() a no-op Zzzt (e.g. storm spire roll with no shortable net).
            if (!__instance.CanFireNow(parms))
            {
                return true;
            }
            CompSurgeProtector protector = StormproofRegistry
                .On(StormproofRegistry.SurgeProtectors, map)
                .FirstOrDefault(p => p.ReadyToAbsorb);
            if (protector == null)
            {
                return true;
            }
            protector.Absorb();
            __result = true;
            return false;
        }
    }
}

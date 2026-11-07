using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    public static class ZzztUtility
    {
        // Same test vanilla's worker uses, without the storyteller's refire and
        // cooldown checks in CanFireNow: forced Zzzts (ion storm, spire surge)
        // must still reach a ready surge protector.
        public static bool Shortable(Map map) =>
            map != null && ShortCircuitUtility.GetShortCircuitablePowerConduits(map).Any();

        public static void TryFire(Map map)
        {
            IncidentDef zzzt = StormproofIncidentDefOf.ShortCircuit;
            if (zzzt == null || !Shortable(map))
            {
                return;
            }
            zzzt.Worker.TryExecute(StorytellerUtility.DefaultParmsNow(zzzt.category, map));
        }

        public static CompSurgeProtector ReadyProtector(Map map)
        {
            List<CompSurgeProtector> protectors = StormproofRegistry.SurgeProtectors.On(map);
            for (int i = 0; i < protectors.Count; i++)
            {
                if (protectors[i].ReadyToAbsorb)
                {
                    return protectors[i];
                }
            }
            return null;
        }
    }

    // A ready surge protector eats the "Zzzt!" short circuit incident:
    // no explosion, no battery drain, just sparks at the protector.
    [HarmonyPatch(typeof(IncidentWorker_ShortCircuit), "TryExecuteWorker")]
    public static class Patch_ShortCircuit
    {
        public static bool Prefix(IncidentParms parms, ref bool __result, ref bool __state)
        {
            __state = false;
            Map map = parms?.target as Map;
            // Never Absorb() a no-op Zzzt: vanilla would find nothing to short.
            if (!ZzztUtility.Shortable(map))
            {
                return true;
            }
            CompSurgeProtector protector = ZzztUtility.ReadyProtector(map);
            if (protector == null)
            {
                return true;
            }
            protector.Absorb();
            MapComponent_Stormproof.For(map)?.NoteZzzt(absorbed: true);
            __state = true;
            __result = true;
            return false;
        }

        public static void Postfix(IncidentParms parms, bool __result, bool __state)
        {
            if (__state || !__result)
            {
                return;
            }
            MapComponent_Stormproof.For(parms?.target as Map)?.NoteZzzt(absorbed: false);
        }
    }
}

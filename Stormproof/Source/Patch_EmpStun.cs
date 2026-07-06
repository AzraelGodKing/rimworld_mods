using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    // Player buildings inside a powered EMP dampener's field shrug off EMP stuns.
    [HarmonyPatch(typeof(StunHandler), nameof(StunHandler.Notify_DamageApplied))]
    public static class Patch_EmpStun
    {
        public static bool Prefix(StunHandler __instance, DamageInfo dinfo)
        {
            if (dinfo.Def != DamageDefOf.EMP)
            {
                return true;
            }
            if (!(__instance.parent is Building building) ||
                building.Faction != Faction.OfPlayer ||
                !building.Spawned)
            {
                return true;
            }
            bool shielded = StormproofRegistry
                .On(StormproofRegistry.Dampeners, building.Map)
                .Any(d => d.Covers(building));
            if (shielded)
            {
                FleckMaker.ThrowMicroSparks(building.DrawPos, building.Map);
                return false;
            }
            return true;
        }
    }
}

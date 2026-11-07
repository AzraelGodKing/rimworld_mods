using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    // Player buildings inside a powered EMP dampener's field shrug off EMP stuns.
    // With the setting on, so do the colony's own mechanoids (AZR-717).
    [HarmonyPatch(typeof(StunHandler), nameof(StunHandler.Notify_DamageApplied))]
    public static class Patch_EmpStun
    {
        public static bool Prefix(StunHandler __instance, DamageInfo dinfo)
        {
            if (dinfo.Def != DamageDefOf.EMP)
            {
                return true;
            }
            Thing thing = __instance.parent;
            if (thing == null || !thing.Spawned || thing.Faction != Faction.OfPlayer || !Covered(thing))
            {
                return true;
            }
            if (!CompEmpDampener.Block(thing))
            {
                return true;
            }
            FleckMaker.ThrowMicroSparks(thing.DrawPos, thing.Map);
            return false;
        }

        private static bool Covered(Thing thing)
        {
            if (thing is Building)
            {
                return true;
            }
            return StormproofMod.Settings.dampenerCoversMechs
                && thing is Pawn pawn
                && pawn.RaceProps != null
                && pawn.RaceProps.IsMechanoid;
        }
    }
}

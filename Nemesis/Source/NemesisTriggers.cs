using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Hunt triggers that must not fire on colony internment / executions.
    /// Steam Aug 11–17: executing a prisoner started a hunt (cinematic escape
    /// or "killed ally"), then the pawn showed up on the world map sedated.
    /// </summary>
    public static class NemesisTriggers
    {
        public static bool IsColonyInternedOrExecution(Pawn pawn, DamageInfo? dinfo)
        {
            if (pawn == null) return false;

            if (dinfo.HasValue && dinfo.Value.Def == DamageDefOf.ExecutionCut)
                return true;

            if (pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
                return true;

            if (pawn.guest != null && pawn.guest.HostFaction == Faction.OfPlayer)
                return true;

            // Ideology / warden executions often happen in a colony bed after
            // guest status has already flipped.
            if (pawn.InBed())
            {
                Building_Bed bed = pawn.CurrentBed();
                if (bed != null && bed.Faction == Faction.OfPlayer)
                    return true;
            }

            return false;
        }
    }
}

using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace Nemesis
{
    /// <summary>
    /// Keep the nemesis pawn out of the "Lord owns a free world pawn" illegal state:
    /// always leave lords before PassToWorld, and always leave WorldPawns before map spawn.
    /// </summary>
    public static class NemesisPawnUtil
    {
        public static void DetachFromLord(Pawn pawn)
        {
            if (pawn == null) return;
            Lord lord = pawn.GetLord();
            if (lord == null) return;
            lord.Notify_PawnLost(pawn, PawnLostCondition.ExitedMap);
        }

        public static void EnsureNotWorldPawn(Pawn pawn)
        {
            if (pawn == null) return;
            if (Find.WorldPawns.GetSituation(pawn) != WorldPawnSituation.None)
                Find.WorldPawns.RemovePawn(pawn);
        }

        public static void ParkAsWorldNemesis(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed) return;

            DetachFromLord(pawn);

            if (pawn.Spawned)
                pawn.DeSpawn(DestroyMode.Vanish);

            if (!pawn.IsWorldPawn())
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
        }

        public static bool TrySpawnOnMap(Pawn pawn, Map map, IntVec3 cell)
        {
            if (pawn == null || pawn.Destroyed || map == null || !cell.IsValid) return false;

            DetachFromLord(pawn);

            if (pawn.Spawned)
            {
                if (pawn.Map == map && pawn.Position == cell)
                {
                    EnsureNotWorldPawn(pawn);
                    return true;
                }
                pawn.DeSpawn(DestroyMode.Vanish);
            }

            EnsureNotWorldPawn(pawn);
            GenSpawn.Spawn(pawn, cell, map);

            // Spawn can race with KeepForever bookkeeping — force clear if still flagged.
            if (pawn.IsWorldPawn())
                Find.WorldPawns.RemovePawn(pawn);

            return pawn.Spawned;
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
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

        /// <summary>
        /// Recover enough body HP that a return raid/assault does not instantly re-flee
        /// (CheckNemesisHealth flees below 30%).
        /// </summary>
        public static void RecoverForReturn(Pawn pawn, float minHealth = 0.7f)
        {
            if (pawn?.health == null || pawn.Dead || pawn.Destroyed) return;

            HediffDef bloodLossDef = DefDatabase<HediffDef>.GetNamedSilentFail("BloodLoss");
            if (bloodLossDef != null)
            {
                Hediff bloodLoss = pawn.health.hediffSet.GetFirstHediffOfDef(bloodLossDef);
                if (bloodLoss != null)
                    pawn.health.RemoveHediff(bloodLoss);
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int guard = 0; guard < 48; guard++)
            {
                if (pawn.health.summaryHealth.SummaryHealthPercent >= minHealth)
                    break;

                Hediff_Injury worst = null;
                float worstSev = 0f;
                for (int i = 0; i < hediffs.Count; i++)
                {
                    if (hediffs[i] is Hediff_Injury injury && injury.Severity > worstSev)
                    {
                        worst = injury;
                        worstSev = injury.Severity;
                    }
                }

                if (worst == null)
                    break;

                worst.Heal(Mathf.Max(1f, worst.Severity));
            }

            // Clear lingering anesthetic so they can fight on return.
            HediffDef anesthetic = DefDatabase<HediffDef>.GetNamedSilentFail("Anesthetic");
            if (anesthetic != null)
            {
                Hediff a = pawn.health.hediffSet.GetFirstHediffOfDef(anesthetic);
                if (a != null)
                    pawn.health.RemoveHediff(a);
            }
        }

        public static void ParkAsWorldNemesis(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed) return;

            DetachFromLord(pawn);

            if (pawn.Spawned)
                pawn.DeSpawn(DestroyMode.Vanish);

            RecoverForReturn(pawn);

            if (!pawn.IsWorldPawn())
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
        }

        public static bool TrySpawnOnMap(Pawn pawn, Map map, IntVec3 cell)
        {
            if (pawn == null || pawn.Destroyed || map == null || !cell.IsValid) return false;

            DetachFromLord(pawn);
            RecoverForReturn(pawn);

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

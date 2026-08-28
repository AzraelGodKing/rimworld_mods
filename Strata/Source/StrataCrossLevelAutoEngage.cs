using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Hostiles and colonists auto-fire through open sky; drafted FireAtWill overwatch.
    public static class StrataCrossLevelAutoEngage
    {
        private const int FailCooldownTicks = 700;
        private const int MaxEngagesPerScan = 4;
        private const int MaxTargetProbes = 4;
        private const int OverwatchRetryTicks = 60;

        private static readonly Dictionary<int, int> cooldownUntil = new Dictionary<int, int>();
        private static readonly List<Pawn> tmpTargets = new List<Pawn>();

        public static bool AutoEngageEnabled =>
            StrataCrossLevelCombat.Enabled
            && (StrataMod.Settings == null || StrataMod.Settings.crossLevelAutoEngage);

        [StrataSessionReset]
        public static void ResetSession() => cooldownUntil.Clear();

        public static void ScanPair(Map sky, Map surface)
        {
            if (!AutoEngageEnabled || sky == null || surface == null || sky.Disposed || surface.Disposed)
            {
                return;
            }

            ScanHostiles(surface, sky);
            ScanHostiles(sky, surface);
            ScanColonists(surface, sky);
            ScanColonists(sky, surface);
            StrataCrossLevelTurret.AcquireAuto(sky, surface);
        }

        public static void TryOverwatchCrossFire(Pawn p)
        {
            try
            {
                if (!AutoEngageEnabled || p == null || !p.Spawned || p.Dead || p.Downed) return;
                if (!p.Drafted || p.drafter == null || !p.drafter.FireAtWill || p.InMentalState) return;

                int now = Find.TickManager.TicksGame;
                if (!Ready(p, now)) return;

                Map paired = StrataCrossLevelCombat.PairedGapMap(p.Map);
                if (paired == null || paired.Disposed)
                {
                    Charge(p, now + 1400);
                    return;
                }

                if (HasNearbySameMapThreat(p))
                {
                    Charge(p, now + OverwatchRetryTicks);
                    return;
                }

                if (StrataCrossLevelCombat.GetRangedVerb(p) == null)
                {
                    Charge(p, now + 1400);
                    return;
                }

                if (!TryEngageAcross(p, paired, allowReposition: false))
                {
                    Charge(p, now + OverwatchRetryTicks);
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Overwatch cross-fire failed: " + e.Message, 0x5B7100B);
            }
        }

        private static void ScanHostiles(Map shooterMap, Map targetMap)
        {
            if (targetMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Count == 0) return;

            IReadOnlyList<Pawn> pawns = shooterMap.mapPawns.AllPawnsSpawned;
            int engaged = 0;
            int now = Find.TickManager.TicksGame;

            for (int i = pawns.Count - 1; i >= 0 && engaged < MaxEngagesPerScan; i--)
            {
                if (i >= pawns.Count) continue;
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Downed || !p.Spawned) continue;
                if (p.Faction == Faction.OfPlayer || !p.HostileTo(Faction.OfPlayer) || p.InMentalState) continue;
                if (p.CurJobDef == StrataDefOf.Strata_CrossLevelAttack) continue;
                if (!Ready(p, now)) continue;
                if (!IsIdleEnough(p)) continue;

                if (StrataCrossLevelCombat.GetRangedVerb(p) == null)
                {
                    Charge(p, now + 1400);
                    continue;
                }

                if (TryEngageAcross(p, targetMap, allowReposition: true))
                {
                    engaged++;
                }
                else
                {
                    Charge(p, now + FailCooldownTicks);
                }
            }
        }

        private static void ScanColonists(Map shooterMap, Map targetMap)
        {
            List<Pawn> colonists = shooterMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            int engaged = 0;
            int now = Find.TickManager.TicksGame;

            for (int i = 0; i < colonists.Count && engaged < MaxEngagesPerScan; i++)
            {
                Pawn p = colonists[i];
                if (p == null || p.Dead || p.Downed || !p.Spawned || p.InMentalState) continue;
                if (!p.IsColonistPlayerControlled && !p.IsColonyMech) continue;
                if (p.CurJobDef == StrataDefOf.Strata_CrossLevelAttack) continue;
                if (!Ready(p, now)) continue;
                if (!IsIdleEnough(p)) continue;
                // Drafted FireAtWill is handled by overwatch; here allow undrafted (and drafted without nearby threat scan).
                if (p.Drafted && p.drafter != null && !p.drafter.FireAtWill) continue;

                if (StrataCrossLevelCombat.GetRangedVerb(p) == null)
                {
                    Charge(p, now + 1400);
                    continue;
                }

                if (HasNearbySameMapThreat(p))
                {
                    Charge(p, now + OverwatchRetryTicks);
                    continue;
                }

                if (TryEngageAcross(p, targetMap, allowReposition: !p.Drafted))
                {
                    engaged++;
                }
                else
                {
                    Charge(p, now + FailCooldownTicks);
                }
            }
        }

        private static bool IsIdleEnough(Pawn p)
        {
            JobDef job = p.CurJobDef;
            if (job == null) return true;
            if (job == JobDefOf.Wait || job == JobDefOf.Wait_Combat || job == JobDefOf.Wait_Wander) return true;
            if (job == JobDefOf.GotoWander) return true;
            // Undrafted colonists mid-work should not drop tools to snipe across floors.
            if (p.IsColonistPlayerControlled && !p.Drafted && p.CurJob != null && !p.CurJob.playerForced)
            {
                return job == JobDefOf.Wait
                    || job == JobDefOf.Wait_SafeTemperature
                    || job == JobDefOf.Wait_Wander
                    || p.mindState?.IsIdle == true;
            }
            return HostileHasNoLocalTarget(p);
        }

        private static bool HostileHasNoLocalTarget(Pawn p)
        {
            if (p.Faction == Faction.OfPlayer) return false;
            Thing focus = p.mindState?.enemyTarget;
            if (focus == null || focus.Destroyed || !focus.Spawned) return true;
            return focus.Map != p.Map;
        }

        private static bool TryEngageAcross(Pawn shooter, Map targetMap, bool allowReposition)
        {
            Verb verb = StrataCrossLevelCombat.GetRangedVerb(shooter);
            if (verb == null || targetMap == null || targetMap.Disposed) return false;

            bool shooterHostile = shooter.HostileTo(Faction.OfPlayer);
            tmpTargets.Clear();
            IReadOnlyList<Pawn> pawns = targetMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn t = pawns[i];
                if (t == null || t.Dead || !t.Spawned || t.Downed) continue;
                if (shooterHostile)
                {
                    if (t.Faction != Faction.OfPlayer) continue;
                }
                else if (!t.HostileTo(Faction.OfPlayer))
                {
                    continue;
                }
                tmpTargets.Add(t);
            }
            if (tmpTargets.Count == 0) return false;

            IntVec3 origin = shooter.Position;
            tmpTargets.Sort((a, b) =>
                (a.Position - origin).LengthHorizontalSquared.CompareTo((b.Position - origin).LengthHorizontalSquared));

            int limit = Math.Min(tmpTargets.Count, MaxTargetProbes);
            for (int i = 0; i < limit; i++)
            {
                Pawn target = tmpTargets[i];
                if (!allowReposition
                    && !StrataCrossLevelCombat.CanFireFrom(shooter.Map, shooter.Position, target, verb, out _))
                {
                    continue;
                }
                if (StrataCrossLevelCombat.TryStartAutoEngage(shooter, target, allowReposition))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasNearbySameMapThreat(Pawn p)
        {
            const float rangeSq = 1600f;
            if (p.Faction == null)
            {
                List<Pawn> players = p.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
                for (int i = 0; i < players.Count; i++)
                {
                    Pawn o = players[i];
                    if (o.Dead || o.Downed) continue;
                    if ((o.Position - p.Position).LengthHorizontalSquared <= rangeSq) return true;
                }
                return false;
            }

            int n = 0;
            foreach (IAttackTarget at in p.Map.attackTargetsCache.TargetsHostileToFaction(p.Faction))
            {
                if (++n > 10) return true;
                Thing t = at.Thing;
                if (t == null || t.Destroyed || !t.Spawned || at.ThreatDisabled(p)) continue;
                if ((t.Position - p.Position).LengthHorizontalSquared <= rangeSq) return true;
            }
            return false;
        }

        private static bool Ready(Pawn p, int now)
        {
            if (cooldownUntil.Count > 512) cooldownUntil.Clear();
            return !cooldownUntil.TryGetValue(p.thingIDNumber, out int until) || now >= until;
        }

        private static void Charge(Pawn p, int until)
        {
            if (cooldownUntil.Count > 512) cooldownUntil.Clear();
            cooldownUntil[p.thingIDNumber] = until;
        }
    }
}

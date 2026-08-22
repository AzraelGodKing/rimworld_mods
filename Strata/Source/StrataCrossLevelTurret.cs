using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Strata
{
    // Direct turrets and mortars (arc / flyOverhead) firing through open sky.
    public static class StrataCrossLevelTurret
    {
        private enum Phase { Warmup, Burst, Cooldown }

        private enum FireResult { Fired, Hold, Dead }

        private sealed class Entry
        {
            public Building_Turret turret;
            public LocalTargetInfo target;
            public Map targetMap;
            public bool arc;
            public bool auto;
            public Phase phase;
            public int nextEventTick;
            public int burstShotsLeft;
            public int revalidateAt;
        }

        private const int RevalidateInterval = 30;
        private const int MaxAutoAcquiresPerScan = 3;
        private const int MaxAutoTargetProbes = 4;

        private static readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
        private static readonly Dictionary<int, int> nextAutoTry = new Dictionary<int, int>();
        private static readonly List<int> tmpDead = new List<int>();
        private static readonly List<Pawn> tmpTargets = new List<Pawn>();

        public static void ClearAll()
        {
            entries.Clear();
            nextAutoTry.Clear();
        }

        public static Verb LauncherVerb(Building_Turret turret)
        {
            Verb verb = turret?.AttackVerb;
            return StrataCrossLevelCombat.IsRangedLaunchVerb(verb) ? verb : null;
        }

        public static bool IsArc(Verb verb)
        {
            if (verb == null) return false;
            ThingDef proj = StrataCrossLevelCombat.GetProjectile(verb);
            if (proj?.projectile?.flyOverhead == true) return true;
            if (verb.verbProps.defaultProjectile?.projectile?.flyOverhead == true) return true;
            return !verb.verbProps.requireLineOfSight;
        }

        public static bool TurretCanFire(
            Building_Turret turret, Thing target, Verb verb, out StrataCrossLevelCombat.GapShot shot)
        {
            return StrataCrossLevelCombat.CanCrossGapFire(turret, target, verb, out shot);
        }

        public static bool TryOrder(Building_Turret turret, LocalTargetInfo target, Map targetMap)
        {
            try
            {
                Verb verb = LauncherVerb(turret);
                if (verb == null || !target.IsValid || targetMap == null) return false;

                bool arc = IsArc(verb);
                if (arc)
                {
                    if (!StrataCrossLevelCombat.CanArcFireAt(
                        turret.Map, turret.Position, target.Cell, targetMap, verb, out _))
                    {
                        Messages.Message("Strata_NoArcPath".Translate(), turret, MessageTypeDefOf.RejectInput, historical: false);
                        return false;
                    }
                }
                else if (!target.HasThing || !TurretCanFire(turret, target.Thing, verb, out _))
                {
                    Messages.Message("Strata_NoGapLine".Translate(), turret, MessageTypeDefOf.RejectInput, historical: false);
                    return false;
                }

                Store(turret, target, targetMap, arc, auto: false);
                SoundDefOf.TurretAcquireTarget?.PlayOneShot(new TargetInfo(turret.Position, turret.Map));
                Messages.Message(
                    (arc ? "Strata_MortarTargetSet" : "Strata_TurretTargetSet").Translate(turret.LabelShort),
                    turret,
                    MessageTypeDefOf.SilentInput,
                    historical: false);
                return true;
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Cross-level turret order failed: " + e.Message, 0x5B71009);
                return false;
            }
        }

        public static bool HasOrder(Building_Turret turret, out LocalTargetInfo target, out Map targetMap)
        {
            target = LocalTargetInfo.Invalid;
            targetMap = null;
            if (turret == null || !entries.TryGetValue(turret.thingIDNumber, out Entry e)) return false;
            target = e.target;
            targetMap = e.targetMap;
            return true;
        }

        public static void Cancel(Building_Turret turret)
        {
            if (turret != null) entries.Remove(turret.thingIDNumber);
        }

        private static void Store(Building_Turret turret, LocalTargetInfo target, Map targetMap, bool arc, bool auto)
        {
            if (entries.Count > 128) entries.Clear();
            entries[turret.thingIDNumber] = new Entry
            {
                turret = turret,
                target = target,
                targetMap = targetMap,
                arc = arc,
                auto = auto,
                phase = Phase.Warmup,
                nextEventTick = Find.TickManager.TicksGame + WarmupTicks(turret),
                revalidateAt = Find.TickManager.TicksGame + RevalidateInterval,
            };
        }

        public static void TryAutoAcquire(Building_Turret turret)
        {
            try
            {
                if (!StrataCrossLevelAutoEngage.AutoEngageEnabled || turret == null || !turret.Spawned) return;
                if (entries.ContainsKey(turret.thingIDNumber)) return;
                if (turret.ForcedTarget.IsValid) return;

                int now = Find.TickManager.TicksGame;
                if (nextAutoTry.TryGetValue(turret.thingIDNumber, out int until) && now < until) return;

                Map paired = StrataCrossLevelCombat.PairedGapMap(turret.Map);
                if (paired == null)
                {
                    Charge(turret, now + 2800);
                    return;
                }

                Verb verb = LauncherVerb(turret);
                if (verb == null || turret.Faction == null)
                {
                    Charge(turret, now + 2800);
                    return;
                }

                if (paired.mapPawns.AllPawnsSpawned.Count == 0)
                {
                    Charge(turret, now + 120);
                    return;
                }

                Pawn target = FindAutoTarget(turret, verb, paired);
                if (target == null)
                {
                    Charge(turret, now + 120);
                    return;
                }

                Store(turret, target, paired, IsArc(verb), auto: true);
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Turret cross-level auto-acquire failed: " + e.Message, 0x5B7100A);
            }
        }

        public static void AcquireAuto(Map sky, Map surface)
        {
            int n = AcquireOnMap(sky, surface, MaxAutoAcquiresPerScan);
            AcquireOnMap(surface, sky, MaxAutoAcquiresPerScan - n);
        }

        private static int AcquireOnMap(Map shooterMap, Map targetMap, int budget)
        {
            if (budget <= 0 || shooterMap == null || targetMap == null) return 0;
            int now = Find.TickManager.TicksGame;
            int n = AcquireFromList(shooterMap.listerBuildings.allBuildingsColonist, targetMap, budget, now);
            if (n < budget)
            {
                n += AcquireFromList(shooterMap.listerBuildings.allBuildingsNonColonist, targetMap, budget - n, now);
            }
            return n;
        }

        private static int AcquireFromList(List<Building> buildings, Map targetMap, int budget, int now)
        {
            int n = 0;
            for (int i = 0; i < buildings.Count && n < budget; i++)
            {
                if (!(buildings[i] is Building_Turret turret)) continue;
                if (entries.ContainsKey(turret.thingIDNumber)) continue;
                if (nextAutoTry.TryGetValue(turret.thingIDNumber, out int until) && now < until) continue;

                Verb verb = LauncherVerb(turret);
                if (verb == null || turret.Faction == null) continue;

                if (turret.CurrentTarget.IsValid || turret.ForcedTarget.IsValid)
                {
                    Charge(turret, now + 700);
                    continue;
                }

                if (!ReadyToFire(turret, verb))
                {
                    Charge(turret, now + 700);
                    continue;
                }

                Pawn target = FindAutoTarget(turret, verb, targetMap);
                if (target == null)
                {
                    Charge(turret, now + 700);
                    continue;
                }

                Store(turret, target, targetMap, IsArc(verb), auto: true);
                n++;
            }
            return n;
        }

        private static void Charge(Building_Turret turret, int untilTick)
        {
            if (nextAutoTry.Count > 256) nextAutoTry.Clear();
            nextAutoTry[turret.thingIDNumber] = untilTick;
        }

        private static Pawn FindAutoTarget(Building_Turret turret, Verb verb, Map targetMap)
        {
            tmpTargets.Clear();
            IReadOnlyList<Pawn> pawns = targetMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Downed || !p.Spawned) continue;
                if (!p.HostileTo(turret)) continue;
                tmpTargets.Add(p);
            }
            if (tmpTargets.Count == 0) return null;

            IntVec3 origin = turret.Position;
            tmpTargets.Sort((a, b) =>
                (a.Position - origin).LengthHorizontalSquared.CompareTo((b.Position - origin).LengthHorizontalSquared));

            int limit = Math.Min(tmpTargets.Count, MaxAutoTargetProbes);
            for (int i = 0; i < limit; i++)
            {
                Pawn p = tmpTargets[i];
                bool ok = IsArc(verb)
                    ? StrataCrossLevelCombat.CanArcFireAt(turret.Map, turret.Position, p.Position, targetMap, verb, out _)
                    : TurretCanFire(turret, p, verb, out _);
                if (ok) return p;
            }
            return null;
        }

        public static void TickPair(Map sky, Map surface, int nowOverride = -1)
        {
            if (entries.Count == 0) return;
            int now = nowOverride >= 0 ? nowOverride : Find.TickManager.TicksGame;
            tmpDead.Clear();

            foreach (KeyValuePair<int, Entry> kv in entries)
            {
                Entry e = kv.Value;
                Building_Turret turret = e.turret;
                if (turret == null || turret.Destroyed || !turret.Spawned)
                {
                    tmpDead.Add(kv.Key);
                    continue;
                }
                if (turret.Map != sky && turret.Map != surface) continue;

                if (now >= e.revalidateAt)
                {
                    e.revalidateAt = now + RevalidateInterval;
                    if (!Revalidate(e, turret))
                    {
                        tmpDead.Add(kv.Key);
                        continue;
                    }
                }

                FaceTarget(turret, e);
                if (now < e.nextEventTick) continue;

                switch (e.phase)
                {
                    case Phase.Warmup:
                        e.phase = Phase.Burst;
                        Verb warmVerb = LauncherVerb(turret);
                        e.burstShotsLeft = StrataCrossLevelCombat.BurstShotCount(warmVerb);
                        e.nextEventTick = now;
                        goto case Phase.Burst;

                    case Phase.Burst:
                        switch (TryFireOne(e, turret, now))
                        {
                            case FireResult.Dead:
                                tmpDead.Add(kv.Key);
                                break;
                            case FireResult.Hold:
                                break;
                            default:
                                e.burstShotsLeft--;
                                if (e.burstShotsLeft > 0)
                                {
                                    Verb burstVerb = LauncherVerb(turret);
                                    e.nextEventTick = now + Mathf.Max(1, burstVerb?.verbProps.ticksBetweenBurstShots ?? 10);
                                }
                                else
                                {
                                    e.phase = Phase.Cooldown;
                                    Verb coolVerb = LauncherVerb(turret);
                                    Pawn manner = turret.TryGetComp<CompMannable>()?.ManningPawn;
                                    e.nextEventTick = now + (coolVerb != null
                                        ? CooldownTicks(turret, coolVerb, manner)
                                        : 250);
                                }
                                break;
                        }
                        break;

                    case Phase.Cooldown:
                        e.phase = Phase.Warmup;
                        e.nextEventTick = now + WarmupTicks(turret);
                        break;
                }
            }

            for (int i = 0; i < tmpDead.Count; i++)
            {
                entries.Remove(tmpDead[i]);
            }
        }

        private static bool Revalidate(Entry e, Building_Turret turret)
        {
            if (e.targetMap == null || e.targetMap.Disposed) return false;
            if (e.target.HasThing)
            {
                Thing t = e.target.Thing;
                if (t == null || t.Destroyed || !t.Spawned || t.MapHeld != e.targetMap) return false;
            }
            else if (!e.target.Cell.InBounds(e.targetMap))
            {
                return false;
            }

            if (turret.CurrentTarget.IsValid) return !e.auto;

            Verb verb = LauncherVerb(turret);
            if (verb == null) return false;

            bool ok = e.arc
                ? StrataCrossLevelCombat.CanArcFireAt(turret.Map, turret.Position, e.target.Cell, e.targetMap, verb, out _)
                : (e.target.HasThing && TurretCanFire(turret, e.target.Thing, verb, out _));

            if (ok) return true;
            if (!e.auto)
            {
                Messages.Message("Strata_GapLineLost".Translate(turret.LabelShort), turret, MessageTypeDefOf.NeutralEvent, historical: false);
            }
            return false;
        }

        private static void FaceTarget(Building_Turret turret, Entry e)
        {
            IntVec3 delta = e.target.Cell - turret.Position;
            Vector3 v = delta.ToVector3();
            if (v.sqrMagnitude <= 0.01f) return;
            float angle = v.AngleFlat();
            if (turret is Building_TurretGun gun && gun.Top != null)
            {
                gun.Top.CurRotation = angle;
                return;
            }

            StrataCombatExtendedSoftCompat.FaceTurret(turret, angle);
        }

        private static FireResult TryFireOne(Entry e, Building_Turret turret, int now)
        {
            Verb verb = LauncherVerb(turret);
            if (verb == null) return FireResult.Dead;
            if (!ReadyToFire(turret, verb))
            {
                e.nextEventTick = now + 60;
                return FireResult.Hold;
            }

            Pawn manner = turret.TryGetComp<CompMannable>()?.ManningPawn;
            NotifyShellUsed(verb);

            if (e.arc)
            {
                if (!StrataCrossLevelCombat.CanArcFireAt(
                    turret.Map, turret.Position, e.target.Cell, e.targetMap, verb, out var shot))
                {
                    return FireResult.Dead;
                }
                return StrataCrossLevelCombat.FireArcShot(turret, manner, verb, e.target, e.targetMap, shot.distance)
                    ? FireResult.Fired
                    : FireResult.Dead;
            }

            if (!e.target.HasThing) return FireResult.Dead;
            return StrataCrossLevelCombat.Fire(turret, verb, e.target.Thing) ? FireResult.Fired : FireResult.Dead;
        }

        private static void NotifyShellUsed(Verb verb)
        {
            ThingWithComps eq = verb.EquipmentSource;
            eq?.TryGetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
        }

        private static bool ReadyToFire(Building_Turret turret, Verb verb)
        {
            CompMannable mannable = turret.TryGetComp<CompMannable>();
            if (mannable != null && !mannable.MannedNow) return false;
            CompPowerTrader power = turret.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn) return false;
            if (StrataCrossLevelCombat.GetProjectile(verb) == null) return false;
            if (!StrataCombatExtendedSoftCompat.CanFire(verb))
            {
                StrataCombatExtendedSoftCompat.TryReload(verb);
                return false;
            }
            if (turret.CurrentTarget.IsValid) return false;
            return true;
        }

        private static int WarmupTicks(Building_Turret turret)
        {
            BuildingProperties b = turret.def.building;
            float sec = b != null ? b.turretBurstWarmupTime.RandomInRange : 0f;
            return Mathf.Max(1, Mathf.RoundToInt(sec * 60f));
        }

        private static int CooldownTicks(Building_Turret turret, Verb verb, Pawn manner)
        {
            float sec = turret.def.building?.turretBurstCooldownTime ?? -1f;
            if (sec <= 0f)
            {
                try { sec = verb.verbProps.AdjustedCooldown(verb, manner); }
                catch { sec = verb.verbProps.defaultCooldownTime; }
            }
            return Mathf.Max(1, Mathf.RoundToInt(sec * 60f));
        }
    }
}

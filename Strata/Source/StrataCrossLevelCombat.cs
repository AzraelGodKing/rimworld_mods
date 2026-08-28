using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Strata
{
    // Ranged fire through Strata_OpenSky between an A+ deck and its source map.
    public static class StrataCrossLevelCombat
    {
        public struct GapShot
        {
            public Map targetMap;
            public float distance;
        }

        public const float GapHeight = 4f;

        internal static readonly Dictionary<int, Thing> PendingTargets = new Dictionary<int, Thing>();

        private static readonly List<IntVec3> tmpCandidates = new List<IntVec3>();

        public static bool Enabled =>
            StrataMod.Settings == null || StrataMod.Settings.crossLevelCombatEnabled;

        [StrataSessionReset]
        public static void ResetSession()
        {
            PendingTargets.Clear();
            StrataCrossLevelTurret.ClearAll();
            StrataCrossLevelAutoEngage.ResetSession();
        }

        public static Map PairedGapMap(Map map)
        {
            if (map == null || map.Disposed) return null;
            if (StrataMapUtility.IsUpperLevel(map))
            {
                return UpperDeckUtility.SourceMapFor(map);
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map other = maps[i];
                if (other == null || other.Disposed || other == map) continue;
                if (StrataMapUtility.IsUpperLevel(other) && UpperDeckUtility.SourceMapFor(other) == map)
                {
                    return other;
                }
            }
            return null;
        }

        public static bool IsRangedLaunchVerb(Verb verb)
        {
            if (verb == null || verb.verbProps == null || verb.verbProps.IsMeleeAttack)
            {
                return false;
            }

            if (verb is Verb_LaunchProjectile)
            {
                return true;
            }

            return StrataCombatExtendedSoftCompat.IsLaunchVerb(verb);
        }

        public static ThingDef GetProjectile(Verb verb)
        {
            if (verb is Verb_LaunchProjectile vanilla)
            {
                return vanilla.Projectile;
            }

            return StrataCombatExtendedSoftCompat.GetProjectile(verb);
        }

        public static int BurstShotCount(Verb verb)
        {
            if (verb == null) return 1;
            PropertyInfo shots = AccessTools.Property(verb.GetType(), "ShotsPerBurst")
                ?? AccessTools.Property(typeof(Verb), "ShotsPerBurst");
            if (shots != null)
            {
                try
                {
                    object value = shots.GetValue(verb);
                    if (value is int n) return Mathf.Max(1, n);
                }
                catch
                {
                    // Fall through to verbProps.
                }
            }

            return Mathf.Max(1, verb.verbProps.burstShotCount);
        }

        public static bool CanArcFireAt(
            Map shooterMap, IntVec3 sCol, IntVec3 tCol, Map targetMap, Verb verb, out GapShot shot)
        {
            shot = default;
            if (!Enabled || shooterMap == null || verb == null || targetMap == null) return false;
            if (!AreCrossGapPaired(shooterMap, targetMap, out Map skyMap, out _)) return false;
            if (!sCol.InBounds(shooterMap) || !tCol.InBounds(targetMap)) return false;

            // Gap column must be open sky (target col when firing down, shooter col when firing up).
            IntVec3 gapOnSky = shooterMap == skyMap
                ? ToSkyCell(targetMap, skyMap, tCol)
                : ToSkyCell(shooterMap, skyMap, sCol);
            if (!IsOpenSky(skyMap, gapOnSky)) return false;

            IntVec3 delta = tCol - sCol;
            float dist = Mathf.Sqrt(delta.LengthHorizontalSquared + GapHeight * GapHeight);
            float range = verb.EffectiveRange;
            if (range <= 1.42f || dist > range) return false;
            float min = verb.verbProps.minRange;
            if (min > 0f && dist < min) return false;

            shot = new GapShot { targetMap = targetMap, distance = dist };
            return true;
        }

        public static bool FireArcShot(
            Thing shooter, Pawn manningPawn, Verb verb, LocalTargetInfo target, Map targetMap, float distance)
        {
            try
            {
                if (shooter == null || verb == null || targetMap == null || targetMap.Disposed || !target.IsValid)
                {
                    return false;
                }

                ThingDef projDef = GetProjectile(verb);
                if (projDef == null) return false;

                IntVec3 dest = target.Cell;
                IntVec3 spawn = shooter.Position;
                spawn.x = Mathf.Clamp(spawn.x, 0, targetMap.Size.x - 1);
                spawn.z = Mathf.Clamp(spawn.z, 0, targetMap.Size.z - 1);
                Vector3 launchPos = spawn.ToVector3Shifted();

                if (StrataCombatExtendedSoftCompat.IsLaunchVerb(verb))
                {
                    bool ceArc = StrataCombatExtendedSoftCompat.TryFire(
                        shooter, verb, target, targetMap, spawn, launchPos, distance, arc: true);
                    if (ceArc)
                    {
                        verb.verbProps.soundCast?.PlayOneShot(new TargetInfo(shooter.Position, shooter.Map));
                    }
                    return ceArc;
                }

                var projectile = (Projectile)GenSpawn.Spawn(projDef, spawn, targetMap);
                Thing equipment = verb.EquipmentSource;
                float miss = verb.verbProps.ForcedMissRadius;
                if (manningPawn != null)
                {
                    miss *= verb.verbProps.GetForceMissFactorFor(equipment, manningPawn);
                }
                float adj = VerbUtility.CalculateAdjustedForcedMiss(miss, dest - spawn);
                IntVec3 impact = dest;
                if (adj > 0.5f)
                {
                    int cells = GenRadial.NumCellsInRadius(adj);
                    impact = dest + GenRadial.RadialPattern[Rand.Range(0, cells)];
                }

                ProjectileHitFlags flags = ProjectileHitFlags.NonTargetWorld;
                if (Rand.Chance(0.5f))
                {
                    flags = ProjectileHitFlags.All;
                }

                if (impact == dest && target.HasThing)
                {
                    projectile.Launch(shooter, launchPos, target, target, flags, preventFriendlyFire: false, equipment);
                }
                else
                {
                    projectile.Launch(shooter, launchPos, impact, target, flags, preventFriendlyFire: false, equipment);
                }

                verb.verbProps.soundCast?.PlayOneShot(new TargetInfo(shooter.Position, shooter.Map));
                return true;
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Cross-level arc fire failed: " + e.Message, 0x5B71007);
                return false;
            }
        }

        public static bool AreCrossGapPaired(Map a, Map b, out Map skyMap, out Map surfaceMap)
        {
            skyMap = null;
            surfaceMap = null;
            if (a == null || b == null || a == b || a.Disposed || b.Disposed) return false;

            if (StrataMapUtility.IsUpperLevel(a) && UpperDeckUtility.SourceMapFor(a) == b)
            {
                skyMap = a;
                surfaceMap = b;
                return true;
            }
            if (StrataMapUtility.IsUpperLevel(b) && UpperDeckUtility.SourceMapFor(b) == a)
            {
                skyMap = b;
                surfaceMap = a;
                return true;
            }
            return false;
        }

        private static IntVec3 ToSkyCell(Map fromMap, Map skyMap, IntVec3 cell)
        {
            if (fromMap == skyMap || fromMap.Size == skyMap.Size) return cell;
            return StrataBelowRenderer.LowerToSkyCell(fromMap, skyMap, cell);
        }

        private static bool IsOpenSky(Map sky, IntVec3 cell)
        {
            if (!cell.InBounds(sky)) return false;
            TerrainDef t = sky.terrainGrid.TerrainAt(cell);
            return t != null && t.defName == UpperDeckUtility.OpenSkyDefName;
        }

        private static bool ExposedToGap(Map skyMap, IntVec3 col)
        {
            if (IsOpenSky(skyMap, col)) return true;
            for (int i = 0; i < 4; i++)
            {
                IntVec3 n = col + GenAdj.CardinalDirections[i];
                if (IsOpenSky(skyMap, n)) return true;
            }
            return false;
        }

        private static bool HasApertureTowards(Map skyMap, IntVec3 from, IntVec3 to)
        {
            if (IsOpenSky(skyMap, from)) return true;
            for (int i = 0; i < 4; i++)
            {
                IntVec3 n = from + GenAdj.CardinalDirections[i];
                if (IsOpenSky(skyMap, n)) return true;
            }

            IntVec3 delta = to - from;
            int limit = Mathf.Max(1, (int)(delta.LengthHorizontal * 0.5f));
            int nSeen = 0;
            foreach (IntVec3 c in GenSight.PointsOnLineOfSight(from, to))
            {
                if (++nSeen > limit) break;
                if (IsOpenSky(skyMap, c)) return true;
            }
            return false;
        }

        public static Verb GetRangedVerb(Pawn p)
        {
            if (p == null) return null;
            Verb primary = p.equipment?.PrimaryEq?.PrimaryVerb;
            if (IsRangedLaunchVerb(primary))
            {
                return primary;
            }

            List<Verb> verbs = p.verbTracker?.AllVerbs;
            if (verbs == null) return null;
            for (int i = 0; i < verbs.Count; i++)
            {
                if (IsRangedLaunchVerb(verbs[i]))
                {
                    return verbs[i];
                }
            }
            return null;
        }

        public static bool CanCrossGapFire(Thing shooter, Thing target, Verb verb, out GapShot shot)
        {
            shot = default;
            if (shooter == null || !shooter.Spawned) return false;
            foreach (IntVec3 cell in shooter.OccupiedRect())
            {
                if (CanFireFrom(shooter.Map, cell, target, verb, out shot))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CanFireFrom(Map shooterMap, IntVec3 sCol, Thing target, Verb verb, out GapShot shot)
        {
            shot = default;
            if (!Enabled || shooterMap == null || verb == null || target == null) return false;
            if (target.Destroyed || !target.Spawned) return false;

            Map targetMap = target.MapHeld;
            if (!AreCrossGapPaired(shooterMap, targetMap, out Map skyMap, out _)) return false;
            if (!sCol.InBounds(shooterMap)) return false;

            IntVec3 targetPos = target.Position;
            IntVec3 skyShooter = ToSkyCell(shooterMap, skyMap, sCol);
            IntVec3 skyTarget = ToSkyCell(targetMap, skyMap, targetPos);
            if (!skyTarget.InBounds(skyMap) || !skyShooter.InBounds(skyMap)) return false;

            if (shooterMap == skyMap)
            {
                // Shooting down: target column must be open sky; shooter needs an aperture.
                if (!IsOpenSky(skyMap, skyTarget)) return false;
                if (!HasApertureTowards(skyMap, skyShooter, skyTarget)) return false;
            }
            else
            {
                // Shooting up: shooter outdoors, target exposed at open sky.
                if (shooterMap.roofGrid.Roofed(sCol)) return false;
                if (!ExposedToGap(skyMap, skyTarget)) return false;
            }

            if (!GenSight.LineOfSight(skyShooter, skyTarget, skyMap, skipFirstCell: true))
            {
                return false;
            }

            IntVec3 delta = skyTarget - skyShooter;
            float dist = Mathf.Sqrt(delta.LengthHorizontalSquared + GapHeight * GapHeight);
            float range = verb.EffectiveRange;
            if (range <= 1.42f || dist > range) return false;
            float min = verb.verbProps.minRange;
            if (min > 0f && dist < min) return false;

            shot = new GapShot { targetMap = targetMap, distance = dist };
            return true;
        }

        public static float ComputeAimChance(Thing shooter, Verb verb, Thing target, float distance)
        {
            float chance = 1f;
            if (verb.verbProps.canGoWild)
            {
                chance *= ShotReport.HitFactorFromShooter(shooter, distance);
            }
            chance *= verb.verbProps.GetHitChanceFactor(verb.EquipmentSource, distance);
            if (target.MapHeld != null)
            {
                chance *= target.MapHeld.weatherManager.CurWeatherAccuracyMultiplier;
            }
            if (chance < 0.0201f) chance = 0.0201f;
            if (target is Pawn pawn)
            {
                chance *= Mathf.Clamp(pawn.BodySize, 0.5f, 2f);
            }
            else
            {
                chance *= Mathf.Clamp(target.def.fillPercent * target.def.size.x * target.def.size.z * 2.5f, 0.5f, 2f);
            }
            return Mathf.Clamp01(chance);
        }

        public static bool Fire(Thing shooter, Verb verb, Thing target)
        {
            try
            {
                if (shooter == null || verb == null || target == null) return false;
                if (!CanCrossGapFire(shooter, target, verb, out GapShot shot)) return false;

                ThingDef projDef = GetProjectile(verb);
                if (projDef == null) return false;

                Map targetMap = shot.targetMap;
                IntVec3 delta = shooter.Position - target.Position;
                Vector3 away = delta.ToVector3();
                away.y = 0f;
                away = away.sqrMagnitude > 0.001f ? away.normalized : Vector3.forward;
                float back = Mathf.Min(2f, Mathf.Max(1f, shot.distance - 1f));
                Vector3 launchPos = target.DrawPos + away * back;
                IntVec3 spawnCell = launchPos.ToIntVec3();
                if (!spawnCell.InBounds(targetMap))
                {
                    spawnCell = target.Position;
                    launchPos = target.DrawPos;
                }

                if (StrataCombatExtendedSoftCompat.IsLaunchVerb(verb))
                {
                    bool ce = StrataCombatExtendedSoftCompat.TryFire(
                        shooter, verb, target, targetMap, spawnCell, launchPos, shot.distance, arc: false);
                    if (ce)
                    {
                        verb.verbProps.soundCast?.PlayOneShot(new TargetInfo(shooter.Position, shooter.Map));
                    }
                    return ce;
                }

                var projectile = (Projectile)GenSpawn.Spawn(projDef, spawnCell, targetMap);
                float hitChance = ComputeAimChance(shooter, verb, target, shot.distance);
                bool hit = Rand.Chance(hitChance);
                Thing equipment = verb.EquipmentSource;

                if (hit)
                {
                    ProjectileHitFlags flags = ProjectileHitFlags.IntendedTarget;
                    if (!target.def.destroyable || target.def.Fillage == FillCategory.Full)
                    {
                        flags |= ProjectileHitFlags.NonTargetPawns;
                    }
                    projectile.Launch(shooter, launchPos, target, target, flags, preventFriendlyFire: false, equipment);
                }
                else
                {
                    var line = new ShootLine(spawnCell, target.Position);
                    bool overhead = projDef.projectile != null && projDef.projectile.flyOverhead;
                    line.ChangeDestToMissWild(hitChance, overhead, targetMap);
                    projectile.Launch(shooter, launchPos, line.Dest, target, ProjectileHitFlags.NonTargetPawns | ProjectileHitFlags.NonTargetWorld,
                        preventFriendlyFire: false, equipment);
                }

                verb.verbProps.soundCast?.PlayOneShot(new TargetInfo(shooter.Position, shooter.Map));
                return true;
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Cross-level fire failed: " + e.Message, 0x5B71005);
                return false;
            }
        }

        public static IntVec3 FindFiringCell(Pawn shooter, Thing target, Verb verb)
        {
            if (shooter == null || !shooter.Spawned) return IntVec3.Invalid;
            if (CanFireFrom(shooter.Map, shooter.Position, target, verb, out _))
            {
                return shooter.Position;
            }

            Map map = shooter.Map;
            float radius = Mathf.Clamp(verb.EffectiveRange, 4f, 30f);
            tmpCandidates.Clear();

            IntVec3 searchCenter = shooter.Position;
            if (AreCrossGapPaired(map, target.MapHeld, out Map sky, out _) && map == sky)
            {
                searchCenter = ToSkyCell(target.MapHeld, sky, target.Position);
            }

            int scanned = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(searchCenter, radius, useCenter: true))
            {
                if (++scanned > 600) break;
                if (!cell.InBounds(map) || !cell.Standable(map)) continue;
                if (!CanFireFrom(map, cell, target, verb, out _)) continue;
                tmpCandidates.Add(cell);
                if (tmpCandidates.Count >= 64) break;
            }

            IntVec3 origin = shooter.Position;
            tmpCandidates.Sort((a, b) =>
                (a - origin).LengthHorizontalSquared.CompareTo((b - origin).LengthHorizontalSquared));

            int checkedReach = 0;
            for (int i = 0; i < tmpCandidates.Count; i++)
            {
                if (++checkedReach > 16) break;
                if (shooter.CanReach(tmpCandidates[i], PathEndMode.OnCell, Danger.Deadly))
                {
                    return tmpCandidates[i];
                }
            }
            return IntVec3.Invalid;
        }

        public static bool TryStartCrossGapAttack(Pawn pawn, Thing target)
        {
            try
            {
                if (!Enabled || pawn == null || !pawn.Spawned || target == null) return false;
                if (!pawn.Drafted || pawn.Downed || pawn.Dead) return false;
                return StartAttackJob(pawn, target, playerForced: true, allowReposition: true);
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Start cross-level attack failed: " + e.Message, 0x5B71006);
                return false;
            }
        }

        public static bool TryStartAutoEngage(Pawn pawn, Thing target, bool allowReposition)
        {
            try
            {
                if (!Enabled || pawn == null || !pawn.Spawned || target == null) return false;
                if (pawn.Downed || pawn.Dead || pawn.jobs == null) return false;
                return StartAttackJob(pawn, target, playerForced: false, allowReposition);
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Auto cross-level engage failed: " + e.Message, 0x5B71008);
                return false;
            }
        }

        /// <summary>
        /// Convert a vanilla AttackStatic order aimed at another floor into gap fire.
        /// </summary>
        public static bool TryConvertOrderedAttack(Pawn pawn, Job job, out bool started)
        {
            started = false;
            if (!Enabled || pawn == null || job == null) return false;
            if (job.def != JobDefOf.AttackStatic) return false;
            Thing target = job.targetA.Thing;
            if (target == null || target.MapHeld == null || target.MapHeld == pawn.Map) return false;
            if (GetRangedVerb(pawn) == null) return false;
            if (!AreCrossGapPaired(pawn.Map, target.MapHeld, out _, out _)) return false;

            // Only claim the order when gap fire (possibly after a short walk) succeeds;
            // otherwise CrossLevelOrderedJobs can still route a stair commute.
            started = StartAttackJob(pawn, target, playerForced: job.playerForced, allowReposition: true);
            return started;
        }

        public static bool IsAttackTarget(Thing t, Pawn pawn)
        {
            if (t == null || t == pawn || !t.Spawned || !t.def.destroyable) return false;
            if (t.def.noRightClickDraftAttack && t.HostileTo(Faction.OfPlayer)) return false;
            if (t.HostileTo(Faction.OfPlayer)) return true;
            return t is Pawn p && WildManUtility.NonHumanlikeOrWildMan(p);
        }

        public static Thing FindAttackTargetAt(Map map, Vector3 clickPos, List<Pawn> forPawns)
        {
            if (map == null || forPawns == null || forPawns.Count == 0) return null;
            IntVec3 cell = clickPos.ToIntVec3();
            if (!cell.InBounds(map)) return null;

            Thing bestNonPawn = null;
            for (int r = 0; r <= 1; r++)
            {
                foreach (IntVec3 c in GenRadial.RadialCellsAround(cell, r, useCenter: true))
                {
                    if (!c.InBounds(map)) continue;
                    List<Thing> things = map.thingGrid.ThingsListAtFast(c);
                    for (int i = 0; i < things.Count; i++)
                    {
                        Thing t = things[i];
                        bool ok = false;
                        for (int p = 0; p < forPawns.Count; p++)
                        {
                            if (IsAttackTarget(t, forPawns[p])) { ok = true; break; }
                        }
                        if (!ok) continue;
                        if (t is Pawn) return t;
                        if (bestNonPawn == null) bestNonPawn = t;
                    }
                }
                if (bestNonPawn != null) return bestNonPawn;
            }
            return bestNonPawn;
        }

        private static bool StartAttackJob(Pawn pawn, Thing target, bool playerForced, bool allowReposition)
        {
            Verb verb = GetRangedVerb(pawn);
            if (verb == null) return false;

            IntVec3 stand = allowReposition
                ? FindFiringCell(pawn, target, verb)
                : (CanFireFrom(pawn.Map, pawn.Position, target, verb, out _)
                    ? pawn.Position
                    : IntVec3.Invalid);
            if (!stand.IsValid) return false;

            Job job = JobMaker.MakeJob(StrataDefOf.Strata_CrossLevelAttack);
            job.SetTarget(TargetIndex.B, stand);
            job.playerForced = playerForced;
            if (PendingTargets.Count > 64) PendingTargets.Clear();
            PendingTargets[pawn.thingIDNumber] = target;

            if (playerForced)
            {
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
            else
            {
                pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            }
            return true;
        }
    }
}

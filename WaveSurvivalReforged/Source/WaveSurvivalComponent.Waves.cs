using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    public partial class WaveSurvivalComponent
    {
        private const float MinRaidPoints = 35f;

        private WavePool PoolFor(int wave)
        {
            WaveSurvivalConfigDef cfg = Config;
            int specialInterval = run?.specialWaveInterval ?? cfg.specialWaveInterval;
            if (cfg.bossWaveInterval > 0 && wave % cfg.bossWaveInterval == 0)
                return WavePool.Boss;
            if (wave % specialInterval == 0 && wave >= cfg.specialWaveMinWave)
                return WavePool.Special;
            return WavePool.Normal;
        }

        // Pre-rolls the next wave so the alert and HUD can name it. Without a home map it
        // stays unrolled and is tried again on the next check.
        private void RollNextPlan()
        {
            Map map = TargetMap();
            nextPlan = map == null ? null : MakePlan(map, waveNum, PoolFor(waveNum));
        }

        private void RunWave(int now)
        {
            WaveSurvivalConfigDef cfg = Config;
            Map map = TargetMap();
            if (map == null)
            {
                Postpone(now, "WSR_WavePostponedNoHome");
                return;
            }

            tracker.Finish(map, stats);
            tracker.BeginGather(map, waveNum);

            WavePlan plan = Validated(nextPlan, map) ?? MakePlan(map, waveNum, PoolFor(waveNum));
            bool ok = plan != null && Execute(plan, map);
            if (!ok && plan != null && plan.type.pool != WavePool.Normal)
            {
                // The planned special wave failed to spawn: a plain raid at full points instead.
                plan = MakePlan(map, waveNum, WavePool.Normal, allowSplit: false);
                ok = plan != null && Execute(plan, map);
            }
            if (!ok)
            {
                plan = ManhunterPlan(map);
                ok = plan != null && Execute(plan, map);
            }
            if (!ok)
            {
                tracker.Cancel();
                Postpone(now, "WSR_WavePostponed");
                return;
            }

            tracker.Started(plan, now);
            stats.Notify_WaveStarted(waveNum);
#if RIMWORLD_1_7
            WaveVignette.Notify_WaveArrived(plan.type.pool);
#endif
            Messages.Message(ArrivedKey(plan.type.pool).Translate(waveNum, plan.Describe()), MessageTypeDefOf.ThreatBig);
            float days = run.daysBetweenWaves + (plan.type.pool != WavePool.Normal ? cfg.breatherDays : 0f);
            ticksNextWave = now + Mathf.RoundToInt(days * GenDate.TicksPerDay);
            postponedWave = 0;
            waveNum++;
            RollNextPlan();
        }

        private void Postpone(int now, string key)
        {
            ticksNextWave = now + Mathf.RoundToInt(Config.postponeHours * GenDate.TicksPerHour);
            nextPlan = null;
            if (postponedWave == waveNum)
                return;
            postponedWave = waveNum;
            int delay = ticksNextWave - now;
            Messages.Message(key.Translate(waveNum, delay.ToStringTicksToPeriod(allowSeconds: false)),
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        private static string ArrivedKey(WavePool pool)
        {
            switch (pool)
            {
                case WavePool.Boss:
                    return "WSR_BossWaveArrived";
                case WavePool.Special:
                    return "WSR_SpecialWaveArrived";
                default:
                    return "WSR_WaveArrived";
            }
        }

        // The pre-rolled plan if it can still run as announced; a raid whose faction can no
        // longer attack keeps its type and points with another faction. Null: roll again.
        private static WavePlan Validated(WavePlan plan, Map map)
        {
            if (plan == null || !plan.IsUsable)
                return null;
            if (plan.type.kind == WaveKind.Incident || CanAttack(plan.faction, plan.type, map, plan.SinglePoints))
                return plan;
            if (!TryFindFaction(plan.type, map, plan.points, out Faction other))
                return null;
            plan.faction = other;
            plan.split = false;
            return plan;
        }

        private float BasePoints(Map map, int wave)
        {
            WaveSurvivalConfigDef cfg = Config;
            float points = cfg.pointsByWave.Evaluate(wave) * ThreatScale;
            if (cfg.wealthBlend > 0f)
                points = Mathf.Lerp(points, StorytellerUtility.DefaultThreatPointsNow(map), cfg.wealthBlend);
            return points;
        }

        private float PointsFor(WaveTypeDef type, Map map, int wave)
        {
            float points = (BasePoints(map, wave) * type.pointsFactor + type.bonusPoints * ThreatScale) * run.pointsFactor;
            return Mathf.Max(points, MinRaidPoints);
        }

        // Picks a wave type from the pool by weight, then a faction able to carry it out.
        // A special or boss pool with nothing able to run falls back to the normal pool.
        private WavePlan MakePlan(Map map, int wave, WavePool pool, bool allowSplit = true)
        {
            var candidates = new List<WaveTypeDef>(WaveTypeDef.InPool(pool, wave));
            while (candidates.TryRandomElementByWeight(t => t.weight, out WaveTypeDef type))
            {
                candidates.Remove(type);
                float points = PointsFor(type, map, wave);
                if (type.kind == WaveKind.Incident)
                    return new WavePlan(type, null, points, false);
                if (!TryFindFaction(type, map, points, out Faction faction))
                    continue;
                float half = points * 0.5f;
                bool split = allowSplit && type.canSplit && Rand.Chance(Config.doubleRaidChance)
                    && half >= MinRaidPoints
                    && CanAttack(faction, type, map, half);
                return new WavePlan(type, faction, points, split);
            }
            return pool == WavePool.Normal ? null : MakePlan(map, wave, WavePool.Normal, allowSplit);
        }

        private WavePlan ManhunterPlan(Map map)
        {
            WaveTypeDef type = DefDatabase<WaveTypeDef>.AllDefsListForReading.FirstOrDefault(t =>
                t.kind == WaveKind.Incident && t.Available && t.Incident == IncidentDefOf.ManhunterPack);
            return type == null ? null : new WavePlan(type, null, PointsFor(type, map, waveNum), false);
        }

        private static bool TryFindFaction(WaveTypeDef type, Map map, float points, out Faction faction)
        {
            return Find.FactionManager.AllFactionsListForReading
                .Where(f => CanAttack(f, type, map, points))
                .TryRandomElement(out faction);
        }

        internal static bool CanAttack(Faction f, WaveTypeDef type, Map map, float points)
        {
            if (f == null || f.IsPlayer || f.defeated || f.temporary || !f.HostileTo(Faction.OfPlayer))
                return false;
            List<FactionDef> only = type.FactionDefs;
            if (only != null ? !only.Contains(f.def) : f.Hidden)
                return false;
            if (f.def.pawnGroupMakers == null || !f.def.pawnGroupMakers.Any(m => m.kindDef == PawnGroupKindDefOf.Combat))
                return false;
            if (points < f.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat))
                return false;
            return type.Strategy.Worker.CanUseWith(
                new IncidentParms { target = map, points = points, faction = f }, PawnGroupKindDefOf.Combat);
        }

        private static bool Execute(WavePlan plan, Map map)
        {
            if (plan.type.kind == WaveKind.Incident)
                return Fire(map, plan.type.Incident, plan.points);
            if (!plan.split)
                return Raid(map, plan.points, plan);
            bool first = Raid(map, plan.SinglePoints, plan);
            bool second = Raid(map, plan.SinglePoints, plan);
            return first || second;
        }

        private static bool Raid(Map map, float points, WavePlan plan)
        {
            var parms = new IncidentParms
            {
                target = map,
                points = points,
                faction = plan.faction,
                raidStrategy = plan.type.Strategy,
                raidArrivalMode = plan.type.ArrivalMode,
                forced = true,
            };
            return IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
        }

        private static bool Fire(Map map, IncidentDef incident, float points)
        {
            if (incident == null)
                return false;
            var parms = new IncidentParms { target = map, points = points, forced = true };
            return incident.Worker.TryExecute(parms);
        }
    }
}

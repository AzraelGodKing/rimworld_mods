using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    public class WaveSurvivalComponent : GameComponent
    {
        private const int CheckInterval = 250;
        private const float MinRaidPoints = 35f;

        private bool started;
        private int waveNum = 1;
        private int ticksNextWave = -1;
        private int ticksNextDay = -1;
        private bool dropPending;
        // Not saved: only stops the postponed message repeating for the same wave.
        private int postponedWave;

        private static List<ThingStuffPair> weaponPool;

        public WaveSurvivalComponent(Game game)
        {
        }

        public static WaveSurvivalComponent Instance => Current.Game?.GetComponent<WaveSurvivalComponent>();

        public static bool ModeActive => Find.Storyteller?.def == WSR_DefOf.WSR_WaveSurvival;

        public bool Started => started;

        public int WaveNum => waveNum;

        public int TicksUntilNextWave => ticksNextWave - Find.TickManager.TicksGame;

        public WavePool NextWavePool => PoolFor(waveNum);

        private static WaveSurvivalConfigDef Config => WSR_DefOf.WSR_Config;

        private static float ThreatScale => Find.Storyteller.difficulty.threatScale;

        public override void GameComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now % CheckInterval != 0 || !ModeActive)
                return;
            if (!started)
                Start(now);
            if (now >= ticksNextWave)
            {
                DeepProfiler.Start("WaveSurvivalReforged.RunWave");
                try
                {
                    RunWave(now);
                }
                finally
                {
                    DeepProfiler.End();
                }
            }
            DeepProfiler.Start("WaveSurvivalReforged.Supplies");
            try
            {
                if (now >= ticksNextDay)
                    RunDay(now);
                else if (dropPending && TryDropSupplies())
                    dropPending = false;
            }
            finally
            {
                DeepProfiler.End();
            }
        }

        private void Start(int now)
        {
            WaveSurvivalConfigDef cfg = Config;
            int hourOffset = cfg.raidHourOffset * GenDate.TicksPerHour;
            ticksNextWave = now + Mathf.RoundToInt(cfg.daysBeforeFirstWave * GenDate.TicksPerDay) + hourOffset;
            ticksNextDay = now + GenDate.TicksPerHour;
            waveNum = 1;
            started = true;
        }

        private static WavePool PoolFor(int wave)
        {
            WaveSurvivalConfigDef cfg = Config;
            if (cfg.bossWaveInterval > 0 && wave % cfg.bossWaveInterval == 0)
                return WavePool.Boss;
            if (wave % cfg.specialWaveInterval == 0 && wave >= cfg.specialWaveMinWave)
                return WavePool.Special;
            return WavePool.Normal;
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

            WavePlan plan = MakePlan(map, waveNum, PoolFor(waveNum));
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
                Postpone(now, "WSR_WavePostponed");
                return;
            }

            Messages.Message(ArrivedKey(plan.type.pool).Translate(waveNum, Describe(plan)), MessageTypeDefOf.ThreatBig);
            float days = cfg.daysBetweenWaves + (plan.type.pool != WavePool.Normal ? cfg.breatherDays : 0f);
            ticksNextWave = now + Mathf.RoundToInt(days * GenDate.TicksPerDay);
            postponedWave = 0;
            waveNum++;
        }

        private void Postpone(int now, string key)
        {
            ticksNextWave = now + Mathf.RoundToInt(Config.postponeHours * GenDate.TicksPerHour);
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

        internal static string Describe(WavePlan plan)
        {
            string label = plan.type.label;
            return plan.faction == null ? label : (string)"WSR_WaveWithFaction".Translate(label, plan.faction.Name);
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
            float points = BasePoints(map, wave) * type.pointsFactor + type.bonusPoints * ThreatScale;
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
                    && half >= faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat);
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
            if (f.IsPlayer || f.defeated || f.temporary || !f.HostileTo(Faction.OfPlayer))
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
            bool first = Raid(map, plan.points * 0.5f, plan);
            bool second = Raid(map, plan.points * 0.5f, plan);
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

        private void RunDay(int now)
        {
            ticksNextDay = now + GenDate.TicksPerDay;
            Map map = TargetMap();
            if (map == null)
                return;
            dropPending = !TryDropSupplies();
            MaybeWanderer(map);
            if (Rand.Chance(Config.traderChance))
                MaybeTrader(map);
        }

        // Vanilla needs a powered comms console for the trade ship to be any use.
        private static void MaybeTrader(Map map)
        {
            IncidentDef trader = IncidentDefOf.OrbitalTraderArrival;
            var parms = new IncidentParms { target = map, forced = true };
            if (trader.Worker.CanFireNow(parms))
                trader.Worker.TryExecute(parms);
        }

        private bool TryDropSupplies()
        {
            Map map = TargetMap();
            if (map == null || !TryFindBeacon(map, out IntVec3 pos))
                return false;

            var things = new List<Thing>();
            int colonists = Mathf.Max(1, map.mapPawns.FreeColonistsSpawnedCount);
            float food = map.resourceCounter.TotalHumanEdibleNutrition;
            if (food < 3f * colonists || food < 20f)
                AddStacks(things, ThingDefOf.MealSurvivalPack, Rand.RangeInclusive(colonists * 2, colonists * 4));

            foreach (SupplyDrop drop in Config.supplyDrops)
            {
                if (drop.thing == null || !Rand.Chance(drop.chance))
                    continue;
                if (map.resourceCounter.GetCount(drop.thing) >= drop.maxStockpile)
                    continue;
                AddStacks(things, drop.thing, drop.count.RandomInRange);
            }

            Thing weapon = MaybeWeapon();
            if (weapon != null)
                things.Add(weapon);

            if (things.Count > 0)
            {
                DropPodUtility.DropThingsNear(pos, map, things, forbid: false);
                Messages.Message("WSR_SupplyDropArrived".Translate(), new TargetInfo(pos, map),
                    MessageTypeDefOf.PositiveEvent);
            }
            return true;
        }

        private static void AddStacks(List<Thing> things, ThingDef def, int count)
        {
            while (count > 0)
            {
                Thing stack = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
                stack.stackCount = Mathf.Min(count, def.stackLimit);
                count -= stack.stackCount;
                things.Add(stack);
            }
        }

        private Thing MaybeWeapon()
        {
            WaveSurvivalConfigDef cfg = Config;
            if (!Rand.Chance(cfg.weaponDropChance))
                return null;
            float budget = waveNum * cfg.weaponBudgetPerWave + cfg.weaponBudget.RandomInRange;
            if (Rand.Chance(0.25f))
                budget += Rand.Range(100f, 300f);
            if (Rand.Chance(0.1f))
                budget += Rand.Range(300f, 1000f);
            if (!WeaponPool.Where(w => w.Price <= budget)
                    .TryRandomElementByWeight(w => w.Commonality * w.Price, out ThingStuffPair pick))
                return null;
            return ThingMaker.MakeThing(pick.thing, pick.stuff);
        }

        private static List<ThingStuffPair> WeaponPool =>
            weaponPool ??= ThingStuffPair.AllWith(td =>
                td.equipmentType == EquipmentType.Primary
                && !td.weaponTags.NullOrEmpty()
                && !td.destroyOnDrop
                && td.tradeability.TraderCanSell()
                && !td.HasComp<CompBladelinkWeapon>()
                && !td.HasComp<CompUniqueWeapon>());

        private static void MaybeWanderer(Map map)
        {
            int colonists = map.mapPawns.FreeColonistsSpawnedCount;
            float chance = colonists <= 0 ? 0f
                : colonists == 1 ? 0.15f
                : colonists <= 3 ? 0.10f
                : colonists <= 9 ? 0.01f
                : 0f;
            if (Rand.Chance(chance))
                Fire(map, IncidentDefOf.WandererJoin, 0f);
        }

        // The home map with a trade beacon, else any home map.
        public static Map TargetMap()
        {
            Map fallback = null;
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome)
                    continue;
                if (TryFindBeacon(map, out _))
                    return map;
                fallback ??= map;
            }
            return fallback;
        }

        public static bool TryFindBeacon(Map map, out IntVec3 pos)
        {
            Building_OrbitalTradeBeacon beacon = map.listerBuildings
                .AllBuildingsColonistOfClass<Building_OrbitalTradeBeacon>()
                .FirstOrDefault(b => b.Spawned);
            pos = beacon?.Position ?? IntVec3.Invalid;
            return beacon != null;
        }

        public static bool AnyHomeHasBeacon()
        {
            return Find.Maps.Any(m => m.IsPlayerHome && TryFindBeacon(m, out _));
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref started, "started");
            Scribe_Values.Look(ref waveNum, "waveNum", 1);
            Scribe_Values.Look(ref ticksNextWave, "ticksNextWave", -1);
            Scribe_Values.Look(ref ticksNextDay, "ticksNextDay", -1);
            Scribe_Values.Look(ref dropPending, "dropPending");
        }
    }
}

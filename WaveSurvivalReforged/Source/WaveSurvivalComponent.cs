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

        private static List<ThingStuffPair> weaponPool;

        public WaveSurvivalComponent(Game game)
        {
        }

        public static WaveSurvivalComponent Instance => Current.Game?.GetComponent<WaveSurvivalComponent>();

        public static bool ModeActive => Find.Storyteller?.def == WSR_DefOf.WSR_WaveSurvival;

        public bool Started => started;

        public int WaveNum => waveNum;

        public int TicksUntilNextWave => ticksNextWave - Find.TickManager.TicksGame;

        public bool NextWaveIsSpecial => IsSpecial(waveNum);

        private static WaveSurvivalConfigDef Config => WSR_DefOf.WSR_Config;

        public override void GameComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now % CheckInterval != 0 || !ModeActive)
                return;
            if (!started)
                Start(now);
            if (now >= ticksNextWave)
                RunWave(now);
            if (now >= ticksNextDay)
                RunDay(now);
            else if (dropPending && TryDropSupplies())
                dropPending = false;
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

        private bool IsSpecial(int wave)
        {
            WaveSurvivalConfigDef cfg = Config;
            return wave % cfg.specialWaveInterval == 0 && wave >= cfg.specialWaveMinWave;
        }

        private void RunWave(int now)
        {
            ticksNextWave = now + Mathf.RoundToInt(Config.daysBetweenWaves * GenDate.TicksPerDay);
            Map map = TargetMap();
            if (map == null)
                return;

            bool special = IsSpecial(waveNum);
            Messages.Message(
                (special ? "WSR_SpecialWaveArrived" : "WSR_WaveArrived").Translate(waveNum),
                MessageTypeDefOf.ThreatBig);
            if (!special || !TrySpecialWave(map))
                StandardWave(map);
            waveNum++;
        }

        private float WavePoints(float scale = 1f)
        {
            WaveSurvivalConfigDef cfg = Config;
            float threatScale = Find.Storyteller.difficulty.threatScale;
            float points = cfg.startingThreatPoints + (waveNum - 1) * cfg.threatPointIncreasePerWave * threatScale;
            return Mathf.Max(points * scale, MinRaidPoints);
        }

        private void StandardWave(Map map)
        {
            if (Rand.Chance(Config.doubleRaidChance))
            {
                Raid(map, WavePoints(0.5f), RaidStrategyDefOf.ImmediateAttack);
                Raid(map, WavePoints(0.5f), RaidStrategyDefOf.ImmediateAttack);
            }
            else
            {
                Raid(map, WavePoints(), RaidStrategyDefOf.ImmediateAttack);
            }
        }

        private bool TrySpecialWave(Map map)
        {
            float points = WavePoints();
            switch (Rand.RangeInclusive(0, 3))
            {
                case 0:
                    return StrategyRaid(map, points + Config.siegeBonusPoints, "Siege");
                case 1:
                    return Fire(map, DefDatabase<IncidentDef>.GetNamedSilentFail("AnimalInsanityMass"), points);
                case 2:
                    return Fire(map, IncidentDefOf.ManhunterPack, points);
                default:
                    return StrategyRaid(map, points + Config.siegeBonusPoints, "ImmediateAttackSappers");
            }
        }

        private static bool Raid(Map map, float points, RaidStrategyDef strategy, Faction faction = null)
        {
            var parms = new IncidentParms
            {
                target = map,
                points = points,
                raidStrategy = strategy,
                faction = faction,
                forced = true,
            };
            return IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
        }

        // Sieges and sappers only work for some factions, so pick one that can do it.
        private static bool StrategyRaid(Map map, float points, string strategyName)
        {
            RaidStrategyDef strategy = DefDatabase<RaidStrategyDef>.GetNamedSilentFail(strategyName);
            if (strategy == null)
                return false;
            IEnumerable<Faction> able = Find.FactionManager.AllFactions.Where(f =>
                !f.IsPlayer && !f.defeated && !f.Hidden && f.HostileTo(Faction.OfPlayer)
                && points >= f.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat)
                && strategy.Worker.CanUseWith(
                    new IncidentParms { target = map, points = points, faction = f },
                    PawnGroupKindDefOf.Combat));
            if (!able.TryRandomElement(out Faction faction))
                return false;
            return Raid(map, points, strategy, faction);
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
                Fire(map, IncidentDefOf.OrbitalTraderArrival, 0f);
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
                && !(td.comps?.Any(c => c.compClass != null
                    && (c.compClass.Name == "CompBladelinkWeapon" || c.compClass.Name == "CompUniqueWeapon")) ?? false));

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

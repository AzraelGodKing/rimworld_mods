using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    public partial class WaveSurvivalComponent
    {
        // Loose items this close to the beacon count toward maxStockpile, so unhauled drops stop more drops.
        private const float BeaconPileRadius = 12f;
        // A fallback drop near the colony carries this share of each stack and no weapon.
        private const float FallbackShare = 0.5f;

        private static List<ThingStuffPair> weaponPool;

        // Days in a row the daily drop found no beacon. Saved.
        private int missedDropDays;

        private void RunDay(int now)
        {
            ticksNextDay = now + GenDate.TicksPerDay;
            Map map = TargetMap();
            if (map == null)
                return;
            if (TryDropSupplies(fallback: false))
            {
                dropPending = false;
                missedDropDays = 0;
            }
            else if (++missedDropDays >= Config.noBeaconFallbackDays && Config.noBeaconFallbackDays > 0)
            {
                // Still no beacon after a few days: a smaller drop near the colony instead.
                TryDropSupplies(fallback: true);
                dropPending = false;
                missedDropDays = 0;
            }
            else
            {
                dropPending = true;
            }
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

        private bool TryDropSupplies(bool fallback)
        {
            Map map;
            IntVec3 pos;
            if (fallback)
            {
                map = TargetMap();
                if (map == null)
                    return false;
                pos = DropCellFinder.TradeDropSpot(map);
            }
            else
            {
                Building found = Beacon();
                if (found == null)
                    return false;
                map = found.Map;
                pos = found.Position;
            }

            float share = fallback ? FallbackShare : 1f;
            var things = new List<Thing>();
            int colonists = Mathf.Max(1, map.mapPawns.FreeColonistsSpawnedCount);
            float food = map.resourceCounter.TotalHumanEdibleNutrition;
            if (food < 3f * colonists || food < 20f)
                AddStacks(things, ThingDefOf.MealSurvivalPack, Rand.RangeInclusive(colonists * 2, colonists * 4));

            foreach (SupplyDrop drop in Config.supplyDrops)
            {
                if (drop.thing == null || !Rand.Chance(Mathf.Min(1f, drop.chance * run.supplyFactor)))
                    continue;
                if (map.resourceCounter.GetCount(drop.thing) + LooseNear(map, pos, drop.thing) >= drop.maxStockpile)
                    continue;
                AddStacks(things, drop.thing, Mathf.Max(1, Mathf.RoundToInt(drop.count.RandomInRange * share)));
            }

            Thing weapon = fallback ? null : MaybeWeapon();
            if (weapon != null)
                things.Add(weapon);

            if (things.Count > 0)
            {
                DropPodUtility.DropThingsNear(pos, map, things, forbid: false);
                Messages.Message((fallback ? "WSR_SupplyDropFallback" : "WSR_SupplyDropArrived").Translate(),
                    new TargetInfo(pos, map), MessageTypeDefOf.PositiveEvent);
                stats.dropsReceived++;
            }
            return true;
        }

        // Items of this def lying outside storage near the drop spot (resourceCounter only counts stored ones).
        private static int LooseNear(Map map, IntVec3 pos, ThingDef def)
        {
            int count = 0;
            float radiusSquared = BeaconPileRadius * BeaconPileRadius;
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t.Spawned && !t.IsInAnyStorage() && (t.Position - pos).LengthHorizontalSquared <= radiusSquared)
                    count += t.stackCount;
            }
            return count;
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
            // Price still tilts the pick toward better guns, but by its square root, so the
            // priciest affordable weapon no longer wins almost every drop.
            if (!WeaponPool.Where(w => w.Price <= budget)
                    .TryRandomElementByWeight(w => w.Commonality * Mathf.Sqrt(w.Price), out ThingStuffPair pick))
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

        private void MaybeWanderer(Map map)
        {
            int colonists = map.mapPawns.FreeColonistsSpawnedCount;
            float chance = colonists <= 0 ? 0f
                : colonists == 1 ? 0.15f
                : colonists <= 3 ? 0.10f
                : colonists <= 9 ? 0.01f
                : 0f;
            if (Rand.Chance(Mathf.Min(1f, chance * run.wandererFactor)))
                Fire(map, IncidentDefOf.WandererJoin, 0f);
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NanoHiveReforged
{
    // The repair cycle (TickRare, every 250 ticks). Allocation-free after the
    // first cycle: the radial cells are cached per hive and the target lists
    // are static scratch buffers (ticks are single-threaded).
    public partial class Building_NanoHiveReforged
    {
        private static readonly List<Thing> scratchTargets = new List<Thing>();
        private static readonly HashSet<Thing> scratchSeen = new HashSet<Thing>();
        private static bool sortBuildingsFirst;

        private List<IntVec3> cachedCells;
        private float cachedCellsRange = -1f;

        // Results of the last cycle, for the inspect pane and the shimmer.
        // Not saved: the next cycle rebuilds them.
        private int lastTargetsDamaged;
        private int lastHpRepaired;

        public bool Working { get; private set; }

        public int LastTargetsDamaged => lastTargetsDamaged;

        public int LastHpRepaired => lastHpRepaired;

        public int Budget => Config.repairPerTarget * NanoHiveProps.For(def).budgetTargets;

        public override void TickRare()
        {
            base.TickRare();
            ProfilerThreadCheck.BeginSample("NanoHiveReforged.TickRare");
            try
            {
                RepairCycle();
            }
            finally
            {
                ProfilerThreadCheck.EndSample();
            }
        }

        private void RepairCycle()
        {
            if (Map == null || Faction == null || (compPowerTrader != null && !compPowerTrader.PowerOn))
            {
                Working = false;
                lastTargetsDamaged = 0;
                lastHpRepaired = 0;
                return;
            }

            List<Thing> targets = scratchTargets;
            targets.Clear();
            scratchSeen.Clear();
            try
            {
                CollectTargets(targets);
                lastTargetsDamaged = targets.Count;
                lastHpRepaired = Repair(targets);
            }
            finally
            {
                targets.Clear();
                scratchSeen.Clear();
            }

            Working = lastHpRepaired > 0;
            SetPowerDraw(Working);
        }

        private void SetPowerDraw(bool working)
        {
            if (compPowerTrader == null)
            {
                return;
            }
            float watts = working ? compPowerTrader.Props.PowerConsumption : NanoHiveProps.For(def).idlePowerConsumption;
            compPowerTrader.PowerOutput = -watts;
        }

        private void CollectTargets(List<Thing> targets)
        {
            HiveConfig cfg = Config;
            Map map = Map;

            AddIfDamaged(this, targets);

            if (cfg.repairBuildings || cfg.repairItems)
            {
                List<IntVec3> cells = CellsInRange();
                for (int c = 0; c < cells.Count; c++)
                {
                    List<Thing> things = map.thingGrid.ThingsListAtFast(cells[c]);
                    for (int i = 0; i < things.Count; i++)
                    {
                        Thing thing = things[i];
                        if (thing is Pawn || thing is Corpse || !thing.def.useHitPoints)
                        {
                            continue;
                        }
                        if (thing is Building)
                        {
                            if (cfg.repairBuildings && thing.Faction == Faction)
                            {
                                AddIfDamaged(thing, targets);
                            }
                        }
                        else if (cfg.repairItems && thing.def.category == ThingCategory.Item && ItemAllowed(thing, cfg))
                        {
                            AddIfDamaged(thing, targets);
                        }
                    }
                }
            }

            if (cfg.repairGear || cfg.repairInventory)
            {
                float rangeSq = cfg.range * cfg.range;
                List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(Faction);
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn.IsPrisoner || pawn.Position.DistanceToSquared(Position) > rangeSq)
                    {
                        continue;
                    }
                    AddPawnThings(pawn, cfg, targets);
                }
            }
        }

        private bool ItemAllowed(Thing thing, HiveConfig cfg)
        {
            if (!thing.Position.Roofed(Map))
            {
                return false;
            }
            return !cfg.onlyStoredItems || (!thing.IsForbidden(Faction) && thing.IsInAnyStorage());
        }

        private static void AddPawnThings(Pawn pawn, HiveConfig cfg, List<Thing> targets)
        {
            if (cfg.repairGear && pawn.equipment != null)
            {
                List<ThingWithComps> equipment = pawn.equipment.AllEquipmentListForReading;
                for (int i = 0; i < equipment.Count; i++)
                {
                    AddIfDamaged(equipment[i], targets);
                }
            }
            if (cfg.repairGear && pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    AddIfDamaged(worn[i], targets);
                }
            }
            if (cfg.repairInventory && pawn.inventory != null)
            {
                ThingOwner<Thing> inner = pawn.inventory.innerContainer;
                for (int i = 0; i < inner.Count; i++)
                {
                    Thing thing = inner[i];
                    if (thing.def.category == ThingCategory.Item)
                    {
                        AddIfDamaged(thing, targets);
                    }
                }
            }
        }

        private static void AddIfDamaged(Thing thing, List<Thing> targets)
        {
            if (thing == null || thing.Destroyed || thing.def == null || !thing.def.useHitPoints
                || thing.HitPoints >= thing.MaxHitPoints)
            {
                return;
            }
            if (scratchSeen.Add(thing))
            {
                targets.Add(thing);
            }
        }

        // Lowest HP% first (buildings first when asked), each target gets up to
        // the rate, until the hive's budget for this cycle is spent.
        private int Repair(List<Thing> targets)
        {
            if (targets.Count == 0)
            {
                return 0;
            }
            HiveConfig cfg = Config;
            sortBuildingsFirst = cfg.buildingsFirst;
            targets.Sort(CompareTargets);

            int budget = Budget;
            int repaired = 0;
            for (int i = 0; i < targets.Count && budget > 0; i++)
            {
                Thing thing = targets[i];
                int amount = System.Math.Min(System.Math.Min(cfg.repairPerTarget, thing.MaxHitPoints - thing.HitPoints), budget);
                if (amount <= 0)
                {
                    continue;
                }
                // Building.HitPoints refreshes the damage overlay itself.
                thing.HitPoints += amount;
                budget -= amount;
                repaired += amount;
                if (thing is Building building && building.Spawned && building.Map == Map)
                {
                    // Like vanilla repair: drops the building off the repair list once full.
                    Map.listerBuildingsRepairable.Notify_BuildingRepaired(building);
                }
            }
            return repaired;
        }

        private static readonly System.Comparison<Thing> CompareTargets = (a, b) =>
        {
            if (sortBuildingsFirst)
            {
                bool ab = a is Building;
                bool bb = b is Building;
                if (ab != bb)
                {
                    return ab ? -1 : 1;
                }
            }
            int byHp = Fraction(a).CompareTo(Fraction(b));
            // thingIDNumber keeps the order the same on every Multiplayer client.
            return byHp != 0 ? byHp : a.thingIDNumber.CompareTo(b.thingIDNumber);
        };

        private static float Fraction(Thing t)
        {
            return (float)t.HitPoints / t.MaxHitPoints;
        }

        private List<IntVec3> CellsInRange()
        {
            float range = Config.range;
            if (cachedCells != null && cachedCellsRange == range)
            {
                return cachedCells;
            }
            cachedCells ??= new List<IntVec3>();
            cachedCells.Clear();
            cachedCellsRange = range;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, range, true))
            {
                if (cell.InBounds(Map))
                {
                    cachedCells.Add(cell);
                }
            }
            return cachedCells;
        }
    }
}

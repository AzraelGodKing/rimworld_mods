using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_OreHoist : CompProperties
    {
        public int transferIntervalTicks = 250;

        public float pickupRadius = 8f;

        public int maxItemsPerTransfer = 5;

        // Ore skip: chunks and slag only. Dumbwaiter / freight lift: any
        // haulable stack except corpses.
        public bool chunksOnly = true;

        public CompProperties_OreHoist()
        {
            compClass = typeof(CompOreHoist);
        }
    }

    public class CompOreHoist : ThingComp
    {
        public CompProperties_OreHoist Props => (CompProperties_OreHoist)props;

        private int TransferInterval
        {
            get
            {
                int ticks = Props.transferIntervalTicks;
                if (parent.def.defName != "Strata_FreightLift")
                {
                    return ticks;
                }
                float brown = StormproofBrownout.For(parent);
                if (brown <= 0.01f)
                {
                    return ticks;
                }
                return (int)(ticks * (1f + brown * 1.5f));
            }
        }

        public bool Active
        {
            get
            {
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                if (power == null)
                {
                    return true;
                }
                return power.PowerOn;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(TransferInterval) || !Active)
            {
                return;
            }
            if (parent is not Building_OreHoist hoist)
            {
                return;
            }
            Building_OreHoist partner = hoist.Partner;
            if (partner == null || !partner.Spawned)
            {
                return;
            }
            TryTransfer(hoist, partner);
        }

        private void TryTransfer(Building_OreHoist from, Building_OreHoist to)
        {
            Map fromMap = from.Map;
            Map toMap = to.Map;
            List<Thing> candidates = fromMap.listerThings.AllThings;
            int moved = 0;
            for (int i = 0; i < candidates.Count && moved < Props.maxItemsPerTransfer; i++)
            {
                Thing thing = candidates[i];
                if (!IsHoistable(thing) || !thing.Position.InHorDistOf(from.Position, Props.pickupRadius))
                {
                    continue;
                }
                IntVec3 drop = FindDropCell(toMap, to.Position);
                if (!drop.IsValid)
                {
                    break;
                }
                Thing payload = thing.stackCount > 1 ? thing.SplitOff(1) : thing;
                if (payload.Spawned)
                {
                    payload.DeSpawn();
                }
                if (GenPlace.TryPlaceThing(payload, drop, toMap, ThingPlaceMode.Near))
                {
                    moved++;
                }
                else if (!payload.Destroyed && !payload.Spawned)
                {
                    GenPlace.TryPlaceThing(payload, from.Position, fromMap, ThingPlaceMode.Near);
                }
            }
        }

        private static IntVec3 FindDropCell(Map map, IntVec3 near)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(near, 3f, useCenter: true))
            {
                if (cell.InBounds(map) && cell.Standable(map) && cell.GetFirstItem(map) == null)
                {
                    return cell;
                }
            }
            return IntVec3.Invalid;
        }

        private bool IsHoistable(Thing thing)
        {
            if (thing == null || !thing.def.EverHaulable || thing.def.category != ThingCategory.Item)
            {
                return false;
            }
            if (thing is Corpse)
            {
                return false;
            }
            if (thing.def.IsWithinCategory(ThingCategoryDefOf.Chunks))
            {
                return true;
            }
            if (thing.def.defName.Contains("Slag") || thing.def.defName.Contains("Chunk"))
            {
                return true;
            }
            return !Props.chunksOnly;
        }

        public override string CompInspectStringExtra()
        {
            if (parent is not Building_OreHoist hoist)
            {
                return null;
            }
            bool waiter = parent.def.defName == "Strata_Dumbwaiter";
            if (hoist.Partner == null)
            {
                return waiter
                    ? "Strata_DumbwaiterNeedsPartner".Translate()
                    : "Strata_OreHoistNeedsPartner".Translate();
            }
            if (waiter)
            {
                return "Strata_DumbwaiterHauling".Translate();
            }
            if (parent.def.defName == "Strata_FreightLift")
            {
                return Active
                    ? "Strata_FreightLiftPowered".Translate()
                    : "Strata_OreHoistNeedsPowerSkip".Translate();
            }
            return Active
                ? "Strata_OreHoistPowered".Translate()
                : "Strata_OreHoistNeedsPowerSkip".Translate();
        }
    }
}

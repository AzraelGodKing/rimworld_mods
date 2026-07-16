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

        public CompProperties_OreHoist()
        {
            compClass = typeof(CompOreHoist);
        }
    }

    public class CompOreHoist : ThingComp
    {
        public CompProperties_OreHoist Props => (CompProperties_OreHoist)props;

        public bool Active
        {
            get
            {
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power != null && power.PowerOn;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Props.transferIntervalTicks) || !Active)
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
                GenPlace.TryPlaceThing(payload, drop, toMap, ThingPlaceMode.Near);
                moved++;
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

        private static bool IsHoistable(Thing thing)
        {
            if (thing == null || !thing.def.EverHaulable || thing.def.category != ThingCategory.Item)
            {
                return false;
            }
            if (thing.def.IsWithinCategory(ThingCategoryDefOf.Chunks))
            {
                return true;
            }
            return thing.def.defName.Contains("Slag") || thing.def.defName.Contains("Chunk");
        }

        public override string CompInspectStringExtra()
        {
            if (parent is not Building_OreHoist hoist)
            {
                return null;
            }
            if (hoist.Partner == null)
            {
                return "Needs a linked partner hoist on another level.";
            }
            return Active
                ? "Powered — shuttling ore chunks to partner level."
                : "Needs power to run the skip.";
        }
    }
}

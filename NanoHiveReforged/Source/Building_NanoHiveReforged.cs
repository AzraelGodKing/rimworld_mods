using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NanoHiveReforged
{
    public class Building_NanoHiveReforged : Building
    {
        private CompPowerTrader compPowerTrader;

        private float EffectiveRange
        {
            get
            {
                if (NanoHiveReforgedMod.Settings != null && NanoHiveReforgedMod.Settings.useRangeOverride)
                {
                    return NanoHiveReforgedMod.Settings.repairRangeOverride;
                }

                return def.specialDisplayRadius;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPowerTrader = GetComp<CompPowerTrader>();
        }

        public override void TickRare()
        {
            base.TickRare();

            if ((compPowerTrader != null && !compPowerTrader.PowerOn) || Faction == null || Map == null)
            {
                return;
            }

            var thingsToRepair = new HashSet<Thing>();
            var pawnsToProcess = new List<Pawn>();

            thingsToRepair.Add(this);

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, EffectiveRange, false))
            {
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                List<Thing> cellThings = Map.thingGrid.ThingsListAt(cell);
                for (int i = 0; i < cellThings.Count; i++)
                {
                    Thing thing = cellThings[i];

                    if (thing is Pawn pawn)
                    {
                        if (pawn.Faction == Faction && !pawn.IsPrisoner)
                        {
                            pawnsToProcess.Add(pawn);
                        }
                        continue;
                    }

                    if (thing is Corpse || thing.def == null || !thing.def.useHitPoints)
                    {
                        continue;
                    }

                    if (thing is Building)
                    {
                        if (thing.Faction == Faction)
                        {
                            thingsToRepair.Add(thing);
                        }
                        continue;
                    }

                    if (thing.def.category == ThingCategory.Item && thing.Position.Roofed(Map))
                    {
                        thingsToRepair.Add(thing);
                    }
                }
            }

            if (NanoHiveReforgedMod.Settings == null || NanoHiveReforgedMod.Settings.allowRepairApparelEquipment || NanoHiveReforgedMod.Settings.allowRepairInventoryItems)
            {
                for (int i = 0; i < pawnsToProcess.Count; i++)
                {
                    foreach (Thing item in GetPawnThingsToRepair(pawnsToProcess[i]))
                    {
                        thingsToRepair.Add(item);
                    }
                }
            }

            int repairAmount = NanoHiveReforgedMod.Settings != null ? NanoHiveReforgedMod.Settings.repairPerTickRare : 1;

            foreach (Thing thing in thingsToRepair)
            {
                if (thing == null || thing.Destroyed || !thing.def.useHitPoints)
                {
                    continue;
                }

                if (thing.HitPoints < thing.MaxHitPoints)
                {
                    thing.HitPoints += repairAmount;
                    if (thing.HitPoints > thing.MaxHitPoints)
                    {
                        thing.HitPoints = thing.MaxHitPoints;
                    }
                }
            }
        }

        private static IEnumerable<Thing> GetPawnThingsToRepair(Pawn pawn)
        {
            bool repairGear = NanoHiveReforgedMod.Settings == null || NanoHiveReforgedMod.Settings.allowRepairApparelEquipment;
            bool repairInventory = NanoHiveReforgedMod.Settings == null || NanoHiveReforgedMod.Settings.allowRepairInventoryItems;

            if (repairGear && pawn.equipment != null)
            {
                foreach (ThingWithComps thing in pawn.equipment.AllEquipmentListForReading)
                {
                    if (thing.def != null && thing.def.useHitPoints)
                    {
                        yield return thing;
                    }
                }
            }

            if (repairGear && pawn.apparel != null)
            {
                foreach (Apparel thing in pawn.apparel.WornApparel)
                {
                    if (thing.def != null && thing.def.useHitPoints)
                    {
                        yield return thing;
                    }
                }
            }

            if (repairInventory && pawn.inventory != null)
            {
                foreach (Thing thing in pawn.inventory.innerContainer)
                {
                    if (thing.def != null && thing.def.category == ThingCategory.Item && thing.def.useHitPoints)
                    {
                        yield return thing;
                    }
                }
            }
        }
    }
}

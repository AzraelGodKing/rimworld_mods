using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanoHiveReforged
{
    public class Building_NanoHiveReforged : Building
    {
        private CompPowerTrader compPowerTrader;

        // Null in a 1.x save and on a fresh build; filled from the colony
        // defaults on spawn.
        private HiveConfig config;

        private static int lastDialogFrame = -1;

        public HiveConfig Config => config ??= NewConfig();

        private float EffectiveRange => Config.range;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPowerTrader = GetComp<CompPowerTrader>();
            config ??= NewConfig();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref config, "nanoHive");
        }

        private static HiveConfig NewConfig()
        {
            GameComponent_NanoHive comp = GameComponent_NanoHive.Instance;
            return comp != null ? comp.Defaults.Copy() : NanoHiveReforgedMod.Settings.NewColonyDefaults();
        }

        // ---- Synced actions (registered with Multiplayer by name) ----

        public void SetToggle(int toggle, bool value)
        {
            Config.Set((HiveToggle)toggle, value);
        }

        public void SetRange(int range)
        {
            Config.range = range;
            Config.Clamp();
        }

        public void SetRepairRate(int rate)
        {
            Config.repairPerTarget = rate;
            Config.Clamp();
        }

        public void MakeColonyDefault()
        {
            GameComponent_NanoHive.Instance?.SetDefaults(Config);
        }

        public void CopyToAllHives()
        {
            if (Map == null)
            {
                return;
            }
            List<Thing> hives = Map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < hives.Count; i++)
            {
                if (hives[i] is Building_NanoHiveReforged other && other != this && other.Faction == Faction)
                {
                    other.Config.CopyFrom(Config);
                }
            }
        }

        // ---- Gizmos ----

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Faction != Faction.OfPlayer)
            {
                yield break;
            }

            foreach (HiveToggle toggle in new[] { HiveToggle.Buildings, HiveToggle.Items, HiveToggle.Gear, HiveToggle.Inventory })
            {
                string key = "NanoHive_Toggle_" + toggle;
                yield return new Command_Toggle
                {
                    defaultLabel = key.Translate(),
                    defaultDesc = (key + "Desc").Translate(),
                    icon = NanoHiveTex.For(toggle),
                    isActive = () => Config.Get(toggle),
                    toggleAction = () => SetToggle((int)toggle, !Config.Get(toggle)),
                };
            }

            yield return new Command_Action
            {
                defaultLabel = "NanoHive_Range".Translate(EffectiveRange.ToString("F0")),
                defaultDesc = "NanoHive_RangeDesc".Translate(),
                icon = NanoHiveTex.Range,
                action = () => OpenSlider(
                    v => "NanoHive_RangeSlider".Translate(v),
                    HiveConfig.MinRange, HiveConfig.MaxRange, Mathf.RoundToInt(EffectiveRange),
                    (hive, v) => hive.SetRange(v)),
            };

            yield return new Command_Action
            {
                defaultLabel = "NanoHive_Rate".Translate(Config.repairPerTarget),
                defaultDesc = "NanoHive_RateDesc".Translate(),
                icon = NanoHiveTex.Rate,
                action = () => OpenSlider(
                    v => "NanoHive_RateSlider".Translate(v),
                    HiveConfig.MinRate, HiveConfig.MaxRate, Config.repairPerTarget,
                    (hive, v) => hive.SetRepairRate(v)),
            };

            yield return new Command_Action
            {
                defaultLabel = "NanoHive_MakeDefault".Translate(),
                defaultDesc = "NanoHive_MakeDefaultDesc".Translate(),
                icon = NanoHiveTex.Default,
                action = MakeColonyDefault,
            };

            yield return new Command_Action
            {
                defaultLabel = "NanoHive_CopyAll".Translate(),
                defaultDesc = "NanoHive_CopyAllDesc".Translate(),
                icon = NanoHiveTex.CopyAll,
                action = CopyToAllHives,
            };
        }

        // One dialog for the whole selection: grouped gizmos each run their
        // action in the same frame, so later ones are skipped.
        private static void OpenSlider(System.Func<int, string> text, int from, int to, int start,
            System.Action<Building_NanoHiveReforged, int> apply)
        {
            if (Time.frameCount == lastDialogFrame)
            {
                return;
            }
            lastDialogFrame = Time.frameCount;
            List<Building_NanoHiveReforged> selected = Find.Selector.SelectedObjects
                .OfType<Building_NanoHiveReforged>()
                .ToList();
            Find.WindowStack.Add(new Dialog_Slider(text, from, to, v =>
            {
                foreach (Building_NanoHiveReforged hive in selected)
                {
                    if (!hive.Destroyed)
                    {
                        apply(hive, v);
                    }
                }
            }, start));
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
                        if (Config.repairBuildings && thing.Faction == Faction)
                        {
                            thingsToRepair.Add(thing);
                        }
                        continue;
                    }

                    if (Config.repairItems && thing.def.category == ThingCategory.Item && thing.Position.Roofed(Map))
                    {
                        thingsToRepair.Add(thing);
                    }
                }
            }

            if (Config.repairGear || Config.repairInventory)
            {
                for (int i = 0; i < pawnsToProcess.Count; i++)
                {
                    foreach (Thing item in GetPawnThingsToRepair(pawnsToProcess[i], Config))
                    {
                        thingsToRepair.Add(item);
                    }
                }
            }

            int repairAmount = Config.repairPerTarget;

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

        private static IEnumerable<Thing> GetPawnThingsToRepair(Pawn pawn, HiveConfig config)
        {
            bool repairGear = config.repairGear;
            bool repairInventory = config.repairInventory;

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

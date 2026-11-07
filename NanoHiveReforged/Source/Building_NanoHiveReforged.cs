using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanoHiveReforged
{
    public partial class Building_NanoHiveReforged : Building
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

        // ---- Selection ring and inspect pane ----

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawRadiusRing(Position, EffectiveRange);
        }

        public override string GetInspectString()
        {
            var sb = new System.Text.StringBuilder(base.GetInspectString());
            void Line(string text)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }
                sb.Append(text);
            }

            Line("NanoHive_Inspect_Settings".Translate(EffectiveRange.ToString("F0"), Config.repairPerTarget, Budget));
            if (compPowerTrader != null && !compPowerTrader.PowerOn)
            {
                Line("NanoHive_Inspect_NoPower".Translate());
            }
            else if (Working)
            {
                Line("NanoHive_Inspect_Repairing".Translate(LastTargetsInRange, LastTargetsDamaged, LastHpRepaired));
            }
            else
            {
                Line("NanoHive_Inspect_Idle".Translate(LastTargetsInRange, NanoHiveProps.For(def).idlePowerConsumption.ToString("F0")));
            }
            return sb.ToString();
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

            foreach (HiveToggle toggle in HiveConfig.AllToggles)
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
    }
}

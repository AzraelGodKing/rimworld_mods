using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_CoreSampler : CompProperties
    {
        public int sampleTicks = 15000;

        public CompProperties_CoreSampler()
        {
            compClass = typeof(CompCoreSampler);
        }
    }

    public class CompCoreSampler : ThingComp
    {
        private int workedTicks;
        private bool complete;
        private IntVec3 sampledCell = IntVec3.Invalid;
        private int sampledDepth = -1;
        private string cachedReport;

        public CompProperties_CoreSampler Props => (CompProperties_CoreSampler)props;

        public bool Active
        {
            get
            {
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power != null && power.PowerOn;
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref workedTicks, "strataSampleWorked");
            Scribe_Values.Look(ref complete, "strataSampleComplete");
            Scribe_Values.Look(ref sampledCell, "strataSampleCell");
            Scribe_Values.Look(ref sampledDepth, "strataSampleDepth", -1);
            Scribe_Values.Look(ref cachedReport, "strataSampleReport");
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(60) || !Active || complete)
            {
                return;
            }
            workedTicks += 60;
            if (workedTicks >= Props.sampleTicks)
            {
                FinishSample();
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!Active)
            {
                return "Strata_CoreSampler_NeedsPower".Translate();
            }
            if (!complete)
            {
                float pct = Mathf.Clamp01(workedTicks / (float)Props.sampleTicks);
                int left = Mathf.Max(0, Props.sampleTicks - workedTicks);
                return "Strata_CoreSampler_Working".Translate(
                    pct.ToStringPercent(),
                    left.ToStringTicksToPeriod());
            }
            return cachedReport ?? "Strata_CoreSampler_NeedsPower".Translate();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (complete)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Strata_CoreSampler_ResampleLabel".Translate(),
                    defaultDesc = "Strata_CoreSampler_ResampleDesc".Translate(),
                    action = Restart,
                };
            }
            if (Prefs.DevMode && !complete)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Dev: finish core sample",
                    action = FinishSample,
                };
            }
        }

        internal void FinishSample()
        {
            complete = true;
            workedTicks = Props.sampleTicks;
            sampledCell = parent.Position;
            sampledDepth = StratumSurvey.TargetDepthBelow(parent.Map);
            cachedReport = BuildReport();
        }

        private void Restart()
        {
            complete = false;
            workedTicks = 0;
            cachedReport = null;
            sampledCell = IntVec3.Invalid;
            sampledDepth = -1;
        }

        private string BuildReport()
        {
            Map map = parent.Map;
            IntVec3 cell = parent.Position;
            int depth = StratumSurvey.TargetDepthBelow(map);
            StratumTruth truth = StratumSurvey.TruthForSample(map, cell);
            Map below = FindExistingBelow(map);
            if (below != null)
            {
                IntVec3 aligned = StrataMapUtility.VerticalAlign(cell, map, below);
                float seenOre = StratumSurvey.ObservedOre(below, aligned, 12f);
                if (seenOre >= 0f)
                {
                    truth.ore = Mathf.Clamp01(truth.ore * 0.35f + seenOre * 0.65f);
                }
            }

            float band = StratumSurvey.Precision();
            var sb = new StringBuilder();
            sb.Append("Strata_CoreSampler_Header".Translate(depth));
            sb.AppendLine();
            sb.Append("Strata_CoreSampler_Ore".Translate(StratumSurvey.BandLabel(
                truth.ore, band,
                "Strata_CoreSampler_OreLow",
                "Strata_CoreSampler_OreMid",
                "Strata_CoreSampler_OreHigh")));
            sb.AppendLine();
            sb.Append("Strata_CoreSampler_Gas".Translate(StratumSurvey.BandLabel(
                truth.gas, band,
                "Strata_CoreSampler_GasLow",
                "Strata_CoreSampler_GasMid",
                "Strata_CoreSampler_GasHigh")));
            sb.AppendLine();
            sb.Append("Strata_CoreSampler_Bugs".Translate(StratumSurvey.BandLabel(
                truth.infestation, band,
                "Strata_CoreSampler_BugsLow",
                "Strata_CoreSampler_BugsMid",
                "Strata_CoreSampler_BugsHigh")));
            sb.AppendLine();
            sb.Append("Strata_CoreSampler_Rock".Translate(StratumSurvey.BandLabel(
                truth.hardness, band,
                "Strata_CoreSampler_RockSoft",
                "Strata_CoreSampler_RockMid",
                "Strata_CoreSampler_RockHard")));
            if (WaterTableUtility.Enabled)
            {
                sb.AppendLine();
                int table = WaterTableUtility.EffectiveTableDepth(map);
                int belowTable = Mathf.Max(0, depth - table);
                if (belowTable > 0)
                {
                    sb.Append("Strata_CoreSampler_WaterWet".Translate(belowTable));
                }
                else
                {
                    sb.Append("Strata_CoreSampler_WaterDry".Translate());
                }
            }
            if (truth.lostHint)
            {
                sb.AppendLine();
                sb.Append("Strata_CoreSampler_LostHint".Translate());
            }
            sb.AppendLine();
            sb.Append("Strata_CoreSampler_Band".Translate(band.ToStringPercent()));
            return sb.ToString().TrimEnd();
        }

        private static Map FindExistingBelow(Map map)
        {
            int want = StratumSurvey.TargetDepthBelow(map);
            List<Map> maps = Find.Maps;
            PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(map);
            for (int i = 0; i < maps.Count; i++)
            {
                Map other = maps[i];
                if (!StrataMapUtility.IsUnderground(other) || StrataDepth.Of(other) != want)
                {
                    continue;
                }
                if (StrataMapUtility.ResolveColonyPlanetTile(other) == tile)
                {
                    return other;
                }
            }
            return null;
        }
    }
}

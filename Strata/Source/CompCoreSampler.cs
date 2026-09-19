using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_CoreSampler : CompProperties
    {
        public int sampleTicks = 7500;
        public float powerDrawWorking = 400f;

        public CompProperties_CoreSampler()
        {
            compClass = typeof(CompCoreSampler);
        }
    }

    // Drill that reports on the stratum directly below this cell.
    public class CompCoreSampler : ThingComp
    {
        private CompPowerTrader powerComp;
        private int workLeft = -1;
        private bool hasReport;
        private string cachedReport;
        private int reportDepth = 1;

        public CompProperties_CoreSampler Props => (CompProperties_CoreSampler)props;

        public bool Powered => powerComp != null && powerComp.PowerOn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            WorldComponent_StrataStratum.Get?.WaterTableDepth(
                StrataMapUtility.ResolveColonyPlanetTile(parent.Map));
        }

        public override void CompTick()
        {
            base.CompTick();
            if (workLeft <= 0 || !Powered)
            {
                if (powerComp != null && workLeft <= 0)
                {
                    powerComp.PowerOutput = -powerComp.Props.PowerConsumption;
                }
                return;
            }

            if (powerComp != null)
            {
                powerComp.PowerOutput = -Props.powerDrawWorking;
            }

            workLeft--;
            if (workLeft <= 0)
            {
                FinishSample();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref workLeft, "strataSampleWorkLeft", -1);
            Scribe_Values.Look(ref hasReport, "strataSampleHasReport", false);
            Scribe_Values.Look(ref cachedReport, "strataSampleReport");
            Scribe_Values.Look(ref reportDepth, "strataSampleDepth", 1);
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            if (!Powered)
            {
                sb.AppendLine("Strata_Sample_NeedsPower".Translate());
            }
            else if (workLeft > 0)
            {
                sb.AppendLine("Strata_Sample_Working".Translate(
                    workLeft.ToStringTicksToPeriod()));
            }
            else if (hasReport && !cachedReport.NullOrEmpty())
            {
                sb.AppendLine(cachedReport);
            }
            else
            {
                sb.AppendLine("Strata_Sample_Ready".Translate());
            }

            Map map = parent.Map;
            if (map != null)
            {
                PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(map);
                int table = WorldComponent_StrataStratum.Get?.WaterTableDepth(tile) ?? 2;
                sb.Append("Strata_Sample_TableDepth".Translate(table));
            }
            return sb.ToString().TrimEnd();
        }

        public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action cmd = new Command_Action
            {
                defaultLabel = "Strata_Sample_GizmoLabel".Translate(),
                defaultDesc = "Strata_Sample_GizmoDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/SelectNextColonist", reportFailure: false),
                action = StartSample,
            };
            if (!Powered)
            {
                cmd.Disable("Strata_Sample_NeedsPower".Translate());
            }
            else if (workLeft > 0)
            {
                cmd.Disable("Strata_Sample_Busy".Translate());
            }
            yield return cmd;
        }

        private void StartSample()
        {
            workLeft = Props.sampleTicks;
            hasReport = false;
            cachedReport = null;
        }

        private void FinishSample()
        {
            Map map = parent.Map;
            if (map == null)
            {
                return;
            }

            PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(map);
            int currentDepth = StrataMapUtility.IsUnderground(map)
                ? Mathf.Max(0, StrataDepth.Of(map))
                : 0;
            reportDepth = currentDepth + 1;

            float quality = parent.GetStatValue(StatDefOf.MaxHitPoints) > 0f
                ? parent.HitPoints / (float)parent.MaxHitPoints
                : 1f;
            float band = StratumForecastUtility.BandWidth(
                StratumForecastUtility.ResearchProgress(), quality);

            StratumForecastUtility.Truth truth =
                StratumForecastUtility.ComputeTruth(tile, reportDepth, parent.Position);
            StratumForecastUtility.Report report =
                StratumForecastUtility.FormatReport(truth, band);

            WorldComponent_StrataStratum.Get?.RememberSample(
                tile, reportDepth, parent.Position, truth, band);

            var sb = new StringBuilder();
            sb.AppendLine("Strata_Sample_Header".Translate(reportDepth));
            sb.AppendLine(report.oreLabel);
            sb.AppendLine(report.gasLabel);
            sb.AppendLine(report.infestationLabel);
            sb.AppendLine(report.rockLabel);
            sb.AppendLine(report.waterLabel);
            if (!report.lostFloorLabel.NullOrEmpty())
            {
                sb.AppendLine(report.lostFloorLabel);
            }
            cachedReport = sb.ToString().TrimEnd();
            hasReport = true;

            Messages.Message(
                "Strata_Sample_Complete".Translate(parent.LabelShort),
                parent,
                MessageTypeDefOf.PositiveEvent);
        }
    }
}

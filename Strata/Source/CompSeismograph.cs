using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_Seismograph : CompProperties
    {
        public CompProperties_Seismograph()
        {
            compClass = typeof(CompSeismograph);
        }
    }

    // Powered console that forecasts tremor, cave-in, and infestation pressure
    // for the map it sits on, plus a summary line for every linked underground
    // level (AZR-271).
    public class CompSeismograph : ThingComp
    {
        private const int SampleIntervalTicks = 250;

        private int lastSampleTick = -9999;
        private float tremorRisk;
        private float caveInRisk;
        private float infestationPressure;
        private bool alertLatch;
        private int lastOtherLevelsTick = -9999;
        private readonly List<string> otherLevelLines = new List<string>();

        public CompProperties_Seismograph Props => (CompProperties_Seismograph)props;

        public bool Powered
        {
            get
            {
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power == null || power.PowerOn;
            }
        }

        public float TremorRisk => EnsureFresh().tremorRisk;
        public float CaveInRisk => EnsureFresh().caveInRisk;
        public float InfestationPressure => EnsureFresh().infestationPressure;

        public bool AnyElevated =>
            Powered && (TremorRisk >= 0.55f || CaveInRisk >= 0.55f || InfestationPressure >= 0.55f);

        public override void CompTick()
        {
            if (!parent.IsHashIntervalTick(SampleIntervalTicks) || !Powered)
            {
                return;
            }
            EnsureFresh(force: true);
            if (AnyElevated && !alertLatch)
            {
                alertLatch = true;
                Messages.Message(
                    "Strata_Seismograph_Alert".Translate(parent.LabelShort),
                    parent,
                    MessageTypeDefOf.CautionInput,
                    historical: false);
            }
            else if (!AnyElevated)
            {
                alertLatch = false;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref alertLatch, "strataSeismoAlertLatch", defaultValue: false);
        }

        public override string CompInspectStringExtra()
        {
            if (!Powered)
            {
                return "Strata_Seismograph_Unpowered".Translate();
            }
            Sample s = EnsureFresh();
            var sb = new StringBuilder();
            sb.AppendLine("Strata_Seismograph_Tremor".Translate(RiskLabel(s.tremorRisk)));
            sb.AppendLine("Strata_Seismograph_CaveIn".Translate(RiskLabel(s.caveInRisk)));
            sb.Append("Strata_Seismograph_Infestation".Translate(RiskLabel(s.infestationPressure)));
            MapComponent_MiningNoise noise = MapComponent_MiningNoise.For(parent.Map);
            if (noise != null && noise.Noise > 0.5f)
            {
                sb.AppendLine();
                sb.Append("Strata_Seismograph_Noise".Translate(noise.Pressure01.ToStringPercent()));
            }
            List<string> others = OtherLevelLines();
            if (others.Count > 0)
            {
                sb.AppendLine();
                sb.Append("Strata_Seismograph_OtherLevels".Translate());
                for (int i = 0; i < others.Count; i++)
                {
                    sb.AppendLine();
                    sb.Append("  ").Append(others[i]);
                }
            }
            return sb.ToString();
        }

        private List<string> OtherLevelLines()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick - lastOtherLevelsTick < SampleIntervalTicks)
            {
                return otherLevelLines;
            }
            lastOtherLevelsTick = tick;
            otherLevelLines.Clear();
            Map here = parent.Map;
            Map surface = StrataMapUtility.IsSurfacePlayerHome(here) ? here : FindSurfaceHome(here);
            if (surface == null)
            {
                return otherLevelLines;
            }
            var levels = new List<Map>();
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(surface))
            {
                if (link.map != null && link.map != here && StrataMapUtility.IsUnderground(link.map)
                    && !levels.Contains(link.map))
                {
                    levels.Add(link.map);
                }
            }
            levels.Sort((a, b) => StrataDepth.Altitude(b).CompareTo(StrataDepth.Altitude(a)));
            for (int i = 0; i < levels.Count; i++)
            {
                Map level = levels[i];
                otherLevelLines.Add("Strata_Seismograph_LevelLine".Translate(
                    LevelName(level),
                    RiskLabel(ComputeTremorRisk(level)),
                    RiskLabel(ComputeCaveInRisk(level)),
                    RiskLabel(ComputeInfestationPressure(level))));
            }
            return otherLevelLines;
        }

        private static string LevelName(Map level)
        {
            string custom = StrataLevelLabels.Get?.GetLabel(level);
            return custom.NullOrEmpty()
                ? "Strata_LevelBelow".Translate(StrataDepth.Altitude(level)).ToString()
                : custom;
        }

        private Sample EnsureFresh(bool force = false)
        {
            int tick = Find.TickManager.TicksGame;
            if (!force && tick - lastSampleTick < SampleIntervalTicks)
            {
                return new Sample(tremorRisk, caveInRisk, infestationPressure);
            }
            lastSampleTick = tick;
            Map map = parent.Map;
            tremorRisk = ComputeTremorRisk(map);
            caveInRisk = ComputeCaveInRisk(map);
            infestationPressure = ComputeInfestationPressure(map);
            return new Sample(tremorRisk, caveInRisk, infestationPressure);
        }

        private static float ComputeTremorRisk(Map map)
        {
            if (map == null)
            {
                return 0f;
            }
            // Tremors fire on the surface home and rattle linked underground floors.
            Map surface = StrataMapUtility.IsSurfacePlayerHome(map)
                ? map
                : FindSurfaceHome(map);
            if (surface == null)
            {
                return StrataMapUtility.IsUnderground(map) ? 0.25f : 0.05f;
            }
            int depth = StrataMapUtility.IsUnderground(map) ? StrataDepth.Of(map) : 0;
            int linkedBelow = 0;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(surface))
            {
                if (StrataMapUtility.IsUnderground(link.map))
                {
                    linkedBelow++;
                }
            }
            float risk = 0.15f + 0.08f * linkedBelow + 0.05f * depth;
            // Storyteller threat points nudge the needle.
            float points = StorytellerUtility.DefaultThreatPointsNow(surface);
            risk += Mathf.Clamp01(points / 1200f) * 0.25f;
            return Mathf.Clamp01(risk);
        }

        private static float ComputeCaveInRisk(Map map)
        {
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return 0f;
            }
            MapComponent_SupportOverlay overlay = map.GetComponent<MapComponent_SupportOverlay>();
            int atRisk = overlay?.AtRiskCells?.Count ?? 0;
            ShoringMapComponent shoring = map.GetComponent<ShoringMapComponent>();
            int pillars = shoring?.ActivePillarCount ?? 0;

            // Sample thick-roof open cells if overlay is empty / cold.
            if (atRisk == 0)
            {
                int sample = 0;
                int thickOpen = 0;
                CellRect bounds = CellRect.WholeMap(map);
                foreach (IntVec3 c in bounds)
                {
                    if ((c.x + c.z * 17) % 23 != 0)
                    {
                        continue;
                    }
                    sample++;
                    if (c.Standable(map)
                        && map.roofGrid.RoofAt(c) == RoofDefOf.RoofRockThick
                        && c.GetEdifice(map) == null
                        && !c.Fogged(map)
                        && (shoring == null || !shoring.CellIsProtected(c)))
                    {
                        thickOpen++;
                    }
                }
                float ratio = sample > 0 ? thickOpen / (float)sample : 0f;
                return Mathf.Clamp01(ratio * 2.2f - pillars * 0.04f);
            }

            float density = Mathf.Clamp01(atRisk / 80f);
            return Mathf.Clamp01(density - pillars * 0.05f);
        }

        private static float ComputeInfestationPressure(Map map)
        {
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return 0f;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.b1InfestationsEnabled)
            {
                return 0f;
            }
            float depthFactor = MapComponent_MiningNoise.DepthInfestationFactor(map);
            // Normalize depth factor (1.3..3) into a 0..~0.7 base.
            float basePressure = Mathf.InverseLerp(1.3f, 3f, depthFactor) * 0.7f;
            MapComponent_MiningNoise noise = MapComponent_MiningNoise.For(map);
            float noiseBoost = noise != null ? noise.Pressure01 * 0.45f : 0f;
            return Mathf.Clamp01(basePressure + noiseBoost);
        }

        private static Map FindSurfaceHome(Map map)
        {
            if (map == null)
            {
                return null;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                if (StrataMapUtility.IsSurfacePlayerHome(link.map))
                {
                    return link.map;
                }
            }
            foreach (Map m in Find.Maps)
            {
                if (StrataMapUtility.IsSurfacePlayerHome(m) && LevelGraph.AnyLinkFrom(m))
                {
                    foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(m))
                    {
                        if (link.map == map)
                        {
                            return m;
                        }
                    }
                }
            }
            return null;
        }

        private static string RiskLabel(float risk)
        {
            if (risk < 0.25f)
            {
                return "Strata_Seismograph_RiskLow".Translate();
            }
            if (risk < 0.55f)
            {
                return "Strata_Seismograph_RiskModerate".Translate();
            }
            if (risk < 0.8f)
            {
                return "Strata_Seismograph_RiskHigh".Translate();
            }
            return "Strata_Seismograph_RiskCritical".Translate();
        }

        private readonly struct Sample
        {
            public readonly float tremorRisk;
            public readonly float caveInRisk;
            public readonly float infestationPressure;

            public Sample(float tremor, float caveIn, float infestation)
            {
                tremorRisk = tremor;
                caveInRisk = caveIn;
                infestationPressure = infestation;
            }
        }
    }

    public class Alert_SeismographWarning : Alert
    {
        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();
        private AlertReport cachedReport = false;
        private int lastScanTick = -9999;

        public Alert_SeismographWarning()
        {
            defaultLabel = "Strata_Alert_Seismograph_Label".Translate();
            defaultExplanation = "Strata_Alert_Seismograph_Explanation".Translate();
            defaultPriority = AlertPriority.Medium;
        }

        public override AlertReport GetReport()
        {
            if (!StrataAlertScanCache.ShouldRescan(ref lastScanTick))
            {
                return cachedReport;
            }
            targets.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                ThingDef def = StrataThingDefOf.Strata_Seismograph;
                if (def == null)
                {
                    continue;
                }
                List<Thing> things = maps[i].listerThings.ThingsOfDef(def);
                if (things == null)
                {
                    continue;
                }
                for (int t = 0; t < things.Count; t++)
                {
                    CompSeismograph seismo = things[t].TryGetComp<CompSeismograph>();
                    if (seismo != null && seismo.AnyElevated)
                    {
                        targets.Add(new GlobalTargetInfo(things[t]));
                    }
                }
            }
            cachedReport = targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
            return cachedReport;
        }
    }
}

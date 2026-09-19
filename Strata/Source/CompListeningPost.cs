using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_ListeningPost : CompProperties
    {
        public float baseRange = 28f;
        public int scanIntervalTicks = 2500;

        public CompProperties_ListeningPost()
        {
            compClass = typeof(CompListeningPost);
        }
    }

    // Powered listening post — hears infestation / deep-raid pressure through rock.
    public class CompListeningPost : ThingComp
    {
        private CompPowerTrader powerComp;
        private string lastReading;
        private float lastNoise;
        private float lastRange;
        private int lastLetterTick = -999999;

        public CompProperties_ListeningPost Props => (CompProperties_ListeningPost)props;

        public bool Powered => powerComp != null && powerComp.PowerOn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Props.scanIntervalTicks) || !Powered)
            {
                return;
            }
            Scan();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastReading, "strataListenReading");
            Scribe_Values.Look(ref lastNoise, "strataListenNoise");
            Scribe_Values.Look(ref lastRange, "strataListenRange");
            Scribe_Values.Look(ref lastLetterTick, "strataListenLetterTick", -999999);
        }

        public override string CompInspectStringExtra()
        {
            if (!Powered)
            {
                return "Strata_Listen_NeedsPower".Translate();
            }
            var sb = new StringBuilder();
            sb.AppendLine("Strata_Listen_Range".Translate(lastRange.ToString("0.#"), Props.baseRange.ToString("0.#")));
            sb.AppendLine("Strata_Listen_Noise".Translate(lastNoise.ToStringPercent()));
            if (!lastReading.NullOrEmpty())
            {
                sb.Append(lastReading);
            }
            else
            {
                sb.Append("Strata_Listen_Quiet".Translate());
            }
            return sb.ToString().TrimEnd();
        }

        private void Scan()
        {
            Map map = parent.Map;
            if (map == null)
            {
                return;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.b1InfestationsEnabled)
            {
                lastReading = "Strata_Listen_Disabled".Translate();
                lastNoise = 0f;
                lastRange = Props.baseRange;
                return;
            }

            lastNoise = MeasureLocalNoise(map, parent.Position);
            lastRange = Props.baseRange * Mathf.Clamp01(1f - lastNoise * 0.85f);
            if (lastRange < 4f)
            {
                lastReading = "Strata_Listen_Deaf".Translate();
                return;
            }

            float pressure = EstimatePressure(map);
            if (pressure < 0.35f)
            {
                lastReading = "Strata_Listen_Quiet".Translate();
                return;
            }

            Rot4 dir = DirectionFromSeed(map);
            string dirLabel = DirLabel(dir);
            string vertical = StrataMapUtility.IsUnderground(map)
                ? "Strata_Listen_ThroughRock".Translate().Resolve()
                : "Strata_Listen_Below".Translate().Resolve();
            string size = pressure > 0.7f
                ? "Strata_Listen_SomethingLarge".Translate().Resolve()
                : "Strata_Listen_Something".Translate().Resolve();

            lastReading = "Strata_Listen_Contact".Translate(size, vertical, dirLabel);

            if (pressure >= 0.55f
                && Find.TickManager.TicksGame - lastLetterTick > 60000
                && HearsThroughSeal(map))
            {
                lastLetterTick = Find.TickManager.TicksGame;
                Find.LetterStack.ReceiveLetter(
                    "Strata_Listen_LetterLabel".Translate(),
                    "Strata_Listen_LetterText".Translate(parent.LabelShort, lastReading),
                    LetterDefOf.ThreatSmall,
                    parent);
            }
        }

        private static float MeasureLocalNoise(Map map, IntVec3 center)
        {
            float noise = 0f;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 12f, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t is Pawn pawn && pawn.CurJob != null
                        && (pawn.CurJob.def == JobDefOf.Mine
                            || pawn.CurJob.def?.defName == "Mine"))
                    {
                        noise += 0.35f;
                        continue;
                    }
                    if (t.def == null)
                    {
                        continue;
                    }
                    string name = t.def.defName ?? string.Empty;
                    if (name.Contains("Drill") || name.Contains("Miner") || name.Contains("CoreSampler"))
                    {
                        noise += 0.45f;
                    }
                    else if (t.TryGetComp<CompPowerTrader>() is CompPowerTrader power
                        && power.PowerOn
                        && power.Props.PowerConsumption >= 200f)
                    {
                        noise += 0.12f;
                    }
                }
            }
            return Mathf.Clamp01(noise);
        }

        private static float EstimatePressure(Map map)
        {
            int depth = Mathf.Max(1, StrataDepth.Of(map));
            if (!StrataMapUtility.IsUnderground(map))
            {
                depth = 1;
            }
            PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(map);
            StratumForecastUtility.Truth truth =
                StratumForecastUtility.ComputeLevelTruth(tile, depth);
            float story = 0.25f;
            if (Find.Storyteller != null)
            {
                story = Mathf.Clamp01(Find.Storyteller.difficulty.threatScale * 0.35f);
            }
            return Mathf.Clamp01(truth.infestationPressure * 0.7f + story * 0.3f);
        }

        private static Rot4 DirectionFromSeed(Map map)
        {
            int seed = Find.TickManager.TicksGame / 2500;
            seed ^= map?.uniqueID ?? 0;
            Rand.PushState(seed);
            try
            {
                return new Rot4(Rand.RangeInclusive(0, 3));
            }
            finally
            {
                Rand.PopState();
            }
        }

        private static string DirLabel(Rot4 rot)
        {
            if (rot == Rot4.North)
            {
                return "Strata_Listen_North".Translate().Resolve();
            }
            if (rot == Rot4.East)
            {
                return "Strata_Listen_East".Translate().Resolve();
            }
            if (rot == Rot4.South)
            {
                return "Strata_Listen_South".Translate().Resolve();
            }
            return "Strata_Listen_West".Translate().Resolve();
        }

        // Sealed stairwells still transmit sound — always true for this map's
        // own reading, and we still warn about linked levels even when sealed.
        private static bool HearsThroughSeal(Map map) => map != null;
    }
}

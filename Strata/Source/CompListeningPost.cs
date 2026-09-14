using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_ListeningPost : CompProperties
    {
        public float baseRange = 42f;

        public CompProperties_ListeningPost()
        {
            compClass = typeof(CompListeningPost);
        }
    }

    public class CompListeningPost : ThingComp
    {
        public CompProperties_ListeningPost Props => (CompProperties_ListeningPost)props;

        public bool Active
        {
            get
            {
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power != null && power.PowerOn;
            }
        }

        public float EffectiveRange
        {
            get
            {
                float noise = MapComponent_StrataNoise.Get(parent.Map)?.Noise01 ?? 0f;
                return Props.baseRange * Mathf.Clamp01(1f - noise * 0.92f);
            }
        }

        public float Noise01 => MapComponent_StrataNoise.Get(parent.Map)?.Noise01 ?? 0f;

        public bool ListeningEnabled
        {
            get
            {
                StrataSettings settings = StrataMod.Settings;
                if (settings == null)
                {
                    return false;
                }
                return settings.b1InfestationsEnabled || settings.noiseAttractsDarkEnabled;
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!Active)
            {
                return "Strata_Listen_NeedsPower".Translate();
            }
            if (!ListeningEnabled)
            {
                return "Strata_Listen_Silent".Translate();
            }
            float range = EffectiveRange;
            float noise = Noise01;
            string rangeLine = "Strata_Listen_Range".Translate(range.ToString("0"));
            if (noise >= 0.55f || range < 10f)
            {
                return rangeLine + "\n" + "Strata_Listen_Deaf".Translate();
            }
            if (TryRead(out string reading))
            {
                return rangeLine + "\n" + reading;
            }
            return rangeLine + "\n" + "Strata_Listen_Quiet".Translate();
        }

        internal bool TryRead(out string reading)
        {
            reading = null;
            if (!Active || !ListeningEnabled || EffectiveRange < 8f)
            {
                return false;
            }
            if (!TryFindPressure(out ListeningHit hit))
            {
                return false;
            }
            reading = FormatHit(hit);
            return true;
        }

        internal bool TryFindPressure(out ListeningHit hit)
        {
            hit = default;
            Map map = parent.Map;
            if (map == null)
            {
                return false;
            }
            float best = 0f;
            ListeningHit bestHit = default;
            PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(map);
            int here = StrataDepth.Altitude(map);
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map other = maps[i];
                if (StrataMapUtility.ResolveColonyPlanetTile(other) != tile)
                {
                    continue;
                }
                if (!ConsiderMap(other, here, ref best, ref bestHit))
                {
                    continue;
                }
            }

            int belowDepth = StratumSurvey.TargetDepthBelow(map);
            if (FindExistingAtDepth(tile, belowDepth) == null)
            {
                StratumTruth truth = StratumSurvey.TruthForSample(map, parent.Position);
                float pressure = truth.infestation * 0.55f;
                if (truth.lostHint)
                {
                    pressure = Mathf.Max(pressure, 0.7f);
                }
                if (pressure > best)
                {
                    best = pressure;
                    IntVec3 ghost = GhostCell(map, belowDepth);
                    bestHit = new ListeningHit
                    {
                        pressure = pressure,
                        below = true,
                        east = ghost.x >= parent.Position.x,
                        imminent = false,
                        ungnerated = true,
                    };
                }
            }

            if (best < 0.28f)
            {
                return false;
            }
            hit = bestHit;
            return true;
        }

        private bool ConsiderMap(Map other, int hereAlt, ref float best, ref ListeningHit bestHit)
        {
            float pressure = 0f;
            bool imminent = false;
            IntVec3 source = other.Center;
            List<Thing> spawners = other.listerThings?.ThingsOfDef(ThingDefOf.TunnelHiveSpawner);
            if (spawners != null && spawners.Count > 0)
            {
                pressure = 0.95f;
                imminent = true;
                source = spawners[0].Position;
            }
            List<Thing> hives = other.listerThings?.ThingsOfDef(ThingDefOf.Hive);
            if (hives != null && hives.Count > 0)
            {
                pressure = Mathf.Max(pressure, 0.55f + 0.08f * hives.Count);
                source = hives[0].Position;
            }
            int insects = 0;
            if (Faction.OfInsects != null)
            {
                insects = other.mapPawns?.SpawnedPawnsInFaction(Faction.OfInsects)?.Count ?? 0;
            }
            if (insects > 0)
            {
                pressure = Mathf.Max(pressure, Mathf.Min(0.4f + insects * 0.04f, 0.85f));
            }
            if (other != parent.Map)
            {
                StratumTruth truth = StratumSurvey.Truth(
                    StrataMapUtility.ResolveColonyPlanetTile(other),
                    Mathf.Max(1, StrataDepth.Of(other)),
                    StrataMapUtility.VerticalAlign(parent.Position, parent.Map, other));
                pressure = Mathf.Max(pressure, truth.infestation * 0.4f);
            }
            if (pressure <= best)
            {
                return false;
            }
            int otherAlt = StrataDepth.Altitude(other);
            IntVec3 aligned = other == parent.Map
                ? source
                : StrataMapUtility.VerticalAlign(source, other, parent.Map);
            best = pressure;
            bestHit = new ListeningHit
            {
                pressure = pressure,
                below = otherAlt < hereAlt,
                east = aligned.x >= parent.Position.x,
                imminent = imminent,
                ungnerated = false,
            };
            return true;
        }

        private static Map FindExistingAtDepth(PlanetTile tile, int depth)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map other = maps[i];
                if (StrataMapUtility.IsUnderground(other)
                    && StrataDepth.Of(other) == depth
                    && StrataMapUtility.ResolveColonyPlanetTile(other) == tile)
                {
                    return other;
                }
            }
            return null;
        }

        private IntVec3 GhostCell(Map map, int depth)
        {
            int seed = StratumSurvey.Seed(
                StrataMapUtility.ResolveColonyPlanetTile(map),
                depth,
                StratumSurvey.ChunkOf(parent.Position.x),
                StratumSurvey.ChunkOf(parent.Position.z));
            Rand.PushState(seed ^ 917);
            try
            {
                return new IntVec3(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
            }
            finally
            {
                Rand.PopState();
            }
        }

        private static string FormatHit(ListeningHit hit)
        {
            string size = hit.pressure >= 0.75f
                ? "Strata_Listen_Large".Translate()
                : hit.pressure >= 0.5f
                    ? "Strata_Listen_Something".Translate()
                    : "Strata_Listen_Faint".Translate();
            string dir = hit.below
                ? (hit.east ? "Strata_Listen_BelowEast".Translate() : "Strata_Listen_BelowWest".Translate())
                : (hit.east ? "Strata_Listen_East".Translate() : "Strata_Listen_West".Translate());
            if (hit.imminent)
            {
                return "Strata_Listen_Imminent".Translate(size, dir);
            }
            if (hit.ungnerated)
            {
                return "Strata_Listen_ThroughRock".Translate(size, dir);
            }
            return "Strata_Listen_Reading".Translate(size, dir);
        }

        internal struct ListeningHit
        {
            public float pressure;
            public bool below;
            public bool east;
            public bool imminent;
            public bool ungnerated;
        }
    }
}

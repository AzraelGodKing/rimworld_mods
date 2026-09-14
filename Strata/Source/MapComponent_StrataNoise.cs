using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    // Ambient industry/mining noise on a level. Loud floors attract bugs
    // (optional) and deafen listening posts.
    public class MapComponent_StrataNoise : MapComponent
    {
        private const int Interval = 60;

        public float Noise01 { get; private set; }

        public MapComponent_StrataNoise(Map map) : base(map)
        {
        }

        public static MapComponent_StrataNoise Get(Map map) =>
            map?.GetComponent<MapComponent_StrataNoise>();

        public override void MapComponentTick()
        {
            if (!map.IsHashIntervalTick(Interval))
            {
                return;
            }
            Noise01 = Measure();
        }

        private float Measure()
        {
            float raw = 0f;
            IReadOnlyList<Pawn> pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    JobDef job = pawn.CurJobDef;
                    if (job == null)
                    {
                        continue;
                    }
                    string jobName = job.defName;
                    if (jobName == "Mine" || jobName == "SmoothFloor" || jobName == "SmoothWall")
                    {
                        raw += 0.22f;
                    }
                    else if (jobName == "OperateDeepDrill" || jobName == "Drill")
                    {
                        raw += 0.35f;
                    }
                    else if (job == JobDefOf.DoBill)
                    {
                        raw += 0.08f;
                    }
                }
            }

            List<Building> buildings = map.listerBuildings?.allBuildingsColonist;
            if (buildings != null)
            {
                for (int i = 0; i < buildings.Count; i++)
                {
                    Building b = buildings[i];
                    if (b.def == ThingDefOf.DeepDrill && Powered(b))
                    {
                        raw += 0.4f;
                    }
                    else if (b.def == StrataThingDefOf.Strata_CoreSampler && Powered(b))
                    {
                        raw += 0.3f;
                    }
                    else if (b.def != null && b.def.IsWorkTable && Powered(b))
                    {
                        raw += 0.05f;
                    }
                }
            }

            LevelRole role = WorldComponent_StrataLevelRoles.Get?.GetRole(map) ?? LevelRole.None;
            if (role == LevelRole.Quarry)
            {
                raw += 0.35f;
            }
            else if (role == LevelRole.Workshop)
            {
                raw += 0.18f;
            }

            return Mathf.Clamp01(raw);
        }

        private static bool Powered(Thing thing)
        {
            CompPowerTrader power = thing.TryGetComp<CompPowerTrader>();
            return power == null || power.PowerOn;
        }
    }
}

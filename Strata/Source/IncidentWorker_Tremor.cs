using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // A deep tremor rolls through the ground, rattling structures on the
    // surface — and echoing through linked underground levels.
    public class IncidentWorker_Tremor : IncidentWorker
    {
        private const int MinStructures = 2;

        private const int MaxStructures = 5;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map
                && StrataMapUtility.IsSurfacePlayerHome(map)
                && (Candidates(map).Count > 0 || HasUndergroundLevels(map));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            List<Building> candidates = Candidates(map);
            IntVec3 epicentre = map.Center;
            if (candidates.Count > 0)
            {
                candidates.Shuffle();
                int count = Mathf.Min(Rand.RangeInclusive(MinStructures, MaxStructures), candidates.Count);
                epicentre = candidates[0].Position;
                for (int i = 0; i < count; i++)
                {
                    Building b = candidates[i];
                    int damage = Mathf.RoundToInt(b.MaxHitPoints * Rand.Range(0.15f, 0.35f));
                    b.TakeDamage(new DamageInfo(DamageDefOf.Crush, damage, 999f));
                }
            }

            if (HasUndergroundLevels(map))
            {
                ApplyUndergroundEffects(map);
            }

            SendStandardLetter(parms, new TargetInfo(epicentre, map));
            return true;
        }

        private static bool HasUndergroundLevels(Map surface)
        {
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(surface))
            {
                if (StrataMapUtility.IsUnderground(link.map))
                {
                    return true;
                }
            }
            return false;
        }

        private static void ApplyUndergroundEffects(Map surface)
        {
            if (Rand.Chance(0.45f))
            {
                TryUnsealRandomStairwell(surface);
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(surface))
            {
                if (!StrataMapUtility.IsUnderground(link.map))
                {
                    continue;
                }
                if (Rand.Chance(0.5f))
                {
                    DamageUndergroundRoof(link.map);
                }
                if (Rand.Chance(0.35f) && StrataMod.Settings?.gasEventsEnabled != false)
                {
                    QueueDeepGas(link.map);
                }
            }
        }

        private static void TryUnsealRandomStairwell(Map surface)
        {
            var sealedControls = new List<CompStairwellControl>();
            var linked = new HashSet<Map> { surface };
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(surface))
            {
                linked.Add(link.map);
            }
            foreach (Map map in Find.Maps)
            {
                if (!linked.Contains(map))
                {
                    continue;
                }
                foreach (Thing thing in map.listerThings.AllThings)
                {
                    if (thing.TryGetComp<CompStairwellControl>() is CompStairwellControl control && control.Sealed)
                    {
                        sealedControls.Add(control);
                    }
                }
            }
            if (sealedControls.Count == 0)
            {
                return;
            }
            CompStairwellControl pick = sealedControls.RandomElement();
            pick.Unseal();
            Messages.Message("A tremor jarred a sealed stairwell open.", pick.parent, MessageTypeDefOf.NegativeEvent);
        }

        private static void DamageUndergroundRoof(Map map)
        {
            if (!CellFinder.TryFindRandomCell(map, c => IsCollapsible(c, map), out IntVec3 root))
            {
                return;
            }
            var blob = new List<IntVec3> { root };
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(root, 1.8f, useCenter: false))
            {
                if (blob.Count >= 4)
                {
                    break;
                }
                if (cell.InBounds(map) && IsCollapsible(cell, map))
                {
                    blob.Add(cell);
                }
            }
            RoofCollapserImmediate.DropRoofInCells(blob, map);
        }

        private static void QueueDeepGas(Map map)
        {
            if (!CellFinder.TryFindRandomCell(map, c => c.Standable(map) && !c.Fogged(map), out IntVec3 cell))
            {
                return;
            }
            float density = Mathf.Min(0.5f + 0.12f * StrataDepth.Of(map), 1.5f);
            AtmosphereMapComponent.QueueSeed(map, cell, StrataGasDefOf.Strata_DeepGas, density);
        }

        private static bool IsCollapsible(IntVec3 c, Map map)
        {
            return c.Standable(map)
                && map.roofGrid.RoofAt(c) == RoofDefOf.RoofRockThick
                && c.GetEdifice(map) == null
                && !c.Fogged(map);
        }

        private static List<Building> Candidates(Map map)
        {
            var list = new List<Building>();
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                if (b.def.useHitPoints && b.def.building != null && !b.def.building.isNaturalRock
                    && b.MaxHitPoints > 50)
                {
                    list.Add(b);
                }
            }
            return list;
        }
    }
}

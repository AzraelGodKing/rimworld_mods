using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // A deep tremor rolls through the ground, rattling structures on the
    // surface. Minor structural damage - a hint of the living rock below.
    public class IncidentWorker_Tremor : IncidentWorker
    {
        private const int MinStructures = 2;

        private const int MaxStructures = 5;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map
                && StrataMapUtility.IsSurfacePlayerHome(map)
                && Candidates(map).Count > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            List<Building> candidates = Candidates(map);
            if (candidates.Count == 0)
            {
                return false;
            }

            candidates.Shuffle();
            int count = Mathf.Min(Rand.RangeInclusive(MinStructures, MaxStructures), candidates.Count);
            IntVec3 epicentre = candidates[0].Position;
            for (int i = 0; i < count; i++)
            {
                Building b = candidates[i];
                int damage = Mathf.RoundToInt(b.MaxHitPoints * Rand.Range(0.15f, 0.35f));
                b.TakeDamage(new DamageInfo(DamageDefOf.Crush, damage, 999f));
            }

            SendStandardLetter(parms, new TargetInfo(epicentre, map));
            return true;
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

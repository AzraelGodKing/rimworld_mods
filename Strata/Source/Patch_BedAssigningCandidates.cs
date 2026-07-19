using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Bed assign UI only lists FreeColonists on the bed's map, so new arrivals
    // on the surface never appear for an unowned bunk upstairs (and vice versa).
    // Pull candidates from every linked colony level.
    [HarmonyPatch(typeof(CompAssignableToPawn_Bed), "get_AssigningCandidates")]
    public static class Patch_BedAssigningCandidates
    {
        public static void Postfix(CompAssignableToPawn_Bed __instance, ref IEnumerable<Pawn> __result)
        {
            ThingWithComps parent = __instance.parent;
            if (parent == null || !parent.Spawned)
            {
                return;
            }

            Map map = parent.Map;
            if (!ColonyBedUtility.MapInStrataColony(map))
            {
                return;
            }

            var maps = new List<Map>();
            ColonyBedUtility.GetColonyNetworkMaps(map, maps);
            if (maps.Count <= 1)
            {
                return;
            }

            bool humanlike = parent.def.building.bed_humanlike;
            var merged = new HashSet<Pawn>();
            if (__result != null)
            {
                foreach (Pawn p in __result)
                {
                    merged.Add(p);
                }
            }

            for (int i = 0; i < maps.Count; i++)
            {
                Map other = maps[i];
                if (other == null || other == map)
                {
                    continue;
                }

                if (humanlike)
                {
                    List<Pawn> colonists = other.mapPawns.FreeColonists;
                    for (int j = 0; j < colonists.Count; j++)
                    {
                        merged.Add(colonists[j]);
                    }

                    // Prison beds need prisoners from every linked floor too.
                    if (parent is Building_Bed bed && bed.ForPrisoners)
                    {
                        List<Pawn> prisoners = other.mapPawns.PrisonersOfColony;
                        for (int j = 0; j < prisoners.Count; j++)
                        {
                            merged.Add(prisoners[j]);
                        }
                    }
                }
                else
                {
                    List<Pawn> factionPawns = other.mapPawns.PawnsInFaction(Faction.OfPlayer);
                    for (int j = 0; j < factionPawns.Count; j++)
                    {
                        Pawn p = factionPawns[j];
                        if (!p.RaceProps.Humanlike)
                        {
                            merged.Add(p);
                        }
                    }
                }
            }

            if (humanlike)
            {
                __result = merged
                    .OrderByDescending(p => p.ownership?.OwnedBed == parent ? 1 : 0)
                    .ThenBy(p => p.LabelShort);
            }
            else
            {
                __result = merged
                    .OrderBy(p => p.KindLabel)
                    .ThenBy(p => p.Label);
            }
        }
    }
}

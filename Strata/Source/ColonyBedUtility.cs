using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Strata
{
    // Bed counts and access checks across portal-linked colony levels.
    public static class ColonyBedUtility
    {
        private static readonly List<Pawn> tmpAssignedPawns = new List<Pawn>();

        public static bool MapInStrataColony(Map map)
        {
            if (map == null)
            {
                return false;
            }
            if (StrataMapUtility.IsUnderground(map))
            {
                return true;
            }
            return map.IsPlayerHome && LevelGraph.AnyLinkFrom(map);
        }

        public static Map FindSurfacePlayerHome(Map from)
        {
            if (from == null)
            {
                return null;
            }
            if (StrataMapUtility.IsSurfacePlayerHome(from))
            {
                return from;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map candidate = maps[i];
                if (StrataMapUtility.IsSurfacePlayerHome(candidate) && MapsLinked(candidate, from))
                {
                    return candidate;
                }
            }
            return null;
        }

        public static void GetColonyNetworkMaps(Map from, List<Map> maps)
        {
            maps.Clear();
            if (from == null)
            {
                return;
            }
            Map home = FindSurfacePlayerHome(from);
            if (home == null)
            {
                if (MapInStrataColony(from))
                {
                    maps.Add(from);
                }
                return;
            }
            maps.Add(home);
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(home))
            {
                if (link.map != null && !maps.Contains(link.map))
                {
                    maps.Add(link.map);
                }
            }
        }

        // Mirrors Alert_NeedColonistBeds.NeedColonistBeds but counts beds and
        // colonists across every level linked from the surface home map.
        public static bool NeedsColonistBedsInNetwork(Map playerHome)
        {
            CollectNetworkMaps(playerHome, out List<Map> maps);
            CountColonistBeds(maps, includeBabies: false, out int singleBeds, out int doubleBeds, out _);
            if (singleBeds >= 0)
            {
                return doubleBeds < 0;
            }
            return true;
        }

        // True when this colonist has an assigned bed or a claimable bed on any
        // linked level — surface pawns with beds only on B1 should not alert.
        public static bool PawnHasBedAccessInNetwork(Pawn pawn)
        {
            if (pawn == null || !pawn.IsFreeColonist)
            {
                return true;
            }
            Map map = pawn.Map;
            if (map == null || !MapInStrataColony(map))
            {
                return false;
            }

            Building_Bed ownBed = pawn.ownership?.OwnedBed;
            if (ownBed != null && ownBed.Spawned && IsValidColonistBed(ownBed)
                && MapsLinked(map, ownBed.Map))
            {
                return true;
            }

            if (PawnRelay.HasUsableBedOnMap(pawn, map))
            {
                return true;
            }

            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                if (PawnRelay.HasClaimableBedFor(pawn, link.map))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool MapsLinked(Map from, Map to)
        {
            if (from == null || to == null)
            {
                return false;
            }
            if (from == to)
            {
                return true;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(from))
            {
                if (link.map == to)
                {
                    return true;
                }
            }
            return false;
        }

        private static void CollectNetworkMaps(Map playerHome, out List<Map> maps)
        {
            maps = new List<Map> { playerHome };
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(playerHome))
            {
                if (link.map != null && !maps.Contains(link.map))
                {
                    maps.Add(link.map);
                }
            }
        }

        // Same rules as Alert_NeedColonistBeds.AvailableColonistBeds, but beds
        // and colonists are pooled across every map in the network.
        private static void CountColonistBeds(
            List<Map> maps,
            bool includeBabies,
            out int singleBeds,
            out int doubleBeds,
            out int cribs)
        {
            singleBeds = 0;
            doubleBeds = 0;
            cribs = 0;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                List<Building> buildings = map.listerBuildings.allBuildingsColonist;
                for (int i = 0; i < buildings.Count; i++)
                {
                    if (buildings[i] is not Building_Bed { ForColonists: true, Medical: false } bed
                        || !bed.def.building.bed_humanlike)
                    {
                        continue;
                    }
                    if (bed.ForHumanBabies)
                    {
                        cribs++;
                    }
                    else if (bed.SleepingSlotsCount == 1)
                    {
                        singleBeds++;
                    }
                    else
                    {
                        doubleBeds++;
                    }
                }
            }

            int singleColonists = 0;
            int coupleColonists = 0;
            int babyColonists = 0;
            tmpAssignedPawns.Clear();
            var colonists = new List<Pawn>();
            for (int m = 0; m < maps.Count; m++)
            {
                colonists.AddRange(
                    maps[m].mapPawns.FreeColonistsSpawned.Where(p => !p.IsSlave));
            }

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if ((!pawn.Spawned && !pawn.BrieflyDespawned())
                    || pawn.needs?.rest == null
                    || tmpAssignedPawns.Contains(pawn)
                    || pawn.DevelopmentalStage.Baby())
                {
                    continue;
                }

                List<DirectPawnRelation> partners =
                    LovePartnerRelationUtility.ExistingLovePartners(pawn, allowDead: false);
                if (partners.NullOrEmpty())
                {
                    singleColonists++;
                    tmpAssignedPawns.Add(pawn);
                    continue;
                }

                Pawn partner = null;
                int bestPartnerCount = int.MaxValue;
                for (int p = 0; p < partners.Count; p++)
                {
                    Pawn other = partners[p].otherPawn;
                    if (other == null
                        || !other.Spawned
                        || !ColonistsShareNetwork(pawn, other, maps)
                        || other.Faction != Faction.OfPlayer
                        || other.HostFaction != null
                        || tmpAssignedPawns.Contains(other)
                        || other.needs?.rest == null)
                    {
                        continue;
                    }
                    int partnerCount =
                        LovePartnerRelationUtility.ExistingLovePartnersCount(other, allowDead: false);
                    if (partner == null || partnerCount < bestPartnerCount)
                    {
                        partner = other;
                        bestPartnerCount = partnerCount;
                    }
                }

                if (partner != null)
                {
                    tmpAssignedPawns.Add(partner);
                    tmpAssignedPawns.Add(pawn);
                    coupleColonists++;
                }
            }

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn.needs?.rest == null || tmpAssignedPawns.Contains(pawn))
                {
                    continue;
                }
                if (pawn.DevelopmentalStage.Baby())
                {
                    babyColonists++;
                }
                else
                {
                    singleColonists++;
                }
            }
            tmpAssignedPawns.Clear();

            for (int i = 0; i < coupleColonists; i++)
            {
                if (doubleBeds > 0)
                {
                    doubleBeds--;
                }
                else
                {
                    singleBeds -= 2;
                }
            }

            for (int i = 0; i < singleColonists; i++)
            {
                if (doubleBeds > 0)
                {
                    doubleBeds--;
                }
                else
                {
                    singleBeds--;
                }
            }

            if (!includeBabies)
            {
                return;
            }

            for (int i = 0; i < babyColonists; i++)
            {
                if (doubleBeds > 0)
                {
                    doubleBeds--;
                }
                else if (singleBeds > 0)
                {
                    singleBeds--;
                }
                else
                {
                    cribs--;
                }
            }
        }

        private static bool ColonistsShareNetwork(Pawn a, Pawn b, List<Map> maps)
        {
            Map mapA = a.Map;
            Map mapB = b.Map;
            if (mapA == null || mapB == null)
            {
                return false;
            }
            if (mapA == mapB)
            {
                return true;
            }
            return maps.Contains(mapA) && maps.Contains(mapB);
        }

        private static bool IsValidColonistBed(Building_Bed bed)
        {
            return bed != null
                && bed.Spawned
                && bed.Faction == Faction.OfPlayer
                && bed.def.building.bed_humanlike
                && !bed.Medical
                && !bed.ForPrisoners;
        }
    }
}

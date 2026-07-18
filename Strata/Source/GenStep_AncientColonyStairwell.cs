using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Occasionally places a pre-built ancient stairwell on a new colony surface
    // map so the player can reach B1 without digging-down research.
    public class GenStep_AncientColonyStairwell : GenStep
    {
        private const int MinDistFromStart = 28;

        private const int MaxDistFromStart = 55;

        private const int ClearRadius = 3;

        public override int SeedPart => 418372901;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (StrataMod.Settings == null || !StrataMod.Settings.ancientColonyStairwellEnabled)
            {
                return;
            }
            if (StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            if (StrataThingDefOf.Strata_AncientColonyStairsDown == null)
            {
                return;
            }
            if (MapHasStairPortal(map))
            {
                return;
            }
            if (!Rand.Chance(StrataMod.Settings.ancientColonyStairwellChance))
            {
                return;
            }
            IntVec3 spot = FindSpot(map);
            if (!spot.IsValid)
            {
                return;
            }
            ClearArea(map, spot);
            Thing stairs = GenSpawn.Spawn(StrataThingDefOf.Strata_AncientColonyStairsDown, spot, map);
            SpawnRubble(map, spot);
            Find.LetterStack.ReceiveLetter(
                "Strata_AncientStairwellLetterTitle".Translate(),
                "Strata_AncientStairwellLetterText".Translate(),
                LetterDefOf.NeutralEvent,
                new TargetInfo(spot, map));
        }

        private static bool MapHasStairPortal(Map map)
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is Building_StairsDown or Building_AncientColonyStairsDown)
                {
                    return true;
                }
            }
            return false;
        }

        private static IntVec3 FindSpot(Map map)
        {
            IntVec3 start = MapGenerator.PlayerStartSpotValid
                ? MapGenerator.PlayerStartSpot
                : map.Center;
            var candidates = new List<IntVec3>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(start, MaxDistFromStart, useCenter: false))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                float dist = cell.DistanceTo(start);
                if (dist < MinDistFromStart || dist > MaxDistFromStart)
                {
                    continue;
                }
                if (SpotOk(map, cell))
                {
                    candidates.Add(cell);
                }
            }
            return candidates.Count > 0 ? candidates.RandomElement() : IntVec3.Invalid;
        }

        private static bool SpotOk(Map map, IntVec3 center)
        {
            if (center.DistanceToEdge(map) < 8)
            {
                return false;
            }
            CellRect rect = GenAdj.OccupiedRect(center, Rot4.North, StrataThingDefOf.Strata_AncientColonyStairsDown.size);
            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                {
                    return false;
                }
                TerrainDef terrain = cell.GetTerrain(map);
                if (terrain.IsWater || terrain.passability == Traversability.Impassable)
                {
                    return false;
                }
                if (cell.Fogged(map))
                {
                    return false;
                }
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing is Pawn or MapPortal)
                    {
                        return false;
                    }
                    if (thing.def.IsEdifice() && thing.def.building?.isNaturalRock != true)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static void ClearArea(Map map, IntVec3 center)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, ClearRadius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];
                    if (thing is not Pawn && thing.def.destroyable && thing.def.category != ThingCategory.Building)
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        private static void SpawnRubble(Map map, IntVec3 center)
        {
            IntVec3[] offsets =
            {
                new IntVec3(2, 0, 0),
                new IntVec3(-2, 0, 1),
                new IntVec3(0, 0, -2),
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                IntVec3 cell = center + offsets[i];
                if (cell.InBounds(map) && cell.Standable(map) && cell.GetFirstBuilding(map) == null)
                {
                    GenSpawn.Spawn(ThingDefOf.ChunkSlagSteel, cell, map);
                }
            }
        }
    }
}

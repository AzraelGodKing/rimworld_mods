using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // The magma layer is the bottom of the world: the deepest level a colony
    // can dig. Nothing opens below it. It holds lava pools ringed by basalt,
    // obsidian seams where the lava cooled against the rock, and magma vents
    // that a geothermal tap turns into a lot of free power and a lot of heat.
    public static class MagmaLayer
    {
        // Ambient rock temperature on the magma layer. Hot enough that
        // colonists need cooling to live there, survivable for short trips.
        public const float OutdoorTemp = 52f;

        public const int HazardIntervalTicks = 250;

        public const int DefaultUnlimitedDepth = 8;

        public const int MaxSettingDepth = 20;

        private const int EdgeMargin = 14;
        private const float MinDistFromLanding = 24f;
        private const float MinDistBetween = 18f;
        private const int PlacementTries = 80;

        // Fire chances per hazard pulse (every 250 ticks).
        private const float FlammableIgniteChance = 0.25f;
        private const float PawnIgniteChance = 0.04f;

        public static bool Enabled => StrataMod.Settings?.magmaLayerEnabled ?? true;

        // With the normal level cap the magma layer sits one floor past the
        // cap (B3). With unlimited levels it sits at the chosen depth.
        public static int Depth
        {
            get
            {
                int floor = StrataLevelCap.HardMaxOffset + 1;
                if (!StrataLevelCap.Unlimited)
                {
                    return floor;
                }
                int chosen = StrataMod.Settings?.magmaLayerDepth ?? DefaultUnlimitedDepth;
                return Mathf.Max(floor, chosen);
            }
        }

        public static bool IsMagmaDepth(int depth) => Enabled && depth >= Depth;

        // True when a brand-new level dug under this map would be the magma layer.
        public static bool NextLevelBelowIsMagma(Map map)
        {
            return map != null && IsMagmaDepth(StrataDepth.Of(map) + 1);
        }

        public static bool IsResearchFinished()
        {
            ResearchProjectDef research = StrataMagmaDefOf.Strata_DeepBore;
            return research == null || research.IsFinished;
        }

        public static bool IsMagmaTerrain(TerrainDef terrain) =>
            terrain != null && terrain == StrataMagmaDefOf.Strata_MagmaPool;

        internal static void PlaceFeatures(Map map, MapComponent_StrataLevelKind comp)
        {
            IntVec3 landing = FindLanding(map);
            var placed = new List<IntVec3>();

            int poolCount = Rand.RangeInclusive(3, 5);
            for (int i = 0; i < poolCount; i++)
            {
                if (!TryFindSpot(map, landing, placed, out IntVec3 spot))
                {
                    break;
                }
                float radius = Rand.Range(2.4f, 4f);
                PlacePool(map, spot, radius);
                comp.AddPool(spot, radius);
                placed.Add(spot);
                PlaceObsidianNear(map, spot, radius);
            }

            int ventCount = Rand.RangeInclusive(2, 3);
            for (int i = 0; i < ventCount; i++)
            {
                if (!TryFindSpot(map, landing, placed, out IntVec3 spot))
                {
                    break;
                }
                PlaceVent(map, spot);
                placed.Add(spot);
            }

            Find.LetterStack.ReceiveLetter(
                "Strata_MagmaLetterTitle".Translate(),
                "Strata_MagmaLetterText".Translate(OutdoorTemp.ToStringTemperature("F0")),
                LetterDefOf.PositiveEvent,
                new LookTargets(landing, map));
        }

        // Lava pools are impassable. Flammable things next to them catch fire
        // now and then, and so do pawns standing at the edge.
        internal static void TickHazards(Map map, MapComponent_StrataLevelKind comp)
        {
            IReadOnlyList<IntVec3> centers = comp.PoolCenters;
            IReadOnlyList<float> radii = comp.PoolRadii;
            for (int i = 0; i < centers.Count; i++)
            {
                int cells = GenRadial.NumCellsInRadius(Mathf.Min(radii[i] + 1.9f, GenRadial.MaxRadialPatternRadius));
                for (int k = 0; k < 2; k++)
                {
                    IntVec3 cell = centers[i] + GenRadial.RadialPattern[Rand.Range(0, cells)];
                    if (!cell.InBounds(map) || IsMagmaTerrain(cell.GetTerrain(map))
                        || !Rand.Chance(FlammableIgniteChance) || !HasFlammable(cell, map))
                    {
                        continue;
                    }
                    FireUtility.TryStartFireIn(cell, map, 0.25f, null);
                }
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = pawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = pawns[i];
                if (pawn.Dead || !NextToMagma(pawn.Position, map) || !Rand.Chance(PawnIgniteChance))
                {
                    continue;
                }
                pawn.TryAttachFire(0.15f, null);
            }
        }

        private static bool NextToMagma(IntVec3 cell, Map map)
        {
            foreach (IntVec3 offset in GenAdj.AdjacentCells)
            {
                IntVec3 next = cell + offset;
                if (next.InBounds(map) && IsMagmaTerrain(next.GetTerrain(map)))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasFlammable(IntVec3 cell, Map map)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is not Pawn && things[i].FlammableNow)
                {
                    return true;
                }
            }
            return false;
        }

        private static void PlacePool(Map map, IntVec3 center, float radius)
        {
            float rim = radius + 1.5f;
            ChamberCarveUtility.CarveCircle(map, center, rim);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, rim, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                bool core = cell.InHorDistOf(center, radius);
                map.terrainGrid.SetTerrain(cell, core ? StrataMagmaDefOf.Strata_MagmaPool : StrataMagmaDefOf.Strata_BasaltCrust);
            }
            GenSpawn.Spawn(StrataMagmaDefOf.Strata_MagmaGlow, center, map);
        }

        // Obsidian forms where the lava met the rock: one seam beside each pool.
        private static void PlaceObsidianNear(Map map, IntVec3 pool, float radius)
        {
            ThingDef seam = StrataMagmaDefOf.Strata_MineableObsidian;
            float dist = radius + 2.5f;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float angle = Rand.Range(0f, 360f);
                IntVec3 root = pool + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * dist, 0f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * dist).ToIntVec3();
                if (!root.InBounds(map) || !OreReveal.IsHostRock(root, map, allowFogged: true))
                {
                    continue;
                }
                OreReveal.PlaceVeinFloodFill(map, root, seam, Rand.RangeInclusive(14, 26),
                    allowFogged: true, maxRadius: 4.5f);
                return;
            }
        }

        private static void PlaceVent(Map map, IntVec3 center)
        {
            ChamberCarveUtility.CarveCircle(map, center, 3.2f);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 3.2f, useCenter: true))
            {
                if (cell.InBounds(map))
                {
                    map.terrainGrid.SetTerrain(cell, StrataMagmaDefOf.Strata_BasaltCrust);
                }
            }
            GenSpawn.Spawn(StrataMagmaDefOf.Strata_MagmaVent, center, map);
        }

        private static IntVec3 FindLanding(Map map)
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is Building_StairsUp)
                {
                    return thing.Position;
                }
            }
            return map.Center;
        }

        private static bool TryFindSpot(Map map, IntVec3 landing, List<IntVec3> placed, out IntVec3 spot)
        {
            for (int i = 0; i < PlacementTries; i++)
            {
                var candidate = new IntVec3(
                    Rand.RangeInclusive(EdgeMargin, map.Size.x - 1 - EdgeMargin), 0,
                    Rand.RangeInclusive(EdgeMargin, map.Size.z - 1 - EdgeMargin));
                if (candidate.DistanceTo(landing) < MinDistFromLanding)
                {
                    continue;
                }
                bool tooClose = false;
                for (int k = 0; k < placed.Count; k++)
                {
                    if (candidate.DistanceTo(placed[k]) < MinDistBetween)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose || candidate.GetFirstThing<Building_StairsUp>(map) != null)
                {
                    continue;
                }
                spot = candidate;
                return true;
            }
            spot = IntVec3.Invalid;
            return false;
        }
    }

    [DefOf]
    public static class StrataMagmaDefOf
    {
        public static TerrainDef Strata_MagmaPool;

        public static TerrainDef Strata_BasaltCrust;

        public static ThingDef Strata_MagmaGlow;

        public static ThingDef Strata_MagmaVent;

        public static ThingDef Strata_GeothermalTap;

        public static ThingDef Strata_MineableObsidian;

        public static ResearchProjectDef Strata_DeepBore;

        static StrataMagmaDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataMagmaDefOf));
        }
    }

    // A magma vent breathes carbon dioxide into the level until a geothermal
    // tap caps it.
    public class CompMagmaVent : CompExhaust
    {
        public bool Capped
        {
            get
            {
                List<Thing> things = parent.Position.GetThingList(parent.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i].def == StrataMagmaDefOf.Strata_GeothermalTap)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override bool Active => parent.Spawned && !Capped;

        public override string CompInspectStringExtra()
        {
            return Capped
                ? "Strata_MagmaVentCapped".Translate()
                : "Strata_MagmaVentOpen".Translate();
        }
    }

    // Geothermal taps only work over a magma vent (like the vanilla
    // geothermal generator on a steam geyser).
    public class PlaceWorker_OnMagmaVent : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot,
            Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            List<Thing> things = loc.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def == StrataMagmaDefOf.Strata_MagmaVent && things[i].Position == loc)
                {
                    return true;
                }
            }
            return "Strata_Place_MustBeOnMagmaVent".Translate();
        }

        public override bool ForceAllowPlaceOver(BuildableDef otherDef)
        {
            return otherDef == StrataMagmaDefOf.Strata_MagmaVent;
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Shared helper for the deep-vein and prospector events: carve a rich ore
    // seam into a cluster of existing solid rock.
    public static class OreReveal
    {
        private struct OreOption
        {
            public ThingDef def;
            public float weight;
        }

        private static readonly List<OreOption> Ores = new List<OreOption>();

        private static void EnsureOres()
        {
            if (Ores.Count > 0)
            {
                return;
            }
            void Add(string name, float weight)
            {
                ThingDef d = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (d != null)
                {
                    Ores.Add(new OreOption { def = d, weight = weight });
                }
            }
            Add("MineableSteel", 1.0f);
            Add("MineableComponentsIndustrial", 0.55f);
            Add("MineableSilver", 0.5f);
            Add("MineableGold", 0.35f);
            Add("MineablePlasteel", 0.3f);
            Add("MineableJade", 0.25f);
            Add("MineableUranium", 0.2f);
        }

        // Replaces a blob of solid rock around a found seed cell with a random
        // rich ore. Returns false (revealing nothing) if the map has no suitable
        // solid rock. On success, 'location' marks the seam for a letter.
        public static bool TryRevealVein(Map map, out IntVec3 location, int minCells = 6, int maxCells = 14)
        {
            EnsureOres();
            location = IntVec3.Invalid;
            if (Ores.Count == 0)
            {
                return false;
            }
            if (!CellFinder.TryFindRandomCell(map, c => IsSolidRock(c, map), out IntVec3 root))
            {
                return false;
            }

            // Deeper levels bias toward richer ores.
            ThingDef ore = WeightedOre(StrataDepth.Of(map));
            int target = Rand.RangeInclusive(minCells, maxCells);
            int placed = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(root, 3.4f, useCenter: true))
            {
                if (placed >= target)
                {
                    break;
                }
                if (!cell.InBounds(map) || !IsSolidRock(cell, map))
                {
                    continue;
                }
                cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
                GenSpawn.Spawn(ore, cell, map);
                placed++;
            }
            if (placed == 0)
            {
                return false;
            }
            location = root;
            return true;
        }

        private static bool IsSolidRock(IntVec3 c, Map map)
        {
            Mineable m = c.GetFirstMineable(map);
            return m != null && m.def.building != null && m.def.building.isNaturalRock && !c.Fogged(map);
        }

        // "Rich" ores (everything past the common steel/components entries) get
        // a weight bonus that grows with depth.
        private static ThingDef WeightedOre(int depth)
        {
            float richBonus = 1f + 0.3f * depth;
            float total = 0f;
            foreach (OreOption o in Ores)
            {
                total += o.weight <= 0.5f ? o.weight * richBonus : o.weight;
            }
            float roll = Rand.Value * total;
            foreach (OreOption o in Ores)
            {
                roll -= o.weight <= 0.5f ? o.weight * richBonus : o.weight;
                if (roll <= 0f)
                {
                    return o.def;
                }
            }
            return Ores[0].def;
        }
    }
}

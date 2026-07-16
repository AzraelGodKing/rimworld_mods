using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Shared helper for deep-vein / prospector events and underground map gen:
    // carve a rich ore seam into a cluster of existing solid rock.
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
        // Incident use: unfogged host rock only, modest seam size.
        public static bool TryRevealVein(Map map, out IntVec3 location, int minCells = 6, int maxCells = 14)
        {
            EnsureOres();
            location = IntVec3.Invalid;
            if (Ores.Count == 0)
            {
                return false;
            }
            if (!CellFinder.TryFindRandomCell(map, c => IsHostRock(c, map, allowFogged: false), out IntVec3 root))
            {
                return false;
            }

            ThingDef ore = WeightedOre(StrataDepth.Of(map));
            int target = Rand.RangeInclusive(minCells, maxCells);
            int placed = PlaceVeinFloodFill(map, root, ore, target, allowFogged: false, maxRadius: 3.6f);
            if (placed == 0)
            {
                return false;
            }
            location = root;
            return true;
        }

        // Map-gen / debug: place one large node at a chosen root (fogged rock OK).
        public static int PlaceRichNode(Map map, IntVec3 root, ThingDef ore, int targetCells)
        {
            EnsureOres();
            float radius = Mathf.Max(4.5f, Mathf.Sqrt(targetCells) * 1.15f);
            return PlaceVeinFloodFill(map, root, ore, targetCells, allowFogged: true, maxRadius: radius);
        }

        public static ThingDef PickOreForDepth(int depth)
        {
            EnsureOres();
            return WeightedOre(depth);
        }

        public static bool HasOres => EnsureOresCounted();

        private static bool EnsureOresCounted()
        {
            EnsureOres();
            return Ores.Count > 0;
        }

        // Compact flood-fill through contiguous host rock, capped by cell count
        // and a soft radius so seams stay node-shaped rather than long snakes.
        public static int PlaceVeinFloodFill(Map map, IntVec3 root, ThingDef ore, int targetCells,
            bool allowFogged, float maxRadius)
        {
            if (ore == null || targetCells <= 0 || !IsHostRock(root, map, allowFogged))
            {
                return 0;
            }

            var queue = new Queue<IntVec3>();
            var seen = new HashSet<IntVec3>();
            queue.Enqueue(root);
            seen.Add(root);

            int placed = 0;
            while (queue.Count > 0 && placed < targetCells)
            {
                IntVec3 cell = queue.Dequeue();
                if (!IsHostRock(cell, map, allowFogged))
                {
                    continue;
                }

                cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
                GenSpawn.Spawn(ore, cell, map);
                placed++;

                // Shuffle neighbor order slightly so seams are not axis-biased.
                foreach (IntVec3 offset in GenAdj.AdjacentCells)
                {
                    IntVec3 next = cell + offset;
                    if (!seen.Add(next) || !next.InBounds(map))
                    {
                        continue;
                    }
                    if (next.DistanceTo(root) > maxRadius)
                    {
                        continue;
                    }
                    if (!IsHostRock(next, map, allowFogged))
                    {
                        continue;
                    }
                    // Prefer cardinal growth; diagonals half the time for blobbier nodes.
                    if (!IsCardinal(offset) && Rand.Chance(0.45f))
                    {
                        continue;
                    }
                    queue.Enqueue(next);
                }
            }
            return placed;
        }

        private static bool IsCardinal(IntVec3 offset)
        {
            return (offset.x == 0) != (offset.z == 0);
        }

        // Host rock = natural stone that is not already a resource ore vein.
        public static bool IsHostRock(IntVec3 c, Map map, bool allowFogged)
        {
            Mineable m = c.GetFirstMineable(map);
            if (m?.def.building == null || !m.def.building.isNaturalRock)
            {
                return false;
            }
            if (!allowFogged && c.Fogged(map))
            {
                return false;
            }
            // Vanilla resource veins are MineableSteel / MineableGold / etc.
            if (m.def.defName.StartsWith("Mineable"))
            {
                return false;
            }
            return true;
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

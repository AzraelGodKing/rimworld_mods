#nullable enable
using System.Collections.Generic;

namespace Niceties
{
    /// <summary>Walkability of a map, by cell index (index = z * width + x).</summary>
    public interface IEncloseGrid
    {
        int Width { get; }

        int Height { get; }

        /// <summary>True when a pawn could stand here once the planned walls are up, ignoring the walls themselves.</summary>
        bool Walkable(int index);

        /// <summary>
        /// True when this cell leads somewhere else, such as stairs or another map portal.
        /// A region touching an exit is never closed (AZR-569).
        /// </summary>
        bool IsExit(int index);
    }

    /// <summary>Reusable buffers, so a flood fill allocates nothing once warm (AZR-549).</summary>
    public sealed class EncloseScratch
    {
        internal int[] Visit = new int[0];
        internal int[] Open = new int[0];
        internal int[] Blocked = new int[0];
        internal int[] Queue = new int[0];
        internal readonly List<int> Fill = new List<int>();
        internal readonly List<int> Starts = new List<int>();
        internal int Generation;

        internal int Next(int cells, int maxRegion)
        {
            if (Visit.Length < cells)
            {
                Visit = new int[cells];
                Open = new int[cells];
                Blocked = new int[cells];
                Generation = 0;
            }

            if (Queue.Length < maxRegion + 1)
            {
                Queue = new int[maxRegion + 1];
            }

            if (Generation >= int.MaxValue - 2)
            {
                System.Array.Clear(Visit, 0, Visit.Length);
                System.Array.Clear(Open, 0, Open.Length);
                System.Array.Clear(Blocked, 0, Blocked.Length);
                Generation = 0;
            }

            return ++Generation;
        }
    }

    /// <summary>
    /// Leave-a-way-out flood fill, free of game types so it can be unit tested.
    /// A region next to the new walls is "closed" when it has fewer than
    /// <c>maxRegion</c> walkable cells; anything that reaches the cap counts as open.
    /// </summary>
    public static class EncloseFill
    {
        private static readonly int[] DX = { 0, 1, 0, -1 };
        private static readonly int[] DZ = { 1, 0, -1, 0 };

        /// <summary>
        /// Appends every cell of every closed region beside <paramref name="blockers"/> to
        /// <paramref name="closed"/>. Regions already proven open are remembered, so a second
        /// start in the same open area stops as soon as it touches it.
        /// </summary>
        public static void ClosedRegions<TGrid>(ref TGrid grid, List<int> blockers, int maxRegion,
            EncloseScratch s, List<int> closed)
            where TGrid : struct, IEncloseGrid
        {
            int w = grid.Width;
            int h = grid.Height;
            int gen = s.Next(w * h, maxRegion);
            for (int i = 0; i < blockers.Count; i++)
            {
                int b = blockers[i];
                if (b >= 0 && b < w * h)
                {
                    s.Blocked[b] = gen;
                }
            }

            s.Starts.Clear();
            for (int i = 0; i < blockers.Count; i++)
            {
                int b = blockers[i];
                int bx = b % w;
                int bz = b / w;
                for (int d = 0; d < 4; d++)
                {
                    int nx = bx + DX[d];
                    int nz = bz + DZ[d];
                    if (nx < 0 || nz < 0 || nx >= w || nz >= h)
                    {
                        continue;
                    }

                    int n = nz * w + nx;
                    if (s.Blocked[n] != gen && !s.Starts.Contains(n))
                    {
                        s.Starts.Add(n);
                    }
                }
            }

            for (int i = 0; i < s.Starts.Count; i++)
            {
                int start = s.Starts[i];
                if (s.Visit[start] == gen || s.Open[start] == gen || !grid.Walkable(start))
                {
                    continue;
                }

                if (grid.IsExit(start))
                {
                    s.Open[start] = gen;
                    continue;
                }

                if (Flood(ref grid, start, maxRegion, s, gen))
                {
                    closed.AddRange(s.Fill);
                }
                else
                {
                    for (int j = 0; j < s.Fill.Count; j++)
                    {
                        s.Open[s.Fill[j]] = gen;
                    }
                }
            }
        }

        /// <summary>True when the region from <paramref name="start"/> is closed.</summary>
        private static bool Flood<TGrid>(ref TGrid grid, int start, int maxRegion, EncloseScratch s, int gen)
            where TGrid : struct, IEncloseGrid
        {
            int w = grid.Width;
            int h = grid.Height;
            s.Fill.Clear();
            int head = 0;
            int tail = 0;
            s.Queue[tail++] = start;
            s.Visit[start] = gen;
            s.Fill.Add(start);
            while (head < tail)
            {
                int cell = s.Queue[head++];
                int cx = cell % w;
                int cz = cell / w;
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + DX[d];
                    int nz = cz + DZ[d];
                    if (nx < 0 || nz < 0 || nx >= w || nz >= h)
                    {
                        continue;
                    }

                    int n = nz * w + nx;
                    if (s.Blocked[n] == gen)
                    {
                        continue;
                    }

                    // Before the visited check: cells of an earlier open flood are visited too.
                    if (s.Open[n] == gen)
                    {
                        return false;
                    }

                    if (s.Visit[n] == gen)
                    {
                        continue;
                    }

                    if (grid.IsExit(n))
                    {
                        return false;
                    }

                    if (!grid.Walkable(n))
                    {
                        continue;
                    }

                    if (s.Fill.Count >= maxRegion)
                    {
                        return false;
                    }

                    s.Visit[n] = gen;
                    s.Fill.Add(n);
                    s.Queue[tail++] = n;
                }
            }

            return s.Fill.Count < maxRegion;
        }

        /// <summary>
        /// Picks the step-aside cell: nearest by squared distance, ties broken by lower z,
        /// then lower x, so every machine picks the same cell (AZR-571).
        /// Returns the index into the candidate lists, or -1 when there is none.
        /// </summary>
        public static int PickStepAside(int fromX, int fromZ, List<int> xs, List<int> zs, List<bool> reachable)
        {
            int best = -1;
            int bestDist = int.MaxValue;
            for (int i = 0; i < xs.Count; i++)
            {
                if (!reachable[i])
                {
                    continue;
                }

                int dx = xs[i] - fromX;
                int dz = zs[i] - fromZ;
                int dist = dx * dx + dz * dz;
                if (best < 0 || dist < bestDist
                    || (dist == bestDist && (zs[i] < zs[best] || (zs[i] == zs[best] && xs[i] < xs[best]))))
                {
                    best = i;
                    bestDist = dist;
                }
            }

            return best;
        }
    }
}

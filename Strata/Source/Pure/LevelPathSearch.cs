using System;
using System.Collections.Generic;

namespace Strata
{
    // Cheapest route between levels on plain values, so Tests/Strata.Tests
    // can check it without the game. A route walks to a portal, rides it,
    // lands at its arrival cell, then walks to the next portal, and so on.
    internal static class LevelPathSearch
    {
        public struct Portal
        {
            public int id;            // thingIDNumber, also the tie-breaker
            public int map;
            public int x;
            public int z;
            public int otherMap;
            public int arrivalX;
            public int arrivalZ;
            public float rideCost;    // cost of the trip through the portal itself
        }

        public struct Route
        {
            public int map;
            public float cost;
            public int firstPortal;   // index into the portal array
            public int hops;
            public int lastPortal;    // index of the portal that lands on this map
        }

        // Walking one cell costs 1. A portal ride is worth this many cells.
        public const float StairRideCost = 20f;
        public const float ElevatorRideCost = 8f;

        // From any portal on startMap at no walking cost (the pawn's position
        // is not known, so the first walk is left to the caller). Returns the
        // cheapest route to every other reachable map, cheapest first; ties go
        // to the lower portal id, so every client picks the same route.
        //
        // legCost(u, v) is the walk from portal u's arrival cell to portal v on
        // the same map: positive infinity when v cannot be reached from there.
        // Null means a straight line.
        public static List<Route> FromMap(Portal[] portals, int startMap, Func<int, int, float> legCost = null)
        {
            return Search(portals, startMap, startX: 0, startZ: 0, walkFromStart: false, legCost);
        }

        // From a cell on startMap; the walk to the first portal counts (as a
        // straight line: the caller knows the pawn, this search does not).
        public static List<Route> FromCell(Portal[] portals, int startMap, int startX, int startZ,
            Func<int, int, float> legCost = null)
        {
            return Search(portals, startMap, startX, startZ, walkFromStart: true, legCost);
        }

        private static List<Route> Search(Portal[] portals, int startMap, int startX, int startZ, bool walkFromStart,
            Func<int, int, float> legCost)
        {
            int n = portals?.Length ?? 0;
            var routes = new List<Route>();
            if (n == 0)
            {
                return routes;
            }

            // Node i = having ridden portal i (standing on its arrival cell).
            var cost = new float[n];
            var first = new int[n];
            var hops = new int[n];
            var done = new bool[n];
            for (int i = 0; i < n; i++)
            {
                cost[i] = float.PositiveInfinity;
                first[i] = -1;
            }
            for (int i = 0; i < n; i++)
            {
                if (portals[i].map != startMap || portals[i].otherMap == startMap)
                {
                    continue;
                }
                float walk = walkFromStart ? Walk(startX, startZ, portals[i].x, portals[i].z) : 0f;
                cost[i] = walk + portals[i].rideCost;
                first[i] = i;
                hops[i] = 1;
            }

            var bestByMap = new Dictionary<int, int>();
            while (true)
            {
                int u = -1;
                for (int i = 0; i < n; i++)
                {
                    if (done[i] || float.IsPositiveInfinity(cost[i]))
                    {
                        continue;
                    }
                    if (u < 0 || cost[i] < cost[u] || (cost[i] == cost[u] && portals[i].id < portals[u].id))
                    {
                        u = i;
                    }
                }
                if (u < 0)
                {
                    break;
                }
                done[u] = true;

                int landed = portals[u].otherMap;
                if (landed != startMap && !bestByMap.ContainsKey(landed))
                {
                    bestByMap[landed] = u;
                    routes.Add(new Route
                    {
                        map = landed,
                        cost = cost[u],
                        firstPortal = first[u],
                        hops = hops[u],
                        lastPortal = u,
                    });
                }

                for (int v = 0; v < n; v++)
                {
                    if (done[v] || portals[v].map != landed || portals[v].otherMap == startMap)
                    {
                        continue;
                    }
                    float leg = legCost != null
                        ? legCost(u, v)
                        : Walk(portals[u].arrivalX, portals[u].arrivalZ, portals[v].x, portals[v].z);
                    if (float.IsPositiveInfinity(leg))
                    {
                        continue;
                    }
                    float next = cost[u] + leg + portals[v].rideCost;
                    if (next < cost[v] || (next == cost[v] && portals[first[u]].id < portals[first[v]].id))
                    {
                        cost[v] = next;
                        first[v] = first[u];
                        hops[v] = hops[u] + 1;
                    }
                }
            }
            return routes;
        }

        public static float Walk(int ax, int az, int bx, int bz)
        {
            int dx = ax - bx;
            int dz = az - bz;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}

using System.Collections.Generic;
using Xunit;

namespace Strata.Tests
{
    public class LevelPathSearchTests
    {
        private const float Stair = LevelPathSearch.StairRideCost;
        private const float Lift = LevelPathSearch.ElevatorRideCost;

        private static LevelPathSearch.Portal P(int id, int map, int x, int z, int other, int ax, int az, float ride = Stair)
        {
            return new LevelPathSearch.Portal
            {
                id = id, map = map, x = x, z = z, otherMap = other, arrivalX = ax, arrivalZ = az, rideCost = ride,
            };
        }

        private static LevelPathSearch.Route RouteTo(List<LevelPathSearch.Route> routes, int map)
        {
            foreach (LevelPathSearch.Route r in routes)
            {
                if (r.map == map)
                {
                    return r;
                }
            }
            throw new Xunit.Sdk.XunitException("no route to map " + map);
        }

        [Fact]
        public void CheaperShaftWinsOverFewerStairs()
        {
            // Surface (1) to B2 (3). Shaft A goes straight down but its B1 stair
            // is 200 cells away from where it lands; shaft B is a two-hop route
            // whose stairs line up. Old BFS took the first-found route by depth.
            var portals = new[]
            {
                P(10, 1, 0, 0, 2, 0, 0),        // surface -> B1, lands at (0,0)
                P(11, 2, 0, 0, 1, 0, 0),        // B1 -> surface
                P(12, 2, 200, 0, 3, 200, 0),    // B1 -> B2, far from that landing
                P(13, 3, 200, 0, 2, 200, 0),
                P(20, 1, 50, 0, 2, 50, 0),      // surface -> B1 second shaft, lands at (50,0)
                P(21, 2, 50, 0, 1, 50, 0),
                P(22, 2, 52, 0, 3, 52, 0),      // B1 -> B2 right next to it
                P(23, 3, 52, 0, 2, 52, 0),
            };
            List<LevelPathSearch.Route> routes = LevelPathSearch.FromMap(portals, 1);
            LevelPathSearch.Route b2 = RouteTo(routes, 3);
            Assert.Equal(4, b2.firstPortal);     // index of portal 20
            Assert.Equal(2, b2.hops);
            Assert.Equal(Stair + 2f + Stair, b2.cost, 3);
        }

        [Fact]
        public void RoutesComeOutCheapestFirst()
        {
            var portals = new[]
            {
                P(1, 1, 0, 0, 2, 0, 0),
                P(2, 2, 0, 0, 1, 0, 0),
                P(3, 2, 30, 0, 3, 30, 0),
                P(4, 3, 30, 0, 2, 30, 0),
                P(5, 1, 0, 0, 4, 0, 0, Lift),
                P(6, 4, 0, 0, 1, 0, 0, Lift),
            };
            List<LevelPathSearch.Route> routes = LevelPathSearch.FromMap(portals, 1);
            Assert.Equal(new[] { 4, 2, 3 }, routes.ConvertAll(r => r.map).ToArray());
            for (int i = 1; i < routes.Count; i++)
            {
                Assert.True(routes[i - 1].cost <= routes[i].cost);
            }
        }

        [Fact]
        public void EqualCostTiesGoToTheLowerPortalId()
        {
            var portals = new[]
            {
                P(9, 1, 0, 0, 2, 0, 0),
                P(4, 1, 0, 0, 2, 0, 0),
                P(5, 2, 0, 0, 1, 0, 0),
            };
            LevelPathSearch.Route b1 = RouteTo(LevelPathSearch.FromMap(portals, 1), 2);
            Assert.Equal(1, b1.firstPortal); // portal id 4
        }

        [Fact]
        public void FromCellCountsTheFirstWalk()
        {
            var portals = new[]
            {
                P(1, 1, 0, 0, 2, 0, 0),
                P(2, 1, 100, 0, 2, 100, 0),
                P(3, 2, 0, 0, 1, 0, 0),
            };
            LevelPathSearch.Route b1 = RouteTo(LevelPathSearch.FromCell(portals, 1, 95, 0), 2);
            Assert.Equal(1, b1.firstPortal);
            Assert.Equal(5f + Stair, b1.cost, 3);
        }

        [Fact]
        public void UnlinkedMapsAreMissingAndTheStartIsNeverARoute()
        {
            var portals = new[]
            {
                P(1, 1, 0, 0, 2, 0, 0),
                P(2, 2, 0, 0, 1, 0, 0),
                P(3, 5, 0, 0, 6, 0, 0),
            };
            List<LevelPathSearch.Route> routes = LevelPathSearch.FromMap(portals, 1);
            Assert.Single(routes);
            Assert.Equal(2, routes[0].map);
        }

        [Fact]
        public void FiveFloorsDeepFindsEveryLevel()
        {
            var list = new List<LevelPathSearch.Portal>();
            int id = 1;
            for (int map = 1; map < 6; map++)
            {
                list.Add(P(id++, map, 10, 10, map + 1, 10, 10));
                list.Add(P(id++, map + 1, 10, 10, map, 10, 10));
            }
            List<LevelPathSearch.Route> routes = LevelPathSearch.FromMap(list.ToArray(), 1);
            Assert.Equal(5, routes.Count);
            Assert.Equal(5, RouteTo(routes, 6).hops);
            Assert.Equal(5 * Stair, RouteTo(routes, 6).cost, 3);
        }

        [Fact]
        public void BlockedLegIsSkippedAndRealCostIsUsed()
        {
            // Same two shafts as CheaperShaftWinsOverFewerStairs, but shaft B
            // lands in a walled-off room on B1, and shaft A's landing cannot walk
            // to shaft B's B2 stair either. Shaft A wins, at the pathfinder's cost.
            var portals = new[]
            {
                P(10, 1, 0, 0, 2, 0, 0),
                P(11, 2, 0, 0, 1, 0, 0),
                P(12, 2, 200, 0, 3, 200, 0),
                P(13, 3, 200, 0, 2, 200, 0),
                P(20, 1, 50, 0, 2, 50, 0),
                P(21, 2, 50, 0, 1, 50, 0),
                P(22, 2, 52, 0, 3, 52, 0),
                P(23, 3, 52, 0, 2, 52, 0),
            };
            float Leg(int u, int v)
            {
                if ((u == 4 && v != 5) || (u == 0 && v == 6))
                {
                    return float.PositiveInfinity;
                }
                if (u == 0 && v == 2)
                {
                    return 230f;
                }
                return LevelPathSearch.Walk(portals[u].arrivalX, portals[u].arrivalZ, portals[v].x, portals[v].z);
            }
            List<LevelPathSearch.Route> routes = LevelPathSearch.FromMap(portals, 1, Leg);
            LevelPathSearch.Route b2 = RouteTo(routes, 3);
            Assert.Equal(0, b2.firstPortal);     // index of portal 10
            Assert.Equal(Stair + 230f + Stair, b2.cost, 3);
        }
    }
}

using System.Collections.Generic;
using Xunit;

namespace Niceties.Tests
{
    /// <summary>
    /// Text grid: '.' walkable, '#' wall, 'B' the frame being finished (a blocker),
    /// 'S' stairs (an exit, walkable), 'F' an unfinished frame (walkable for the fill).
    /// </summary>
    internal struct TextGrid : IEncloseGrid
    {
        private readonly string[] rows;

        /// <summary>The rows are surrounded by a 12-cell walkable margin, so "outside" is open.</summary>
        public TextGrid(params string[] rows)
        {
            const int margin = 12;
            int w = rows[0].Length + 2 * margin;
            var padded = new List<string>();
            for (int i = 0; i < margin; i++)
            {
                padded.Add(new string('.', w));
            }

            foreach (string row in rows)
            {
                padded.Add(new string('.', margin) + row + new string('.', margin));
            }

            for (int i = 0; i < margin; i++)
            {
                padded.Add(new string('.', w));
            }

            this.rows = padded.ToArray();
            Margin = margin;
        }

        public int Margin { get; }

        public int Width => rows[0].Length;

        public int Height => rows.Length;

        public char At(int index) => rows[index / Width][index % Width];

        public bool Walkable(int index) => At(index) != '#';

        public bool IsExit(int index) => At(index) == 'S';

        public List<int> Blockers()
        {
            var list = new List<int>();
            for (int i = 0; i < Width * Height; i++)
            {
                if (At(i) == 'B')
                {
                    list.Add(i);
                }
            }

            return list;
        }

        public int Index(int x, int z) => (z + Margin) * Width + x + Margin;
    }

    public class EncloseFillTests
    {
        private static List<int> Closed(TextGrid grid, int maxRegion = 80, EncloseScratch? scratch = null)
        {
            var closed = new List<int>();
            EncloseFill.ClosedRegions(ref grid, grid.Blockers(), maxRegion, scratch ?? new EncloseScratch(), closed);
            return closed;
        }

        [Fact]
        public void LastWallOfARingClosesTheInside()
        {
            var grid = new TextGrid(
                ".......",
                ".#####.",
                ".#...#.",
                ".#...B.",
                ".#...#.",
                ".#####.",
                ".......");
            List<int> closed = Closed(grid);
            Assert.Equal(9, closed.Count);
            Assert.Contains(grid.Index(3, 3), closed);
            Assert.DoesNotContain(grid.Index(6, 3), closed);
        }

        [Fact]
        public void GapElsewhereLeavesItOpen()
        {
            var grid = new TextGrid(
                ".......",
                ".##.##.",
                ".#...#.",
                ".#...B.",
                ".#...#.",
                ".#####.",
                ".......");
            Assert.Empty(Closed(grid));
        }

        /// <summary>A square room of the given inside size, walls 1 thick, frame on the east wall.</summary>
        private static TextGrid Room(int side)
        {
            var rows = new List<string>();
            for (int z = 0; z < side + 2; z++)
            {
                var row = new char[side + 2];
                for (int x = 0; x < side + 2; x++)
                {
                    bool wall = x == 0 || x == side + 1 || z == 0 || z == side + 1;
                    row[x] = wall ? (x == side + 1 && z == 1 ? 'B' : '#') : '.';
                }

                rows.Add(new string(row));
            }

            return new TextGrid(rows.ToArray());
        }

        [Theory]
        [InlineData(8, true)]
        [InlineData(9, false)]
        public void RoomSizeAgainstTheDefaultLimit(int side, bool closedExpected)
        {
            // An 8×8 room is 64 cells (closed under 80); 9×9 is 81 (counts as open).
            Assert.Equal(closedExpected ? side * side : 0, Closed(Room(side)).Count);
        }

        [Fact]
        public void BiggerLimitCatchesBiggerRooms()
        {
            Assert.Empty(Closed(Room(10)));
            Assert.Equal(100, Closed(Room(10), 120).Count);
        }

        [Fact]
        public void BuilderCellInsideTheRingIsInTheClosedArea()
        {
            var grid = new TextGrid(
                ".....",
                ".###.",
                ".#.B.",
                ".###.",
                ".....");
            List<int> closed = Closed(grid);
            Assert.Equal(new[] { grid.Index(2, 2) }, closed);
        }

        [Fact]
        public void UnfinishedFrameDoesNotCloseARing()
        {
            // Replace Stuff: the other frame (F) is still walkable for the fill, so the
            // room is not closed until it is finished as well.
            var grid = new TextGrid(
                ".......",
                ".##F##.",
                ".#...#.",
                ".#...B.",
                ".#####.",
                ".......");
            Assert.Empty(Closed(grid));
        }

        [Fact]
        public void InPlaceSwapOfAWallCellKeepsTheRoomAsItWas()
        {
            // Swapping a finished wall for another in place: the frame sits in the ring,
            // and the inside was already closed, so finishing changes nothing outside.
            var grid = new TextGrid(
                ".......",
                ".#####.",
                ".#...B.",
                ".#####.",
                ".......");
            List<int> closed = Closed(grid);
            Assert.Equal(3, closed.Count);
            Assert.DoesNotContain(grid.Index(5, 2), closed);
        }

        [Fact]
        public void StairsInsideCountAsAWayOut()
        {
            var grid = new TextGrid(
                ".......",
                ".#####.",
                ".#.S.#.",
                ".#...B.",
                ".#####.",
                ".......");
            Assert.Empty(Closed(grid));
        }

        [Fact]
        public void ScratchIsReusableAcrossCalls()
        {
            var scratch = new EncloseScratch();
            var ring = new TextGrid(
                ".....",
                ".###.",
                ".#.B.",
                ".###.",
                ".....");
            var open = new TextGrid(
                ".....",
                ".#.#.",
                ".#.B.",
                ".###.",
                ".....");
            for (int i = 0; i < 5; i++)
            {
                Assert.Single(Closed(ring, scratch: scratch));
                Assert.Empty(Closed(open, scratch: scratch));
            }
        }

        [Fact]
        public void TwoStartsInOneOpenAreaStayOpen()
        {
            // A long corridor past the cap: both sides of the frame lead into it.
            var grid = new TextGrid(
                "#####################",
                "#..................B#",
                "#..................##",
                "#####################");
            Assert.Empty(Closed(grid, 20));
        }

        [Fact]
        public void StepAsidePicksNearestThenLowestZThenLowestX()
        {
            var xs = new List<int> { 5, 3, 4, 4 };
            var zs = new List<int> { 4, 4, 3, 5 };
            var reach = new List<bool> { true, true, true, true };
            // From (4,4): all four are at distance 1. Lowest z is (4,3).
            Assert.Equal(2, EncloseFill.PickStepAside(4, 4, xs, zs, reach));
            reach[2] = false;
            // Next: z = 4, lowest x is (3,4).
            Assert.Equal(1, EncloseFill.PickStepAside(4, 4, xs, zs, reach));
        }

        [Fact]
        public void StepAsideWithNothingReachableReturnsMinusOne()
        {
            Assert.Equal(-1, EncloseFill.PickStepAside(0, 0, new List<int> { 1 }, new List<int> { 1 },
                new List<bool> { false }));
        }
    }
}

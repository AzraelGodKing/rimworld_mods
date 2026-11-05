using System.Collections.Generic;
using Verse;
using Xunit;

namespace Strata.Tests
{
    // A seed must always carve the same cave. These hashes were taken from
    // 3.13 geometry; a change here means saves that regenerate a level, and
    // multiplayer clients, would get a different layout.
    public class CavernPlanTests
    {
        private const int Size = 250;

        public static IEnumerable<object[]> WarrenSeeds()
        {
            yield return new object[] { 1, WarrenHash1 };
            yield return new object[] { 424242, WarrenHash2 };
            yield return new object[] { -987654321, WarrenHash3 };
        }

        public static IEnumerable<object[]> VaultSeeds()
        {
            yield return new object[] { 7, VaultHash1 };
            yield return new object[] { 20261105, VaultHash2 };
        }

        private const ulong WarrenHash1 = 6720052326409140014UL;
        private const ulong WarrenHash2 = 2159399295599356146UL;
        private const ulong WarrenHash3 = 10862897124035721262UL;
        private const ulong VaultHash1 = 17312176187587935438UL;
        private const ulong VaultHash2 = 1008080241339271639UL;

        [Theory]
        [MemberData(nameof(WarrenSeeds))]
        public void WarrenIsStableForSeed(int seed, ulong expected)
        {
            StrataCavernPlan.Result plan = StrataCavernPlan.PlanWarren(
                Size, Size, new IntVec3(125, 0, 125), targetChambers: 9, arrivalRadius: 6f,
                minChamberRadius: 4f, maxChamberRadius: 9f, edgeMargin: 8, seed: seed);
            Assert.Equal(expected, Hash(plan));
        }

        [Theory]
        [MemberData(nameof(VaultSeeds))]
        public void VaultIsStableForSeed(int seed, ulong expected)
        {
            StrataCavernPlan.Result plan = StrataCavernPlan.PlanVault(
                Size, Size, new IntVec3(60, 0, 190), targetChambers: 4,
                minChamberRadius: 5f, maxChamberRadius: 8f, edgeMargin: 10, seed: seed);
            Assert.Equal(expected, Hash(plan));
        }

        [Fact]
        public void SameSeedSameCave()
        {
            StrataCavernPlan.Result a = StrataCavernPlan.PlanWarren(Size, Size, new IntVec3(100, 0, 80), 7, 5f, 3f, 8f, 8, 99);
            StrataCavernPlan.Result b = StrataCavernPlan.PlanWarren(Size, Size, new IntVec3(100, 0, 80), 7, 5f, 3f, 8f, 8, 99);
            Assert.Equal(Hash(a), Hash(b));
        }

        [Fact]
        public void CaveStaysInsideTheEdgeMarginAndCarvesTheArrival()
        {
            const int margin = 8;
            var start = new IntVec3(125, 0, 125);
            StrataCavernPlan.Result plan = StrataCavernPlan.PlanWarren(Size, Size, start, 9, 6f, 4f, 9f, margin, 31337);
            Assert.True(plan.carveMask[start.z * Size + start.x]);
            Assert.True(plan.chambers.Count > 1);
            for (int z = 0; z < Size; z++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (plan.carveMask[z * Size + x])
                    {
                        Assert.InRange(x, 1, Size - 2);
                        Assert.InRange(z, 1, Size - 2);
                    }
                }
            }
        }

        internal static ulong Hash(StrataCavernPlan.Result plan)
        {
            unchecked
            {
                ulong h = 14695981039346656037UL;
                void Mix(int v)
                {
                    h ^= (uint)v;
                    h *= 1099511628211UL;
                }
                for (int i = 0; i < plan.carveMask.Length; i++)
                {
                    if (plan.carveMask[i])
                    {
                        Mix(i);
                    }
                }
                foreach (IntVec3 c in plan.chambers)
                {
                    Mix(c.x);
                    Mix(c.z);
                }
                return h;
            }
        }
    }
}

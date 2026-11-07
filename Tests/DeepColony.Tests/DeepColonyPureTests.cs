using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DeepColony.Tests
{
    public class TickPhaseTests
    {
        [Fact]
        public void OffsetsAreUniqueAndInsideTheInterval()
        {
            int[] all = TickPhaseOffsets.All;
            Assert.Equal(all.Length, all.Distinct().Count());
            Assert.All(all, o => Assert.InRange(o, 0, TickPhaseOffsets.Interval - 1));
        }

        [Fact]
        public void AllListsEveryOffsetConstant()
        {
            int constants = typeof(TickPhaseOffsets)
                .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Count(f => f.IsLiteral && f.Name != nameof(TickPhaseOffsets.Interval));
            Assert.Equal(constants, TickPhaseOffsets.All.Length);
        }
    }

    public class TouchComfortTests
    {
        public static IEnumerable<object[]> Thresholds() =>
            new[] { 0.1f, 0.2f, 0.5f, 0.75f, 0.9f, 1f }.Select(t => new object[] { t });

        [Theory]
        [MemberData(nameof(Thresholds))]
        public void TierMinsRiseWithTier(float threshold)
        {
            float last = -1f;
            for (var tier = TouchComfortTier.Distant; tier <= TouchComfortTier.Intimate; tier++)
            {
                float min = TouchComfortMath.TierMin(tier, threshold);
                Assert.True(min > last, $"{tier} min {min} not above {last}");
                Assert.InRange(min, 0f, TouchComfortMath.ComfortMax);
                last = min;
            }
        }

        [Theory]
        [MemberData(nameof(Thresholds))]
        public void TierOfItsOwnMinIsThatTier(float threshold)
        {
            for (var tier = TouchComfortTier.Distant; tier <= TouchComfortTier.Intimate; tier++)
                Assert.Equal(tier, TouchComfortMath.TierOf(TouchComfortMath.TierMin(tier, threshold), threshold));
        }

        [Fact]
        public void FineWithTouchStartsAtTheClampedThreshold()
        {
            Assert.Equal(0.5f, TouchComfortMath.TierMin(TouchComfortTier.FineWithTouch, 0.5f));
            Assert.Equal(0.2f, TouchComfortMath.TierMin(TouchComfortTier.FineWithTouch, 0.05f));
            Assert.Equal(0.9f, TouchComfortMath.TierMin(TouchComfortTier.FineWithTouch, 1f));
        }

        [Theory]
        [InlineData(-200f, 0.4f)]
        [InlineData(0f, 0.7f)]
        [InlineData(100f, 1.6f)]
        [InlineData(200f, 1.6f)]
        public void OpinionMultiplierIsClamped(float opinion, float expected)
        {
            Assert.Equal(expected, TouchComfortMath.OpinionMultiplier(opinion), 3);
        }

        [Theory]
        [InlineData(1f, 0.5f)]
        [InlineData(3f, 0.6f)]
        [InlineData(7f, 0.75f)]
        public void SharingARoomReachesTheThresholdInTheSetDays(float days, float threshold)
        {
            const int interval = 2500;
            float gain = TouchComfortMath.GainPerInterval(days, threshold, interval);
            float neutralOpinion = (1f - 0.7f) / 0.009f; // multiplier 1
            float comfort = 0f;
            int checks = (int)System.Math.Round(days * 60000f / interval);
            for (int i = 0; i < checks; i++)
                comfort = TouchComfortMath.NextComfort(comfort, false, true, neutralOpinion, 1f, gain);
            Assert.Equal(threshold, comfort, 3);
        }

        [Fact]
        public void NextComfortStaysInRange()
        {
            Assert.Equal(TouchComfortMath.ComfortMax,
                TouchComfortMath.NextComfort(0.99f, true, true, 100f, 5f, 0.5f));
            Assert.Equal(0f, TouchComfortMath.NextComfort(0.001f, true, true, -50f, 1f, 0.5f));
            Assert.Equal(0f, TouchComfortMath.NextComfort(0f, false, false, 0f, 1f, 0.5f));
        }

        [Fact]
        public void AdjacencyGainsFasterAndDislikeLosesComfort()
        {
            float gain = 0.01f;
            float room = TouchComfortMath.NextComfort(0.3f, false, true, 50f, 1f, gain);
            float adjacent = TouchComfortMath.NextComfort(0.3f, true, true, 50f, 1f, gain);
            float apart = TouchComfortMath.NextComfort(0.3f, false, false, 50f, 1f, gain);
            float disliked = TouchComfortMath.NextComfort(0.3f, true, true, -10f, 1f, gain);
            Assert.True(adjacent > room && room > 0.3f);
            Assert.True(apart < 0.3f);
            Assert.True(disliked < apart);
            // A disliked pawn in the same room but not adjacent changes nothing.
            Assert.Equal(0.3f, TouchComfortMath.NextComfort(0.3f, false, true, -10f, 1f, gain));
        }
    }

    public class FactionRepTests
    {
        [Theory]
        [InlineData(0, 0f, 0f, FactionAttitude.Neutral)]
        [InlineData(-75, 0f, 0f, FactionAttitude.Vengeful)]
        [InlineData(0, -6f, 0f, FactionAttitude.Vengeful)]
        [InlineData(-74, -5.9f, 0f, FactionAttitude.Wary)]
        [InlineData(-40, 0f, 0f, FactionAttitude.Wary)]
        [InlineData(0, -2f, 20f, FactionAttitude.Wary)]
        [InlineData(20, 0f, 8f, FactionAttitude.Indebted)]
        [InlineData(19, 0f, 8f, FactionAttitude.Neutral)]
        [InlineData(60, 0f, 8f, FactionAttitude.Indebted)]
        [InlineData(40, 0f, 7.9f, FactionAttitude.Cordial)]
        [InlineData(39, 0f, 0f, FactionAttitude.Neutral)]
        public void AttitudeThresholds(int goodwill, float raids, float giftTrade, FactionAttitude expected)
        {
            Assert.Equal(expected, FactionRepMath.Classify(goodwill, raids, giftTrade));
        }

        [Fact]
        public void LedgerMergesSameFactionReasonAndSignWithinADay()
        {
            const int now = 100000;
            Assert.True(Merge(1, FactionRepReason.Gift, 2f, now - 60000, 1, FactionRepReason.Gift, 1f, now));
            Assert.False(Merge(1, FactionRepReason.Gift, 2f, now - 60001, 1, FactionRepReason.Gift, 1f, now));
            Assert.False(Merge(1, FactionRepReason.Gift, 2f, now, 2, FactionRepReason.Gift, 1f, now));
            Assert.False(Merge(1, FactionRepReason.Gift, 2f, now, 1, FactionRepReason.Raid, 1f, now));
            Assert.False(Merge(1, FactionRepReason.Raid, -2f, now, 1, FactionRepReason.Raid, 1f, now));
            Assert.True(Merge(1, FactionRepReason.Raid, -2f, now, 1, FactionRepReason.Raid, -1f, now));
        }

        private static bool Merge(int ef, FactionRepReason er, float ea, int et, int f, FactionRepReason r, float a, int now) =>
            FactionRepMath.ShouldMerge(ef, er, ea, et, f, r, a, now);
    }
}

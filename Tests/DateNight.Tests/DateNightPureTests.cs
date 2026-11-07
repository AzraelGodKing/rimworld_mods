using System.Collections.Generic;
using Xunit;

namespace DateNight.Tests
{
    public class CoupleKeyTests
    {
        [Theory]
        [InlineData(1, 2)]
        [InlineData(500, 7)]
        [InlineData(0, int.MaxValue)]
        [InlineData(-3, 9)]
        public void KeyIsSymmetric(int a, int b)
        {
            Assert.Equal(DateNightPure.CoupleKey(a, b), DateNightPure.CoupleKey(b, a));
        }

        [Fact]
        public void KeyPacksLowerIdHigh()
        {
            Assert.Equal((7L << 32) | 500L, DateNightPure.CoupleKey(500, 7));
        }

        [Fact]
        public void DifferentCouplesDoNotCollide()
        {
            var seen = new HashSet<long>();
            for (int a = 0; a < 40; a++)
            {
                for (int b = a + 1; b < 40; b++)
                {
                    Assert.True(seen.Add(DateNightPure.CoupleKey(a, b)));
                }
            }
        }

        [Fact]
        public void NegativeLowIdDoesNotSignExtend()
        {
            long key = DateNightPure.CoupleKey(5, -1);
            Assert.Equal(-1, (int)(key >> 32));
            Assert.Equal(5u, (uint)key);
        }
    }

    public class MilestoneTests
    {
        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(4, false)]
        [InlineData(5, true)]
        [InlineData(6, false)]
        [InlineData(10, true)]
        [InlineData(25, true)]
        [InlineData(-5, false)]
        public void EveryFifthYear(int years, bool expected)
        {
            Assert.Equal(expected, DateNightPure.IsMilestoneYear(years));
        }
    }

    public class GroupActivityTests
    {
        [Theory]
        [InlineData(DateActivity.Walk, DateActivity.Hangout)]
        [InlineData(DateActivity.Gift, DateActivity.Hangout)]
        [InlineData(DateActivity.Dinner, DateActivity.Dinner)]
        [InlineData(DateActivity.Dance, DateActivity.Dance)]
        [InlineData(DateActivity.Stargaze, DateActivity.Stargaze)]
        [InlineData(DateActivity.Fishing, DateActivity.Fishing)]
        public void WalksAndGiftsBecomeHangouts(DateActivity input, DateActivity expected)
        {
            Assert.Equal(expected, DateNightPure.CoerceForGroup(input));
        }

        [Fact]
        public void ActivityNumbersAreAppendOnly()
        {
            // Saves store these numbers; changing one breaks old saves.
            Assert.Equal(1, (int)DateActivity.Hangout);
            Assert.Equal(7, (int)DateActivity.Gift);
            Assert.Equal(8, (int)DateActivity.Recreation);
            Assert.Equal(9, (int)DateActivity.Fishing);
        }
    }

    public class RapportTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(5, 5)]
        [InlineData(6, 5)]
        [InlineData(100, 5)]
        [InlineData(-5, -5)]
        [InlineData(-6, -5)]
        [InlineData(int.MinValue, -5)]
        public void Clamped(int input, int expected)
        {
            Assert.Equal(expected, DateNightPure.ClampRapport(input));
        }
    }

    public class QualityScoreTests
    {
        private static DateQualityInputs Plain()
        {
            return new DateQualityInputs { beauty = 1f, mutualOpinion = 30, impressiveness = -1f };
        }

        [Fact]
        public void PlainDateScoresZero()
        {
            Assert.Equal(0, DateNightPure.QualityScore(Plain()));
        }

        [Fact]
        public void EachInputMovesScoreByItsWeight()
        {
            DateQualityInputs q = Plain();
            q.special = true; Assert.Equal(1, DateNightPure.QualityScore(q)); q = Plain();
            q.beauty = 4f; Assert.Equal(1, DateNightPure.QualityScore(q)); q = Plain();
            q.beauty = -0.1f; Assert.Equal(-1, DateNightPure.QualityScore(q)); q = Plain();
            q.rainedOn = true; Assert.Equal(-1, DateNightPure.QualityScore(q)); q = Plain();
            q.anniversary = true; Assert.Equal(2, DateNightPure.QualityScore(q)); q = Plain();
            q.favorite = true; Assert.Equal(1, DateNightPure.QualityScore(q)); q = Plain();
            q.mutualOpinion = 60; Assert.Equal(1, DateNightPure.QualityScore(q)); q = Plain();
            q.mutualOpinion = -1; Assert.Equal(-1, DateNightPure.QualityScore(q)); q = Plain();
            q.kind = true; Assert.Equal(1, DateNightPure.QualityScore(q)); q = Plain();
            q.abrasive = true; Assert.Equal(-1, DateNightPure.QualityScore(q)); q = Plain();
            q.fineMeal = true; Assert.Equal(1, DateNightPure.QualityScore(q)); q = Plain();
            q.impressiveness = 65f; Assert.Equal(1, DateNightPure.QualityScore(q)); q = Plain();
            q.impressiveness = 64.9f; Assert.Equal(0, DateNightPure.QualityScore(q)); q = Plain();
            q.roll = -1; Assert.Equal(-1, DateNightPure.QualityScore(q));
        }

        [Fact]
        public void HigherOpinionNeverLowersScore()
        {
            DateQualityInputs q = Plain();
            int last = int.MinValue;
            for (int opinion = -100; opinion <= 100; opinion += 5)
            {
                q.mutualOpinion = opinion;
                int score = DateNightPure.QualityScore(q);
                Assert.True(score >= last);
                last = score;
            }
        }

        [Fact]
        public void FineDinnerInImpressiveRoomIsWonderful()
        {
            DateQualityInputs q = Plain();
            q.special = true;
            q.fineMeal = true;
            q.impressiveness = 80f;
            q.roll = -1;
            Assert.True(DateNightPure.QualityScore(q) >= 2);
        }
    }

    public class ActivityWeightTests
    {
        [Fact]
        public void NoTasteIsUniform()
        {
            foreach (DateActivity a in System.Enum.GetValues(typeof(DateActivity)))
            {
                Assert.Equal(1f, DateNightPure.ActivityWeight(a, default));
            }
        }

        [Fact]
        public void TraitsSkewTowardTheirActivities()
        {
            Assert.True(DateNightPure.ActivityWeight(DateActivity.Dinner, new CoupleTaste { gourmand = true }) > 1f);
            Assert.True(DateNightPure.ActivityWeight(DateActivity.Stargaze, new CoupleTaste { nightOwl = true }) > 1f);
            Assert.True(DateNightPure.ActivityWeight(DateActivity.Walk, new CoupleTaste { ascetic = true }) > 1f);
            Assert.True(DateNightPure.ActivityWeight(DateActivity.Picnic, new CoupleTaste { artistic = true }) > 1f);
            Assert.True(DateNightPure.ActivityWeight(DateActivity.Fishing, new CoupleTaste { animals = true }) > 1f);
            Assert.True(DateNightPure.ActivityWeight(DateActivity.Dinner, new CoupleTaste { ascetic = true }) < 1f);
        }

        [Fact]
        public void WeightNeverDropsBelowFloor()
        {
            var taste = new CoupleTaste { ascetic = true };
            Assert.True(DateNightPure.ActivityWeight(DateActivity.Dinner, taste) >= 0.25f);
        }
    }

    public class GiftGiverTests
    {
        [Theory]
        [InlineData(3, 9)]
        [InlineData(9, 3)]
        [InlineData(100, 101)]
        public void ExactlyOnePartnerGivesEachDay(int a, int b)
        {
            for (int day = 0; day < 6; day++)
            {
                Assert.NotEqual(DateNightPure.IsGiftGiver(a, b, day), DateNightPure.IsGiftGiver(b, a, day));
            }
        }

        [Fact]
        public void GiverAlternatesByDay()
        {
            Assert.True(DateNightPure.IsGiftGiver(3, 9, 0));
            Assert.False(DateNightPure.IsGiftGiver(3, 9, 1));
            Assert.True(DateNightPure.IsGiftGiver(9, 3, 1));
        }
    }

    public class GiftOrderTests
    {
        [Fact]
        public void OrderIsOrdinalDistinctAndTrimmed()
        {
            List<string> ordered = DateNightPure.OrderedGiftNames(
                new string?[] { " Chocolate", "Beer", "beer", null, "", "Ambrosia", "Beer ", "InsectJelly" });
            Assert.Equal(new[] { "Ambrosia", "Beer", "Chocolate", "InsectJelly", "beer" }, ordered);
        }

        [Fact]
        public void InputOrderDoesNotMatter()
        {
            var a = DateNightPure.OrderedGiftNames(new[] { "PsychiteTea", "Beer", "Chocolate" });
            var b = DateNightPure.OrderedGiftNames(new[] { "Chocolate", "PsychiteTea", "Beer" });
            Assert.Equal(a, b);
        }
    }

    public class ProposalTests
    {
        [Theory]
        [InlineData(-1, 1f)]
        [InlineData(0, 1f)]
        [InlineData(1, 1.25f)]
        [InlineData(4, 2f)]
        [InlineData(9, 2f)]
        public void StreakRaisesWeightUpToDouble(int streak, float expected)
        {
            Assert.Equal(expected, DateNightPure.ProposalMultiplier(streak), 3);
        }
    }
}

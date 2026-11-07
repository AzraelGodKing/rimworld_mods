using System;
using System.Collections.Generic;
using LivingWorld;
using Xunit;

namespace LivingWorld.Tests
{
    public class BattleOddsTests
    {
        [Fact]
        public void EvenFactionsAreACoinFlip()
        {
            Assert.Equal(0.5f, LivingWorldRules.BattleOddsA(3, 3, 0, 0), 3);
        }

        [Fact]
        public void BiggerFactionWinsMoreOften()
        {
            Assert.True(LivingWorldRules.BattleOddsA(6, 2, 0, 0) > 0.5f);
            Assert.True(LivingWorldRules.BattleOddsA(2, 6, 0, 0) < 0.5f);
        }

        [Fact]
        public void ProsperityTipsAnEvenFight()
        {
            Assert.True(LivingWorldRules.BattleOddsA(3, 3, 4, -4) > 0.5f);
        }

        [Theory]
        [InlineData(100, 0, 8, -8)]
        [InlineData(0, 100, -8, 8)]
        [InlineData(0, 0, -100, -100)]
        public void OddsStayInsideTheUpsetBand(int a, int b, int pa, int pb)
        {
            float odds = LivingWorldRules.BattleOddsA(a, b, pa, pb);
            Assert.InRange(odds, LivingWorldRules.MinBattleOdds, LivingWorldRules.MaxBattleOdds);
        }

        [Fact]
        public void OddsAreSymmetric()
        {
            float ab = LivingWorldRules.BattleOddsA(5, 2, 1, -1);
            float ba = LivingWorldRules.BattleOddsA(2, 5, -1, 1);
            Assert.Equal(1f, ab + ba, 3);
        }
    }

    public class ConquestTests
    {
        [Theory]
        [InlineData(0, 3, false, false)]
        [InlineData(1, 3, false, false)]
        [InlineData(2, 3, false, true)]
        [InlineData(3, 1, false, true)]
        [InlineData(3, 1, true, false)]
        [InlineData(3, 2, true, true)]
        [InlineData(3, 0, false, false)]
        public void OnlyHardWarsTakeSettlementsAndProtectedKeepTheirLast(
            int intensity, int loserSettlements, bool loserProtected, bool expected)
        {
            Assert.Equal(expected, LivingWorldRules.ConquestAllowed(intensity, loserSettlements, loserProtected));
        }
    }

    public class SeedToneTests
    {
        [Fact]
        public void HostileStartsTenseNotAtWar()
        {
            LivingWorldRules.SeedTone(true, false, out int tone, out int intensity);
            Assert.Equal(1, tone);
            Assert.Equal(2, intensity);
        }

        [Fact]
        public void HostileWinsOverAllied()
        {
            LivingWorldRules.SeedTone(true, true, out int tone, out _);
            Assert.Equal(1, tone);
        }

        [Fact]
        public void AlliesStartAllied()
        {
            LivingWorldRules.SeedTone(false, true, out int tone, out int intensity);
            Assert.Equal(3, tone);
            Assert.Equal(1, intensity);
        }

        [Fact]
        public void OthersStartAtPeace()
        {
            LivingWorldRules.SeedTone(false, false, out int tone, out int intensity);
            Assert.Equal(0, tone);
            Assert.Equal(0, intensity);
        }
    }

    public class ToneTransitionTests
    {
        [Theory]
        [InlineData(0.01f, 1f, true, LivingWorldRules.PeaceOutcome.Alliance)]
        [InlineData(0.01f, 1f, false, LivingWorldRules.PeaceOutcome.Tension)]
        [InlineData(0.10f, 1f, true, LivingWorldRules.PeaceOutcome.Tension)]
        [InlineData(0.30f, 1f, true, LivingWorldRules.PeaceOutcome.None)]
        [InlineData(0.30f, 2f, true, LivingWorldRules.PeaceOutcome.Tension)]
        public void Peace(float roll, float rate, bool canAlly, LivingWorldRules.PeaceOutcome expected)
        {
            Assert.Equal(expected, LivingWorldRules.Peace(roll, rate, canAlly));
        }

        [Fact]
        public void NoAllianceForPermanentEnemiesAtAnyRoll()
        {
            for (float roll = 0f; roll < 1f; roll += 0.005f)
            {
                Assert.NotEqual(LivingWorldRules.PeaceOutcome.Alliance, LivingWorldRules.Peace(roll, 2f, canAlly: false));
            }
        }

        [Theory]
        [InlineData(0.10f, 1f, LivingWorldRules.TensionOutcome.CoolOff)]
        [InlineData(0.40f, 1f, LivingWorldRules.TensionOutcome.Escalate)]
        [InlineData(0.60f, 1f, LivingWorldRules.TensionOutcome.Skirmish)]
        [InlineData(0.90f, 1f, LivingWorldRules.TensionOutcome.None)]
        public void Tension(float roll, float rate, LivingWorldRules.TensionOutcome expected)
        {
            Assert.Equal(expected, LivingWorldRules.Tension(roll, rate));
        }

        [Theory]
        [InlineData(2, false, 1)]
        [InlineData(1, false, 0)]
        [InlineData(0, false, 0)]
        [InlineData(1, true, 1)]
        [InlineData(3, true, 2)]
        public void CoolOff(int intensity, bool hostile, int expected)
        {
            Assert.Equal(expected, LivingWorldRules.CoolOffIntensity(intensity, hostile));
        }

        [Theory]
        [InlineData(0.10f, 1f, LivingWorldRules.WarOutcome.WhitePeace)]
        [InlineData(0.50f, 1f, LivingWorldRules.WarOutcome.Battle)]
        [InlineData(0.90f, 1f, LivingWorldRules.WarOutcome.None)]
        // Low war rate still fights at the 0.35 floor: 0.2 + 0.55 * 0.35 = 0.3925.
        [InlineData(0.38f, 0.1f, LivingWorldRules.WarOutcome.Battle)]
        [InlineData(0.40f, 0.1f, LivingWorldRules.WarOutcome.None)]
        public void War(float roll, float rate, LivingWorldRules.WarOutcome expected)
        {
            Assert.Equal(expected, LivingWorldRules.War(roll, rate));
        }

        [Theory]
        [InlineData(0.05f, 1f, true, LivingWorldRules.AllianceOutcome.Betrayal)]
        [InlineData(0.15f, 1f, true, LivingWorldRules.AllianceOutcome.Dissolve)]
        [InlineData(0.50f, 1f, true, LivingWorldRules.AllianceOutcome.None)]
        [InlineData(0.50f, 1f, false, LivingWorldRules.AllianceOutcome.Lapse)]
        [InlineData(0.01f, 1f, false, LivingWorldRules.AllianceOutcome.Lapse)]
        public void Alliance(float roll, float rate, bool canAlly, LivingWorldRules.AllianceOutcome expected)
        {
            Assert.Equal(expected, LivingWorldRules.Alliance(roll, rate, canAlly));
        }
    }

    public class PairKeyTests
    {
        [Theory]
        [InlineData(1, 2)]
        [InlineData(0, int.MaxValue)]
        [InlineData(-1, 5)]
        [InlineData(7, 7)]
        public void KeyIsOrderIndependent(int a, int b)
        {
            Assert.Equal(LivingWorldRules.PairKey(a, b), LivingWorldRules.PairKey(b, a));
        }

        [Fact]
        public void DifferentPairsGetDifferentKeys()
        {
            var keys = new HashSet<long>();
            for (int a = 0; a < 40; a++)
            {
                for (int b = a + 1; b < 40; b++)
                {
                    Assert.True(keys.Add(LivingWorldRules.PairKey(a, b)));
                }
            }
        }

        [Theory]
        [InlineData(5, 3, 3, 5)]
        [InlineData(3, 5, 3, 5)]
        [InlineData(4, 4, 4, 4)]
        public void NormalizeIds(int a, int b, int lo, int hi)
        {
            LivingWorldRules.NormalizeIds(ref a, ref b);
            Assert.Equal(lo, a);
            Assert.Equal(hi, b);
        }
    }

    public class RumourTests
    {
        [Theory]
        [InlineData(WorldEventKind.DecisiveVictory, WorldEventKind.Skirmish)]
        [InlineData(WorldEventKind.Skirmish, WorldEventKind.DecisiveVictory)]
        [InlineData(WorldEventKind.OwnershipFlip, WorldEventKind.Skirmish)]
        [InlineData(WorldEventKind.Betrayal, WorldEventKind.Betrayal)]
        [InlineData(WorldEventKind.Collapse, WorldEventKind.Collapse)]
        public void DistortKind(WorldEventKind real, WorldEventKind reported)
        {
            Assert.Equal((int)reported, LivingWorldRules.DistortKind((int)real));
        }
    }

    public class EvictTests
    {
        private sealed class Entry
        {
            public int Id;
            public bool Pending;
        }

        private static List<Entry> Make(int n, Func<int, bool> pending)
        {
            var list = new List<Entry>();
            for (int i = 0; i < n; i++)
            {
                list.Add(new Entry { Id = i, Pending = pending(i) });
            }
            return list;
        }

        [Fact]
        public void UnderCapacityKeepsEverything()
        {
            List<Entry> chronicle = Make(5, _ => true);
            var evicted = new List<Entry>();
            LivingWorldRules.Evict(chronicle, 10, evicted, 4, e => e.Pending);
            Assert.Equal(5, chronicle.Count);
            Assert.Empty(evicted);
        }

        [Fact]
        public void OldestLeaveFirstAndPendingOnesAreKept()
        {
            List<Entry> chronicle = Make(10, i => i % 2 == 0);
            var evicted = new List<Entry>();
            LivingWorldRules.Evict(chronicle, 6, evicted, 10, e => e.Pending);
            Assert.Equal(6, chronicle.Count);
            Assert.Equal(4, chronicle[0].Id);
            Assert.Equal(new[] { 0, 2 }, evicted.ConvertAll(e => e.Id));
        }

        [Fact]
        public void EvictedListIsCappedOldestFirst()
        {
            List<Entry> chronicle = Make(12, _ => true);
            var evicted = new List<Entry>();
            LivingWorldRules.Evict(chronicle, 2, evicted, 3, e => e.Pending);
            Assert.Equal(new[] { 7, 8, 9 }, evicted.ConvertAll(e => e.Id));
        }

        [Fact]
        public void EmptyChronicleIsFine()
        {
            var chronicle = new List<Entry>();
            var evicted = new List<Entry>();
            LivingWorldRules.Evict(chronicle, 0, evicted, 0, e => e.Pending);
            Assert.Empty(chronicle);
        }
    }

    public class BudgetTests
    {
        [Fact]
        public void FirstCallOnlyRecordsThePeriod()
        {
            bool init = false;
            int q = 0, y = -1, letters = 5, morphs = 3;
            LivingWorldRules.RollBudgets(2, 5501, ref init, ref q, ref y, ref letters, ref morphs);
            Assert.True(init);
            Assert.Equal(2, q);
            Assert.Equal(5501, y);
            Assert.Equal(5, letters);
            Assert.Equal(3, morphs);
        }

        [Fact]
        public void NewQuadrumResetsLettersOnly()
        {
            bool init = true;
            int q = 1, y = 5501, letters = 5, morphs = 3;
            LivingWorldRules.RollBudgets(2, 5501, ref init, ref q, ref y, ref letters, ref morphs);
            Assert.Equal(0, letters);
            Assert.Equal(3, morphs);
        }

        [Fact]
        public void NewYearResetsBoth()
        {
            bool init = true;
            int q = 3, y = 5501, letters = 5, morphs = 3;
            LivingWorldRules.RollBudgets(0, 5502, ref init, ref q, ref y, ref letters, ref morphs);
            Assert.Equal(0, letters);
            Assert.Equal(0, morphs);
        }

        [Fact]
        public void SamePeriodKeepsCounters()
        {
            bool init = true;
            int q = 2, y = 5501, letters = 5, morphs = 3;
            LivingWorldRules.RollBudgets(2, 5501, ref init, ref q, ref y, ref letters, ref morphs);
            Assert.Equal(5, letters);
            Assert.Equal(3, morphs);
        }
    }

    /// <summary>Saves store these enums by member name: renaming one breaks saves.</summary>
    public class SavedEnumNameTests
    {
        [Fact]
        public void WorldEventKindNamesAreFrozen()
        {
            Assert.Equal(new[]
            {
                "Skirmish", "DecisiveVictory", "FamineRumor", "Founding", "Collapse", "Alliance", "Betrayal",
                "OwnershipFlip", "ProsperityRise", "ProsperityFall", "OutpostFounded", "SettlementAbandoned",
                "Epithet", "WhitePeace", "RefugeeFlight", "TradeBlackout", "WarbandPass", "Correction",
                "TradeCaravan", "CaravanDiverted",
            }, Enum.GetNames(typeof(WorldEventKind)));
        }

        [Fact]
        public void HearChannelAndSeverityNamesAreFrozen()
        {
            Assert.Equal(new[] { "Rumour", "Proximity", "Radio" }, Enum.GetNames(typeof(HearChannel)));
            Assert.Equal(new[] { "Minor", "Normal", "Major" }, Enum.GetNames(typeof(NewsSeverity)));
        }
    }
}

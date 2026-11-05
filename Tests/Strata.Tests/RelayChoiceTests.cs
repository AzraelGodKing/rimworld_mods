using Xunit;

namespace Strata.Tests
{
    public class RelayChoiceTests
    {
        [Fact]
        public void BetterPriorityWithinDepthQualifies()
        {
            Assert.True(RelayChoice.Qualifies(localPriority: 4, remotePriority: 1, depth: 1, maxDepth: 2, emergencyTrip: false));
        }

        [Fact]
        public void EqualPriorityNeverQualifiesSoFloorsDoNotPingPong()
        {
            Assert.False(RelayChoice.Qualifies(3, 3, 1, 2, false));
        }

        [Fact]
        public void TooFarDoesNotQualify()
        {
            Assert.False(RelayChoice.Qualifies(4, 1, 3, 2, false));
        }

        [Fact]
        public void ZeroMaxDepthTurnsPriorityTripsOff()
        {
            Assert.False(RelayChoice.Qualifies(4, 1, 1, 0, false));
        }

        [Fact]
        public void EmergencyQualifiesAtAnyPriorityAndDepth()
        {
            Assert.True(RelayChoice.Qualifies(1, 4, 6, 0, true));
        }

        [Fact]
        public void FirstCandidateWins()
        {
            Assert.True(RelayChoice.Wins(false, false, 4, 5, false, 0, 0));
        }

        [Fact]
        public void EmergencyBeatsBetterPriority()
        {
            Assert.True(RelayChoice.Wins(true, true, 4, 3, bestEmergency: false, bestPriority: 1, bestDepth: 1));
            Assert.False(RelayChoice.Wins(true, false, 1, 1, bestEmergency: true, bestPriority: 4, bestDepth: 3));
        }

        [Fact]
        public void BetterPriorityThenNearerFloorWins()
        {
            Assert.True(RelayChoice.Wins(true, false, 1, 2, false, 2, 1));
            Assert.True(RelayChoice.Wins(true, false, 2, 1, false, 2, 2));
            Assert.False(RelayChoice.Wins(true, false, 2, 2, false, 2, 2));
        }
    }
}

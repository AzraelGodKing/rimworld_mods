using Xunit;

namespace Strata.Tests
{
    public class AtmosphereDepthTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void SurfaceAndB1BreatheSurfaceAir(int depth)
        {
            Assert.Equal(AtmosphereDepth.OxygenFraction, AtmosphereDepth.OxygenForDepth(depth));
        }

        [Fact]
        public void EachLevelBelowB1LosesElevenPercent()
        {
            Assert.Equal(0.2095f * 0.89f, AtmosphereDepth.OxygenForDepth(2), 5);
            Assert.Equal(0.2095f * 0.78f, AtmosphereDepth.OxygenForDepth(3), 5);
        }

        [Fact]
        public void OxygenNeverDropsBelowTheDeepFloor()
        {
            for (int depth = 2; depth < 40; depth++)
            {
                float o2 = AtmosphereDepth.OxygenForDepth(depth);
                Assert.True(o2 >= AtmosphereDepth.MinDeepOxygenFraction);
                Assert.True(o2 <= AtmosphereDepth.OxygenForDepth(depth - 1));
            }
        }
    }
}

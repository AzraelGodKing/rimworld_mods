using Xunit;

namespace Strata.Tests
{
    // Cave plans use System.Random(seed). On 1.6 that is .NET Framework (Mono)
    // and on 1.7 it is CoreCLR. A seeded Random on CoreCLR keeps the old
    // Framework algorithm; this checks it against a copy of that algorithm, so
    // the same seed carves the same cave on both game versions.
    public class SeededRandomTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(99)]
        [InlineData(424242)]
        [InlineData(-987654321)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void SeededRandomMatchesFrameworkAlgorithm(int seed)
        {
            var runtime = new System.Random(seed);
            var framework = new FrameworkRandom(seed);
            for (int i = 0; i < 2000; i++)
            {
                switch (i % 4)
                {
                    case 0:
                        Assert.Equal(framework.NextDouble(), runtime.NextDouble());
                        break;
                    case 1:
                        Assert.Equal(framework.Next(), runtime.Next());
                        break;
                    case 2:
                        Assert.Equal(framework.Next(-50, 400), runtime.Next(-50, 400));
                        break;
                    default:
                        Assert.Equal(framework.Next(1000), runtime.Next(1000));
                        break;
                }
            }
        }

        // The .NET Framework 4.x System.Random (Knuth's subtractive generator).
        private sealed class FrameworkRandom
        {
            private const int MBig = int.MaxValue;
            private const int MSeed = 161803398;

            private readonly int[] seedArray = new int[56];
            private int inext;
            private int inextp;

            public FrameworkRandom(int seed)
            {
                int subtraction = seed == int.MinValue ? int.MaxValue : System.Math.Abs(seed);
                int mj = MSeed - subtraction;
                seedArray[55] = mj;
                int mk = 1;
                for (int i = 1; i < 55; i++)
                {
                    int ii = (21 * i) % 55;
                    seedArray[ii] = mk;
                    mk = mj - mk;
                    if (mk < 0)
                    {
                        mk += MBig;
                    }
                    mj = seedArray[ii];
                }
                for (int k = 1; k < 5; k++)
                {
                    for (int i = 1; i < 56; i++)
                    {
                        seedArray[i] -= seedArray[1 + (i + 30) % 55];
                        if (seedArray[i] < 0)
                        {
                            seedArray[i] += MBig;
                        }
                    }
                }
                inext = 0;
                inextp = 21;
            }

            private int InternalSample()
            {
                int locINext = inext + 1;
                int locINextp = inextp + 1;
                if (locINext >= 56)
                {
                    locINext = 1;
                }
                if (locINextp >= 56)
                {
                    locINextp = 1;
                }
                int retVal = seedArray[locINext] - seedArray[locINextp];
                if (retVal == MBig)
                {
                    retVal--;
                }
                if (retVal < 0)
                {
                    retVal += MBig;
                }
                seedArray[locINext] = retVal;
                inext = locINext;
                inextp = locINextp;
                return retVal;
            }

            private double Sample() => InternalSample() * (1.0 / MBig);

            public int Next() => InternalSample();

            public int Next(int maxValue) => (int)(Sample() * maxValue);

            public int Next(int minValue, int maxValue) => (int)(Sample() * ((long)maxValue - minValue)) + minValue;

            public double NextDouble() => Sample();
        }
    }
}

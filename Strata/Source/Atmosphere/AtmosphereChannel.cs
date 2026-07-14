using UnityEngine;

namespace Strata
{
    // Room-density gas channels. Smoke keeps the original tuning; toxic and natural
    // gas reuse the same ventilation/dispersion rules so vents, ducts, and shafts
    // work on every channel without special cases.
    public enum AtmosphereChannel
    {
        Smoke = 0,
        Toxic = 1,
        NaturalGas = 2,
    }

    public static class AtmosphereChannelUtility
    {
        public static readonly AtmosphereChannel[] All =
        {
            AtmosphereChannel.Smoke,
            AtmosphereChannel.Toxic,
            AtmosphereChannel.NaturalGas,
        };

        public static string Label(AtmosphereChannel channel)
        {
            switch (channel)
            {
                case AtmosphereChannel.Toxic: return "toxic gas";
                case AtmosphereChannel.NaturalGas: return "natural gas";
                default: return "smoke";
            }
        }

        public static float HarmThreshold(AtmosphereChannel channel)
        {
            switch (channel)
            {
                case AtmosphereChannel.Toxic: return 0.12f;
                case AtmosphereChannel.NaturalGas: return 0.25f;
                default: return 0.15f;
            }
        }

        public static float SeverityGain(AtmosphereChannel channel)
        {
            switch (channel)
            {
                case AtmosphereChannel.Toxic: return 0.008f;
                case AtmosphereChannel.NaturalGas: return 0.004f;
                default: return 0.006f;
            }
        }

        public static float IgnitionDensity(AtmosphereChannel channel)
        {
            return channel == AtmosphereChannel.NaturalGas ? 0.38f : 1f;
        }

        public static Color OverlayColor(AtmosphereChannel channel, float density)
        {
            float a = Mathf.Clamp01(density);
            switch (channel)
            {
                case AtmosphereChannel.Toxic:
                    return new Color(0.15f, 0.45f, 0.12f, a * 0.85f);
                case AtmosphereChannel.NaturalGas:
                    return new Color(0.55f, 0.48f, 0.08f, a * 0.75f);
                default:
                    return new Color(0.04f, 0.04f, 0.05f, a);
            }
        }
    }
}

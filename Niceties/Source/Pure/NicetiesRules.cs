#nullable enable

namespace Niceties
{
    /// <summary>
    /// Game-free rules shared by the mod and Tests/Niceties.Tests. Nothing here may
    /// reference Verse or RimWorld types.
    /// </summary>
    public static class NicetiesRules
    {
        /// <summary>Quality index as RimWorld's QualityCategory: Awful = 0 … Legendary = 6.</summary>
        public static float WearChanceForQuality(int quality)
        {
            switch (quality)
            {
                case 0:
                    return 1f;
                case 1:
                    return 0.8f;
                case 2:
                    return 0.45f;
                case 3:
                    return 0.2f;
                case 4:
                    return 0.08f;
                case 5:
                case 6:
                    return 0f;
                default:
                    return 0.45f;
            }
        }

        /// <summary>
        /// Chance that a day's deterioration damage lands on worn apparel.
        /// <paramref name="craftingLevel"/> is -1 when the bonus is off or the skill is disabled.
        /// </summary>
        public static float DailyWearChance(bool qualityScaling, bool protectCorpse, int quality,
            int craftingLevel, bool wearerDead)
        {
            if (wearerDead && !protectCorpse)
            {
                return 1f;
            }

            if (!qualityScaling)
            {
                return 0f;
            }

            float chance = WearChanceForQuality(quality);
            if (craftingLevel >= 0)
            {
                int level = craftingLevel > 20 ? 20 : craftingLevel;
                chance *= 1f - (level / 20f) * 0.5f;
            }

            return chance < 0f ? 0f : (chance > 1f ? 1f : chance);
        }
    }
}

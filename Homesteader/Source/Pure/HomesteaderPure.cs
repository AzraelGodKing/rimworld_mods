#nullable enable
using System;
using System.Collections.Generic;

namespace Homesteader
{
    /// <summary>Maple sap seasons, without the game's Season type (AZR-578).</summary>
    internal enum SapSeason
    {
        Spring,
        Summer,
        Fall,
        Winter,
    }

    /// <summary>
    /// Food allergy defName tokens by allergy id, matched by
    /// <see cref="HomesteaderPure.NameMatchesAllergy"/> (AZR-528, AZR-578).
    /// </summary>
    internal static class AllergyTokens
    {
        internal static readonly string[] Milk = new[] { "Milk", "Cheese", "Butter", "Cream", "Yogurt" };
        internal static readonly string[] Eggs = new[] { "Egg" };
        internal static readonly string[] Peanuts = new[] { "Peanut" };
        internal static readonly string[] TreeNuts = new[] { "Nuts", "Walnut", "Almond", "Cashew", "Pecan", "Hazelnut" };
        internal static readonly string[] Wheat = new[] { "Wheat", "Flour", "Bread", "Biscuit", "Toast", "Pasta", "Hardtack" };
        internal static readonly string[] Soy = new[] { "Soy", "Tofu", "Edamame" };
        internal static readonly string[] Fish = new[] { "Fish" };
        internal static readonly string[] Shellfish = new[] { "Shellfish", "Shrimp", "Crab", "Lobster", "Oyster", "Clam", "Mussel" };
        internal static readonly string[] Sesame = new[] { "Sesame", "Tahini" };
    }

    /// <summary>
    /// Homesteader rules that need no game types, so Tests/Homesteader.Tests can run
    /// them without RimWorld (AZR-578). The game code calls these; keep them free of
    /// Verse, RimWorld and Unity.
    /// </summary>
    internal static class HomesteaderPure
    {
        /// <summary>
        /// Match an allergy token on word or camelCase boundaries, so "Egg" does not hit
        /// Veggie* or Eggplant, "Nuts" does not hit Donuts, "Crab" does not hit Crabapple
        /// (AZR-528). A trailing plural "s" still matches.
        /// </summary>
        internal static bool DefNameContainsToken(string? defName, string? token)
        {
            if (defName == null || token == null || defName.Length == 0 || token.Length == 0)
            {
                return false;
            }

            int idx = 0;
            while (idx <= defName.Length - token.Length)
            {
                int found = defName.IndexOf(token, idx, StringComparison.OrdinalIgnoreCase);
                if (found < 0)
                {
                    return false;
                }

                bool startOk = found == 0
                    || !char.IsLetter(defName[found - 1])
                    || (char.IsLower(defName[found - 1]) && char.IsUpper(defName[found]));
                int end = found + token.Length;
                // A plural "s" is part of the word: "ScrambledEggs" is eggs, "Walnuts" is walnut.
                if (end < defName.Length && defName[end] == 's' && !token.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                {
                    end++;
                }

                bool endOk = end >= defName.Length
                    || !char.IsLetter(defName[end])
                    || char.IsUpper(defName[end]);
                if (startOk && endOk)
                {
                    return true;
                }

                idx = found + 1;
            }

            return false;
        }

        /// <summary>
        /// The defName-token part of an allergy match. Peanuts are their own allergy,
        /// so TreeNuts never claims a Peanut* def.
        /// </summary>
        internal static bool NameMatchesAllergy(string allergyId, string defName, string[]? tokens)
        {
            if (tokens == null)
            {
                return false;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                if (!DefNameContainsToken(defName, tokens[i]))
                {
                    continue;
                }

                if (allergyId == "TreeNuts" && DefNameContainsToken(defName, "Peanut"))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>Saved taste lists are '|'-joined defNames.</summary>
        internal static string Pack(List<string>? list)
        {
            return list == null || list.Count == 0 ? string.Empty : string.Join("|", list);
        }

        internal static List<string> Unpack(string? packed)
        {
            var list = new List<string>();
            if (packed == null || packed.Length == 0)
            {
                return list;
            }

            string[] parts = packed.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    list.Add(parts[i]);
                }
            }

            return list;
        }

        /// <summary>Aging tier: how many of the tier thresholds (in days) have passed.</summary>
        internal static int AgingTier(float agedDays, IList<float>? tierDays)
        {
            int tier = 0;
            if (tierDays == null)
            {
                return tier;
            }

            for (int i = 0; i < tierDays.Count; i++)
            {
                if (agedDays >= tierDays[i])
                {
                    tier = i + 1;
                }
            }

            return tier;
        }

        /// <summary>Market value factor for a tier; the last factor covers any higher tier.</summary>
        internal static float AgingValueFactor(int tier, IList<float>? factors)
        {
            if (factors == null || factors.Count == 0)
            {
                return 1f;
            }

            return factors[Math.Min(Math.Max(tier, 0), factors.Count - 1)];
        }

        /// <summary>Maple sap runs hard in cool spring weather, slows in fall and winter, and nearly stops in summer.</summary>
        internal static float MapleYieldFactor(SapSeason season, float outdoorTemp)
        {
            switch (season)
            {
                case SapSeason.Spring:
                    return outdoorTemp < 12f ? 1.45f : 1.15f;
                case SapSeason.Winter:
                    return outdoorTemp < 5f ? 0.85f : 0.55f;
                case SapSeason.Fall:
                    return 0.7f;
                default:
                    return outdoorTemp > 22f ? 0.12f : 0.28f;
            }
        }

        /// <summary>Well-stocked larder stage at 3, 6 and 9 preserve kinds; -1 = no thought.</summary>
        internal static int LarderStage(int preserveKinds)
        {
            return preserveKinds >= 9 ? 2 : preserveKinds >= 6 ? 1 : preserveKinds >= 3 ? 0 : -1;
        }

        /// <summary>Festival stage from the pantry; pumpkin pie or cider lifts it one step, up to the top.</summary>
        internal static int FestivalStage(int preserveKinds, bool seasonalFood)
        {
            int stage = preserveKinds >= 6 ? 2 : preserveKinds >= 3 ? 1 : 0;
            return seasonalFood && stage < 2 ? stage + 1 : stage;
        }
    }
}

#nullable enable
using System;
using System.Collections.Generic;

namespace DateNight
{
    public enum DateActivity
    {
        Unresolved = 0,
        Hangout = 1,
        Dinner = 2,
        Picnic = 3,
        Walk = 4,
        Stargaze = 5,
        Dance = 6,
        Gift = 7,
        Recreation = 8,
        // Append only: saves store the number (AZR-559).
        Fishing = 9,
    }

    /// <summary>Everything the date quality score looks at (AZR-559).</summary>
    public struct DateQualityInputs
    {
        public bool special;
        public float beauty;
        public bool rainedOn;
        public int roll;
        public bool anniversary;
        public bool favorite;
        public int mutualOpinion;
        public bool kind;
        public bool abrasive;
        public bool fineMeal;
        /// <summary>Room impressiveness, or -1 outdoors.</summary>
        public float impressiveness;
    }

    /// <summary>What a couple likes: either partner has the trait or passion (AZR-559).</summary>
    public partial struct CoupleTaste
    {
        public bool gourmand;
        public bool ascetic;
        public bool nightOwl;
        public bool artistic;
        public bool animals;
        public bool kind;
        public bool abrasive;
    }

    /// <summary>
    /// Date Night rules with no game types, so Tests/DateNight.Tests can run them
    /// headless (AZR-575). Game code calls these; keep them free of Verse/Unity.
    /// </summary>
    public static class DateNightPure
    {
        public const int MinRapport = -5;
        public const int MaxRapport = 5;
        public const int MilestoneEveryYears = 5;
        public const int MaxProposalStreak = 4;
        public const float ProposalPerWonderfulDate = 0.25f;

        /// <summary>Order-free couple key: lower id in the high 32 bits.</summary>
        public static long CoupleKey(int x, int y)
        {
            if (x > y)
            {
                int tmp = x;
                x = y;
                y = tmp;
            }
            return ((long)x << 32) | (uint)y;
        }

        /// <summary>Lower id gives on even days, the other partner on odd days.</summary>
        public static bool IsGiftGiver(int pawnId, int partnerId, int day)
        {
            return (pawnId < partnerId) == (day % 2 == 0);
        }

        public static bool IsMilestoneYear(int years)
        {
            return years >= MilestoneEveryYears && years % MilestoneEveryYears == 0;
        }

        /// <summary>
        /// 2+ is wonderful, -1 or less awkward. Opinion, traits, a fine meal and an
        /// impressive room joined beauty, weather and luck in 2.0.0 (AZR-559).
        /// </summary>
        public static int QualityScore(in DateQualityInputs q)
        {
            int score = q.roll;
            if (q.special) score++;
            if (q.beauty >= 4f) score++;
            else if (q.beauty < 0f) score--;
            if (q.rainedOn) score--;
            if (q.anniversary) score += 2;
            if (q.favorite) score++;
            if (q.mutualOpinion >= 60) score++;
            else if (q.mutualOpinion < 0) score--;
            if (q.kind) score++;
            if (q.abrasive) score--;
            if (q.fineMeal) score++;
            if (q.impressiveness >= 65f) score++;
            return score;
        }

        /// <summary>Pick weight from the couple's tastes; 1 for anything they have no taste for.</summary>
        public static float ActivityWeight(DateActivity activity, CoupleTaste taste)
        {
            float w = 1f;
            switch (activity)
            {
                case DateActivity.Dinner:
                    if (taste.gourmand) w += 1.5f;
                    if (taste.ascetic) w -= 0.5f;
                    break;
                case DateActivity.Picnic:
                    if (taste.artistic) w += 1f;
                    if (taste.gourmand) w += 0.5f;
                    break;
                case DateActivity.Walk:
                    if (taste.ascetic) w += 1f;
                    break;
                case DateActivity.Stargaze:
                    if (taste.ascetic) w += 1f;
                    if (taste.nightOwl) w += 1.5f;
                    break;
                case DateActivity.Fishing:
                    if (taste.animals) w += 1f;
                    break;
            }
            return w < 0.25f ? 0.25f : w;
        }

        /// <summary>A walk or a gift does not work for four; they hang out instead.</summary>
        public static DateActivity CoerceForGroup(DateActivity activity)
        {
            if (activity == DateActivity.Walk || activity == DateActivity.Gift)
            {
                return DateActivity.Hangout;
            }
            return activity;
        }

        public static int ClampRapport(int score)
        {
            return score < MinRapport ? MinRapport : score > MaxRapport ? MaxRapport : score;
        }

        /// <summary>Proposal weight factor: +25% per wonderful date in a row, up to +100%.</summary>
        public static float ProposalMultiplier(int wonderfulStreak)
        {
            int n = wonderfulStreak < 0 ? 0 : wonderfulStreak > MaxProposalStreak ? MaxProposalStreak : wonderfulStreak;
            return 1f + ProposalPerWonderfulDate * n;
        }

        /// <summary>
        /// Distinct, trimmed, ordinal-sorted gift defNames. HashSet order differs
        /// per process on CoreCLR, so the order never comes from the set (AZR-540).
        /// </summary>
        public static List<string> OrderedGiftNames(IEnumerable<string?> names)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<string>();
            foreach (string? raw in names)
            {
                if (raw == null)
                {
                    continue;
                }
                string name = raw.Trim();
                if (name.Length > 0 && seen.Add(name))
                {
                    list.Add(name);
                }
            }
            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }
}

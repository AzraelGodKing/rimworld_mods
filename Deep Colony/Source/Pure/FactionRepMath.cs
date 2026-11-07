#nullable enable
namespace DeepColony
{
    public enum FactionRepReason : byte
    {
        Other = 0,
        Raid = 1,
        SharedEnemyRaid = 2,
        TradeCaravan = 3,
        SuccessfulTrade = 4,
        Gift = 5,
        SharedKill = 6,
        Grudge = 7,
        LwVictory = 8,
        LwBetrayal = 9,
        LwRefugee = 10,
        IdleAlly = 11,
        IdleEnemy = 12,
        Envoy = 13,
        Debug = 14,
        Tribute = 15,
        EnvoyVisit = 16,
        EnvoyPresent = 17,
        FamilyDefect = 18
    }

    public enum FactionAttitude : byte
    {
        Neutral = 0,
        Cordial = 1,
        Indebted = 2,
        Wary = 3,
        Vengeful = 4
    }

    /// <summary>Faction reputation rules with no game types, so logic tests can run them.</summary>
    internal static class FactionRepMath
    {
        internal const int AggregateWindowTicks = 60000; // merge same reason within a day

        /// <summary>Attitude from goodwill, the raid ledger sum and the gift + trade ledger sum.</summary>
        internal static FactionAttitude Classify(int goodwill, float raidSum, float giftTradeSum)
        {
            if (goodwill <= -75 || raidSum <= -6f)
                return FactionAttitude.Vengeful;
            if (goodwill <= -40 || raidSum <= -2f)
                return FactionAttitude.Wary;
            if (giftTradeSum >= 8f && goodwill >= 20)
                return FactionAttitude.Indebted;
            if (goodwill >= 40)
                return FactionAttitude.Cordial;
            return FactionAttitude.Neutral;
        }

        /// <summary>
        /// A new ledger amount folds into an existing entry for the same faction and
        /// reason when that entry is under a day old and has the same sign.
        /// </summary>
        internal static bool ShouldMerge(
            int entryFactionId, FactionRepReason entryReason, float entryAmount, int entryTick,
            int factionId, FactionRepReason reason, float amount, int now)
        {
            if (entryFactionId != factionId || entryReason != reason) return false;
            if (now - entryTick > AggregateWindowTicks) return false;
            return (entryAmount >= 0f) == (amount >= 0f);
        }
    }
}

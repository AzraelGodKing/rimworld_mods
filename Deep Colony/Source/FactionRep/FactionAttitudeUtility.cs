using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>B12 — derive attitude from goodwill + ledger; consequences settings-gated.</summary>
    public static class FactionAttitudeUtility
    {
        public static FactionAttitude GetAttitude(Faction faction)
        {
            if (faction == null || faction.IsPlayer || faction.defeated)
                return FactionAttitude.Neutral;

            int goodwill = faction.GoodwillWith(Faction.OfPlayer);
            var gc = GameComp_DeepColony.Instance;
            float raidSum = gc?.SumLedger(faction, FactionRepReason.Raid) ?? 0f;
            float giftTrade = (gc?.SumLedger(faction, FactionRepReason.Gift) ?? 0f)
                + (gc?.SumLedger(faction, FactionRepReason.SuccessfulTrade) ?? 0f)
                + (gc?.SumLedger(faction, FactionRepReason.TradeCaravan) ?? 0f);

            if (goodwill <= -75 || raidSum <= -6f)
                return FactionAttitude.Vengeful;
            if (goodwill <= -40 || raidSum <= -2f)
                return FactionAttitude.Wary;
            if (giftTrade >= 8f && goodwill >= 20)
                return FactionAttitude.Indebted;
            if (goodwill >= 40)
                return FactionAttitude.Cordial;
            return FactionAttitude.Neutral;
        }

        public static string AttitudeLabel(FactionAttitude attitude)
        {
            return ("DC_Attitude_" + attitude).Translate();
        }

        public static bool ConsequencesActive =>
            DeepColonySettings.Get.enableFactionRep
            && DeepColonySettings.Get.enableAttitudeConsequences;

        /// <summary>Buy price multiplier (player pays). &gt;1 = worse for player.</summary>
        public static float TradeBuyFactor(Faction faction)
        {
            if (!ConsequencesActive) return 1f;
            switch (GetAttitude(faction))
            {
                case FactionAttitude.Indebted: return 0.92f;
                case FactionAttitude.Cordial: return 0.96f;
                case FactionAttitude.Wary: return 1.08f;
                case FactionAttitude.Vengeful: return 1.18f;
                default: return 1f;
            }
        }

        /// <summary>Sell price multiplier (player receives). &lt;1 = worse for player.</summary>
        public static float TradeSellFactor(Faction faction)
        {
            if (!ConsequencesActive) return 1f;
            switch (GetAttitude(faction))
            {
                case FactionAttitude.Indebted: return 1.08f;
                case FactionAttitude.Cordial: return 1.04f;
                case FactionAttitude.Wary: return 0.92f;
                case FactionAttitude.Vengeful: return 0.85f;
                default: return 1f;
            }
        }

        public static float RaidPointsFactor(Faction faction)
        {
            if (!ConsequencesActive) return 1f;
            switch (GetAttitude(faction))
            {
                case FactionAttitude.Vengeful: return 1.15f;
                case FactionAttitude.Wary: return 1.05f;
                case FactionAttitude.Indebted: return 0.85f;
                case FactionAttitude.Cordial: return 0.90f;
                default: return 1f;
            }
        }

        public static bool ShouldBlockTraderCaravan(Faction faction)
        {
            if (!ConsequencesActive || faction == null) return false;
            FactionAttitude a = GetAttitude(faction);
            if (a == FactionAttitude.Vengeful) return true;
            if (a == FactionAttitude.Wary) return Rand.Chance(0.35f);
            return false;
        }

        public static bool ShouldRefuseTrade(Faction faction)
        {
            if (!ConsequencesActive || faction == null) return false;
            return GetAttitude(faction) == FactionAttitude.Vengeful
                && faction.GoodwillWith(Faction.OfPlayer) <= -60
                && Rand.Chance(0.25f);
        }
    }
}

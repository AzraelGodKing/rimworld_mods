using RimWorld;
using Verse;

namespace DeepColony
{
    public static class FactionRepUtility
    {
        public static void OnRaidFromFaction(Faction raider)
        {
            if (!DeepColonySettings.Get.enableFactionRep) return;
            if (raider == null || raider.IsPlayer) return;

            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                if (other.IsPlayer || other == raider) continue;
                if (other.RelationKindWith(raider) == FactionRelationKind.Hostile)
                    GameComp_DeepColony.Instance?.AddFactionDrift(other, 1f, FactionRepReason.SharedEnemyRaid);
            }

            GameComp_DeepColony.Instance?.AddFactionDrift(raider, -2f, FactionRepReason.Raid);
            GrudgeUtility.OnRaidFromFaction(raider);
        }

        public static void OnTradeCaravanFromFaction(Faction trader)
        {
            if (!DeepColonySettings.Get.enableFactionRep) return;
            if (trader == null || trader.IsPlayer) return;
            GameComp_DeepColony.Instance?.AddFactionDrift(trader, 1f, FactionRepReason.TradeCaravan);
        }

        public static void OnSuccessfulTrade(Faction trader)
        {
            if (!DeepColonySettings.Get.enableFactionRep) return;
            if (trader == null || trader.IsPlayer) return;
            GameComp_DeepColony.Instance?.AddFactionDrift(trader, 2f, FactionRepReason.SuccessfulTrade);

            Pawn envoy = FactionEnvoyUtility.FindEnvoy(trader);
            if (envoy == null || envoy.Dead || !envoy.Spawned) return;
            Map tradeMap = TradeSession.playerNegotiator?.Map ?? Find.CurrentMap;
            if (tradeMap != null && envoy.Map == tradeMap)
            {
                GameComp_DeepColony.Instance?.AddFactionDrift(trader, 1f, FactionRepReason.EnvoyPresent);
            }
        }

        public static void OnGiftFromFaction(Faction giver)
        {
            if (!DeepColonySettings.Get.enableFactionRep) return;
            if (giver == null || giver.IsPlayer) return;
            GameComp_DeepColony.Instance?.AddFactionDrift(giver, 3f, FactionRepReason.Gift);
        }

        /// <summary>
        /// Player killed someone from <paramref name="victimFaction"/> — factions hostile
        /// to that victim like you a little more (enemy of my enemy).
        /// </summary>
        public static void OnPlayerKilledHostile(Faction victimFaction)
        {
            if (!DeepColonySettings.Get.enableFactionRep) return;
            if (victimFaction == null || victimFaction.IsPlayer) return;

            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                if (other.IsPlayer || other == victimFaction) continue;
                if (other.RelationKindWith(victimFaction) == FactionRelationKind.Hostile)
                    GameComp_DeepColony.Instance?.AddFactionDrift(other, 0.35f, FactionRepReason.SharedKill);
            }
        }
    }
}

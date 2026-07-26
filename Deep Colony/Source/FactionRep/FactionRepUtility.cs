using RimWorld;
using Verse;

namespace DeepColony
{
    public static class FactionRepUtility
    {
        public static void OnRaidFromFaction(Faction raider)
        {
            if (raider == null || raider.IsPlayer) return;

            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                if (other.IsPlayer || other == raider) continue;
                if (other.RelationKindWith(raider) == FactionRelationKind.Hostile)
                    GameComp_DeepColony.Instance?.AddFactionDrift(other, 1f);
            }

            GameComp_DeepColony.Instance?.AddFactionDrift(raider, -2f);
        }

        public static void OnTradeCaravanFromFaction(Faction trader)
        {
            if (trader == null || trader.IsPlayer) return;
            GameComp_DeepColony.Instance?.AddFactionDrift(trader, 1f);
        }

        public static void OnSuccessfulTrade(Faction trader)
        {
            if (trader == null || trader.IsPlayer) return;
            GameComp_DeepColony.Instance?.AddFactionDrift(trader, 2f);
        }

        public static void OnGiftFromFaction(Faction giver)
        {
            if (giver == null || giver.IsPlayer) return;
            GameComp_DeepColony.Instance?.AddFactionDrift(giver, 3f);
        }

        /// <summary>
        /// Player killed someone from <paramref name="victimFaction"/> — factions hostile
        /// to that victim like you a little more (enemy of my enemy).
        /// </summary>
        public static void OnPlayerKilledHostile(Faction victimFaction)
        {
            if (victimFaction == null || victimFaction.IsPlayer) return;

            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                if (other.IsPlayer || other == victimFaction) continue;
                if (other.RelationKindWith(victimFaction) == FactionRelationKind.Hostile)
                    GameComp_DeepColony.Instance?.AddFactionDrift(other, 0.35f);
            }
        }
    }
}

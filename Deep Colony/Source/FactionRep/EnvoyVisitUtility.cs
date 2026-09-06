using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// C23 — settings-gated envoy visit. Dispatches a goodwill pulse to an allied
    /// settlement without forming a real caravan (those are easy to get stuck).
    /// </summary>
    public static class EnvoyVisitUtility
    {
        private const int TickInterval = 2500;
        private const float MtbDays = 12f;
        private const float BaseDrift = 3f;
        private const int MinGoodwill = 10;

        public static void GameTick()
        {
            if (!DeepColonySettings.Get.enableFactionRep) return;
            if (!DeepColonySettings.Get.enableEnvoyVisits) return;
            if (!TickPhase.Due(1670)) return;

            var gc = GameComp_DeepColony.Instance;
            if (gc == null) return;

            foreach (Pawn p in FactionEnvoyUtility.EnvoyCandidates())
            {
                if (p.Dead || p.Downed || p.Drafted) continue;
                if (!p.Spawned) continue;
                Faction faction = FactionEnvoyUtility.GetEnvoyFaction(p);
                if (faction == null || faction.defeated) continue;
                if (faction.GoodwillWith(Faction.OfPlayer) < MinGoodwill) continue;
                if (!HasAlliedSettlement(faction)) continue;

                if (!Rand.MTBEventOccurs(MtbDays, 60000f, TickInterval)) continue;

                float extra = SoftCompat.HasAnyRoyalTitle(p) ? 0.75f : 0f;
                GameComp_DeepColony.Instance?.AddFactionDrift(
                    faction, BaseDrift + extra, FactionRepReason.EnvoyVisit);
                ExternalDiplomacySoftCompat.OnEnvoyVisit(faction, p);
                gc.lastEnvoyVisitTick = Find.TickManager.TicksGame;

                string title = "DC_EnvoyVisitLabel".Translate(p.LabelShort.Named("PAWN"));
                string body = extra > 0f
                    ? "DC_EnvoyVisitBodyTitle".Translate(
                        p.LabelShort.Named("PAWN"), faction.Name.Named("FACTION"))
                    : "DC_EnvoyVisitBody".Translate(
                        p.LabelShort.Named("PAWN"), faction.Name.Named("FACTION"));
                Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.PositiveEvent, p);
                return; // one visit per interval
            }
        }

        private static bool HasAlliedSettlement(Faction faction)
        {
            if (Find.WorldObjects?.Settlements == null) return false;
            foreach (Settlement s in Find.WorldObjects.Settlements)
            {
                if (s.Faction == faction && !s.Destroyed)
                    return true;
            }
            return false;
        }
    }
}

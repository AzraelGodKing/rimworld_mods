using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>A17 — personal envoy: Social colonist assigned to a faction for soft goodwill.</summary>
    public static class FactionEnvoyUtility
    {
        private const int TickInterval = 2500;
        private const float EnvoyMtbDays = 5f;
        private const float EnvoyDrift = 0.5f;
        private const int MinSocial = 5;

        public static void GameTick()
        {
            if (!DeepColonySettings.Get.enableFactionRep) return;
            if (!TickPhase.Due(1503)) return;

            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    var comp = p.TryGetComp<Comp_DeepColony>();
                    if (comp == null || comp.envoyFactionId < 0) continue;
                    if (p.Dead || p.Downed) continue;
                    if ((p.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0) < MinSocial) continue;

                    Faction faction = FindFaction(comp.envoyFactionId);
                    if (faction == null || faction.defeated || faction.IsPlayer)
                    {
                        ClearEnvoy(p);
                        continue;
                    }

                    if (Rand.MTBEventOccurs(EnvoyMtbDays, 60000f, TickInterval))
                    {
                        float amount = EnvoyDrift;
                        if (SoftCompat.HasAnyRoyalTitle(p))
                            amount += 0.2f;
                        GameComp_DeepColony.Instance?.AddFactionDrift(
                            faction, amount, FactionRepReason.Envoy);
                    }
                }
            }
        }

        public static void NotifyPawnDied(Pawn pawn)
        {
            if (pawn == null) return;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null || comp.envoyFactionId < 0) return;

            Faction faction = FindFaction(comp.envoyFactionId);
            if (faction != null && !faction.IsPlayer)
            {
                GameComp_DeepColony.Instance?.AddFactionDrift(faction, -2f, FactionRepReason.Envoy);
                Messages.Message(
                    "DC_EnvoyDied".Translate(pawn.LabelShort.Named("PAWN"), faction.Name.Named("FACTION")),
                    pawn,
                    MessageTypeDefOf.NegativeEvent,
                    false);
            }
            comp.envoyFactionId = -1;
        }

        public static void SetEnvoy(Pawn pawn, Faction faction)
        {
            if (pawn == null || faction == null || faction.IsPlayer) return;
            if (!DeepColonySettings.Get.enableFactionRep) return;

            // One envoy per faction / one faction per pawn.
            ClearEnvoyForFaction(faction);
            ClearEnvoy(pawn);

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            comp.envoyFactionId = faction.loadID;

            Messages.Message(
                "DC_EnvoyAssigned".Translate(pawn.LabelShort.Named("PAWN"), faction.Name.Named("FACTION")),
                pawn,
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        public static void ClearEnvoy(Pawn pawn)
        {
            var comp = pawn?.TryGetComp<Comp_DeepColony>();
            if (comp != null) comp.envoyFactionId = -1;
        }

        public static void ClearEnvoyForFaction(Faction faction)
        {
            if (faction == null) return;
            foreach (Pawn p in AllPlayerColonists())
            {
                var c = p.TryGetComp<Comp_DeepColony>();
                if (c != null && c.envoyFactionId == faction.loadID)
                    c.envoyFactionId = -1;
            }
        }

        public static Pawn FindEnvoy(Faction faction)
        {
            if (faction == null) return null;
            foreach (Pawn p in AllPlayerColonists())
            {
                var c = p.TryGetComp<Comp_DeepColony>();
                if (c != null && c.envoyFactionId == faction.loadID)
                    return p;
            }
            return null;
        }

        public static Faction GetEnvoyFaction(Pawn pawn)
        {
            var comp = pawn?.TryGetComp<Comp_DeepColony>();
            if (comp == null || comp.envoyFactionId < 0) return null;
            return FindFaction(comp.envoyFactionId);
        }

        public static IEnumerable<Faction> CandidateFactions()
        {
            return Find.FactionManager.AllFactionsListForReading
                .Where(f => !f.IsPlayer && !f.defeated && !f.Hidden);
        }

        public static IEnumerable<Pawn> EnvoyCandidates()
        {
            var list = new List<Pawn>();
            foreach (Pawn p in AllPlayerColonists())
            {
                if (p.Dead || p.skills?.GetSkill(SkillDefOf.Social)?.TotallyDisabled == true)
                    continue;
                list.Add(p);
            }
            return list
                .OrderByDescending(SoftCompat.RoyalTitleSeniority)
                .ThenByDescending(p => p.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0);
        }

        private static IEnumerable<Pawn> AllPlayerColonists()
        {
            List<Pawn> found = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (found != null)
            {
                return found;
            }

            var fallback = new List<Pawn>();
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.FreeColonists == null) continue;
                fallback.AddRange(map.mapPawns.FreeColonists);
            }
            return fallback;
        }

        private static Faction FindFaction(int loadId)
        {
            if (loadId < 0) return null;
            return Find.FactionManager?.AllFactionsListForReading?.Find(f => f.loadID == loadId);
        }
    }
}

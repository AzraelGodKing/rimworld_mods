using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace DeepColony
{
    /// <summary>
    /// D17 — kin on the player map may join or defect. Spouse is the strongest pull;
    /// ex-lovers contribute 0% (they reconcile separately). Non-hostile family recruit
    /// skips the goodwill penalty; hostile kin keep the grudge via FamilyDefect drift.
    /// </summary>
    public static class FamilyJoinUtility
    {
        [System.ThreadStatic]
        private static int suppressDepth;

        public static bool SuppressGoodwillPenalty => suppressDepth > 0;

        public static void EnterSuppress() => suppressDepth++;

        public static void ExitSuppress()
        {
            if (suppressDepth > 0) suppressDepth--;
        }

        public static void GameTick()
        {
            if (!DeepColonySettings.Get.enableFamilyJoin) return;
            if (!TickPhase.Due(417)) return;
            foreach (Map map in Find.Maps)
            {
                if (map?.IsPlayerHome != true) continue;
                if (map.mapPawns?.AllPawnsSpawned == null) continue;
                IReadOnlyList<Pawn> spawned = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < spawned.Count; i++)
                    TryConsiderJoin(spawned[i]);
            }
        }

        public static void NotifySpawned(Pawn pawn)
        {
            if (!DeepColonySettings.Get.enableFamilyJoin) return;
            TryConsiderJoin(pawn);
        }

        public static bool ShouldSuppressGoodwillForJoin(Pawn pawn, Faction newFaction)
        {
            if (!DeepColonySettings.Get.enableFamilyJoin) return false;
            if (pawn == null || newFaction == null || !newFaction.IsPlayer) return false;
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike) return false;
            Faction old = pawn.Faction;
            if (old == null || old.IsPlayer) return false;
            if (IsHostileKin(pawn, old)) return false;
            return MaxKinWeight(pawn) > 0.001f;
        }

        public static float MaxKinWeight(Pawn pawn)
        {
            if (pawn?.relations == null) return 0f;
            float best = 0f;
            foreach (Pawn colonist in PlayerColonists())
            {
                if (colonist == null || colonist.Dead || colonist == pawn) continue;
                float w = KinWeight(pawn, colonist);
                if (w > best) best = w;
            }
            return best;
        }

        public static float KinWeight(Pawn a, Pawn b)
        {
            if (a?.relations == null || b?.relations == null || a == b) return 0f;
            float w = 0f;

            if (HasDirect(a, b, PawnRelationDefOf.Spouse))
                w = System.Math.Max(w, 1.00f);
            if (PawnRelationDefOf.Fiance != null && HasDirect(a, b, PawnRelationDefOf.Fiance))
                w = System.Math.Max(w, 0.80f);
            if (HasDirect(a, b, PawnRelationDefOf.Parent))
                w = System.Math.Max(w, 0.70f);
            if (PawnRelationDefOf.Lover != null && HasDirect(a, b, PawnRelationDefOf.Lover))
                w = System.Math.Max(w, 0.55f);
            if (PawnRelationDefOf.Sibling != null
                && PawnRelationDefOf.Sibling.Worker != null
                && PawnRelationDefOf.Sibling.Worker.InRelation(a, b))
                w = System.Math.Max(w, 0.50f);
            if (PawnRelationDefOf.Grandparent != null
                && PawnRelationDefOf.Grandparent.Worker != null
                && (PawnRelationDefOf.Grandparent.Worker.InRelation(a, b)
                    || PawnRelationDefOf.Grandparent.Worker.InRelation(b, a)))
                w = System.Math.Max(w, 0.30f);

            // ExLover / ExSpouse are intentionally 0% for faction join / defect.
            return w;
        }

        public static bool TryForceJoin(Pawn pawn)
        {
            if (!CanConsider(pawn)) return false;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp != null) comp.familyJoinRolled = true;
            bool hostile = IsHostileKin(pawn, pawn.Faction);
            return JoinPlayer(pawn, hostile);
        }

        private static void TryConsiderJoin(Pawn pawn)
        {
            if (!CanConsider(pawn)) return;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null || comp.familyJoinRolled) return;

            float weight = MaxKinWeight(pawn);
            if (weight <= 0.001f) return;

            comp.familyJoinRolled = true;
            bool hostile = IsHostileKin(pawn, pawn.Faction);
            var settings = DeepColonySettings.Get;
            float chance = weight * (hostile
                ? settings.familyRaidDefectChance
                : settings.familyVisitJoinChance);
            if (chance <= 0f || !Rand.Chance(chance)) return;
            JoinPlayer(pawn, hostile);
        }

        private static bool CanConsider(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned) return false;
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike) return false;
            if (pawn.Faction == null || pawn.Faction.IsPlayer) return false;
            if (pawn.Map == null || !pawn.Map.IsPlayerHome) return false;
            if (pawn.IsPrisoner || pawn.IsSlave) return false;
            if (pawn.DevelopmentalStage < DevelopmentalStage.Child) return false;
            if (pawn.Faction.leader == pawn) return false;
            if (IsQuestLodger(pawn)) return false;
            return true;
        }

        private static bool IsQuestLodger(Pawn pawn)
        {
            try { return pawn.IsQuestLodger(); }
            catch { return false; }
        }

        private static bool IsHostileKin(Pawn pawn, Faction faction)
        {
            if (faction != null && Faction.OfPlayer != null && faction.HostileTo(Faction.OfPlayer))
                return true;
            try { return GenHostility.IsActiveThreatToPlayer(pawn); }
            catch { return pawn.HostileTo(Faction.OfPlayer); }
        }

        private static bool JoinPlayer(Pawn pawn, bool hostile)
        {
            if (pawn == null) return false;
            Faction oldFaction = pawn.Faction;
            string name = pawn.LabelShort;
            string factionName = oldFaction?.Name ?? "";

            try
            {
                Lord lord = pawn.GetLord();
                lord?.Notify_PawnLost(pawn, PawnLostCondition.ChangedFaction);
            }
            catch
            {
                // Leave-lord is best-effort; Recruit still proceeds.
            }

            bool entered = ShouldSuppressGoodwillForJoin(pawn, Faction.OfPlayer);
            if (entered) EnterSuppress();
            try
            {
                RecruitToPlayer(pawn);
            }
            finally
            {
                if (entered) ExitSuppress();
            }

            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
                return false;

            if (hostile && oldFaction != null && !oldFaction.IsPlayer)
            {
                GameComp_DeepColony.Instance?.AddFactionDrift(
                    oldFaction, -2f, FactionRepReason.FamilyDefect);
            }
            ExternalDiplomacySoftCompat.OnFamilyJoin(pawn, oldFaction, hostile);

            string titleKey = hostile ? "DC_Letter_FamilyDefectLabel" : "DC_Letter_FamilyJoinLabel";
            string bodyKey = hostile ? "DC_Letter_FamilyDefectBody" : "DC_Letter_FamilyJoinBody";
            Find.LetterStack.ReceiveLetter(
                titleKey.Translate(name.Named("PAWN")),
                bodyKey.Translate(
                    name.Named("PAWN"),
                    factionName.Named("FACTION")),
                hostile ? LetterDefOf.PositiveEvent : LetterDefOf.PositiveEvent,
                pawn);
            return true;
        }

        /// <summary>God-mode / debug: drop guest status and recruit with no join letter.</summary>
        public static void ForceMakeColonist(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;
            try { pawn.guest?.SetGuestStatus(null); }
            catch { /* Guest API differs across DLC mixes. */ }

            if (pawn.Faction != null && pawn.Faction.IsPlayer) return;

            try
            {
                Lord lord = pawn.GetLord();
                lord?.Notify_PawnLost(pawn, PawnLostCondition.ChangedFaction);
            }
            catch
            {
                // Leave-lord is best-effort; Recruit still proceeds.
            }

            EnterSuppress();
            try { RecruitToPlayer(pawn); }
            finally { ExitSuppress(); }
        }

        private static void RecruitToPlayer(Pawn pawn)
        {
            Pawn recruiter = FindRecruiter(pawn);
            System.Reflection.MethodInfo recruit = AccessTools.Method(
                typeof(RecruitUtility), "Recruit",
                new[] { typeof(Pawn), typeof(Faction), typeof(Pawn) })
                ?? AccessTools.Method(
                    typeof(RecruitUtility), "Recruit",
                    new[] { typeof(Pawn), typeof(Faction) });
            if (recruit != null)
            {
                if (recruit.GetParameters().Length >= 3)
                    recruit.Invoke(null, new object[] { pawn, Faction.OfPlayer, recruiter });
                else
                    recruit.Invoke(null, new object[] { pawn, Faction.OfPlayer });
                return;
            }
            pawn.SetFaction(Faction.OfPlayer, recruiter);
        }

        private static Pawn FindRecruiter(Pawn pawn)
        {
            Pawn best = null;
            float bestW = 0f;
            foreach (Pawn colonist in PlayerColonists())
            {
                if (colonist == null || colonist.Dead) continue;
                float w = KinWeight(pawn, colonist);
                if (w > bestW)
                {
                    bestW = w;
                    best = colonist;
                }
            }
            if (best != null) return best;
            if (pawn.Map?.mapPawns?.FreeColonistsSpawned == null) return null;
            foreach (Pawn p in pawn.Map.mapPawns.FreeColonistsSpawned)
            {
                if (p != null && !p.Dead) return p;
            }
            return null;
        }

        private static bool HasDirect(Pawn a, Pawn b, PawnRelationDef def)
        {
            if (def == null) return false;
            return a.relations.DirectRelationExists(def, b)
                || b.relations.DirectRelationExists(def, a);
        }

        private static IEnumerable<Pawn> PlayerColonists()
        {
            List<Pawn> found = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (found != null)
            {
                for (int i = 0; i < found.Count; i++)
                    yield return found[i];
                yield break;
            }
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.FreeColonists == null) continue;
                foreach (Pawn p in map.mapPawns.FreeColonists)
                    yield return p;
            }
        }
    }
}

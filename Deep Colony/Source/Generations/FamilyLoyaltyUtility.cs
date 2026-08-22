using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// D19 — unwavering prisoners can have Recruitable restored by family only,
    /// and only when both like each other (mutual opinion).
    /// </summary>
    public static class FamilyLoyaltyUtility
    {
        public static bool IsUnwaveringPrisoner(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return false;
            if (!pawn.IsPrisonerOfColony) return false;
            if (pawn.guest == null) return false;
            return !pawn.guest.Recruitable;
        }

        public static bool IsFamily(Pawn a, Pawn b)
        {
            return FamilyJoinUtility.KinWeight(a, b) > 0.001f;
        }

        public static int MinOpinion =>
            Mathf.Clamp(DeepColonySettings.Get.familyUnwaveringMinOpinion, 0, 100);

        public static bool MutualOpinionGood(Pawn a, Pawn b)
        {
            if (a?.relations == null || b?.relations == null) return false;
            int min = MinOpinion;
            return a.relations.OpinionOf(b) >= min
                && b.relations.OpinionOf(a) >= min;
        }

        public static string OpinionFailReason(Pawn colonist, Pawn prisoner)
        {
            int need = MinOpinion;
            int a = colonist?.relations?.OpinionOf(prisoner) ?? 0;
            int b = prisoner?.relations?.OpinionOf(colonist) ?? 0;
            return "DC_FamilyLoyalty_LowOpinion".Translate(need, a, b);
        }

        /// <summary>Show the family-talk option: kin vs an unwavering prisoner (opinion may still fail).</summary>
        public static bool WouldShowFamilyTalk(Pawn colonist, Pawn prisoner)
        {
            if (!DeepColonySettings.Get.enableFamilyJoin) return false;
            if (colonist == null || prisoner == null || colonist == prisoner) return false;
            if (!colonist.IsColonistPlayerControlled) return false;
            if (!IsUnwaveringPrisoner(prisoner)) return false;
            if (!prisoner.Spawned || prisoner.Dead || prisoner.Downed) return false;
            if (colonist.WorkTagIsDisabled(WorkTags.Social)) return false;
            return IsFamily(colonist, prisoner);
        }

        public static bool CanAttemptBreak(Pawn colonist, Pawn prisoner, out string reason)
        {
            reason = null;
            if (!WouldShowFamilyTalk(colonist, prisoner))
            {
                reason = "DC_FamilyLoyalty_NotFamily".Translate();
                return false;
            }
            if (!MutualOpinionGood(colonist, prisoner))
            {
                reason = OpinionFailReason(colonist, prisoner);
                return false;
            }
            return true;
        }

        public static bool TryBreak(Pawn colonist, Pawn prisoner, bool force = false)
        {
            if (!force && !CanAttemptBreak(colonist, prisoner, out _))
                return false;
            if (prisoner?.guest == null) return false;
            if (prisoner.guest.Recruitable) return true;

            if (!force)
            {
                float weight = FamilyJoinUtility.KinWeight(colonist, prisoner);
                float chance = weight * DeepColonySettings.Get.familyUnwaveringBreakChance;
                chance *= OpinionFactor(colonist, prisoner);
                chance = Mathf.Clamp(chance, 0.05f, 0.95f);
                if (!Rand.Chance(chance))
                {
                    if (prisoner.Spawned && prisoner.Map != null)
                    {
                        MoteMaker.ThrowText(prisoner.DrawPos, prisoner.Map,
                            "DC_FamilyLoyalty_StillHolds".Translate(), 4f);
                    }
                    return false;
                }
            }

            prisoner.guest.Recruitable = true;
            Find.LetterStack.ReceiveLetter(
                "DC_Letter_FamilyLoyaltyLabel".Translate(),
                "DC_Letter_FamilyLoyaltyBody".Translate(
                    colonist.LabelShort.Named("FAMILY"),
                    prisoner.LabelShort.Named("PAWN")),
                LetterDefOf.PositiveEvent,
                new LookTargets(prisoner, colonist));
            return true;
        }

        public static bool TryForceBreak(Pawn prisoner)
        {
            if (!IsUnwaveringPrisoner(prisoner) || prisoner.Map == null) return false;
            Pawn family = FindWillingFamilyOnMap(prisoner);
            if (family == null) return false;
            return TryBreak(family, prisoner, force: true);
        }

        private static float OpinionFactor(Pawn colonist, Pawn prisoner)
        {
            int a = colonist.relations.OpinionOf(prisoner);
            int b = prisoner.relations.OpinionOf(colonist);
            float avg = (a + b) * 0.5f;
            int min = MinOpinion;
            if (avg <= min) return 0.75f;
            return 0.75f + Mathf.Clamp01((avg - min) / (100f - min)) * 0.50f;
        }

        private static Pawn FindWillingFamilyOnMap(Pawn prisoner)
        {
            if (prisoner.Map?.mapPawns?.FreeColonistsSpawned == null) return null;
            foreach (Pawn p in prisoner.Map.mapPawns.FreeColonistsSpawned)
            {
                if (CanAttemptBreak(p, prisoner, out _)) return p;
            }
            foreach (Pawn p in prisoner.Map.mapPawns.FreeColonistsSpawned)
            {
                if (WouldShowFamilyTalk(p, prisoner)) return p;
            }
            return null;
        }
    }
}

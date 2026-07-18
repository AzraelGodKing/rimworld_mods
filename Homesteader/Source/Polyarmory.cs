using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
    public static class PolyarmoryUtility
    {
        public const string TraitDefName = "Homesteader_Polyarmory";

        public static TraitDef Trait => DefDatabase<TraitDef>.GetNamedSilentFail(TraitDefName);

        public static bool HasPolyarmory(Pawn pawn)
        {
            TraitDef trait = Trait;
            return trait != null && pawn?.story?.traits != null && pawn.story.traits.HasTrait(trait);
        }

        /// <summary>
        /// Direct love partners, or metamours connected through love relations among polyarmory pawns.
        /// </summary>
        public static bool AreInSamePolycule(Pawn a, Pawn b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            if (a == b)
            {
                return true;
            }

            if (!HasPolyarmory(a) || !HasPolyarmory(b))
            {
                return false;
            }

            if (LovePartnerRelationUtility.LovePartnerRelationExists(a, b))
            {
                return true;
            }

            // BFS through love partners, only stepping via polyarmory pawns (polycule members).
            var visited = new HashSet<Pawn> { a };
            var queue = new Queue<Pawn>();
            queue.Enqueue(a);
            while (queue.Count > 0)
            {
                Pawn cur = queue.Dequeue();
                List<DirectPawnRelation> partners = LovePartnerRelationUtility.ExistingLovePartners(cur, allowDead: false);
                if (partners == null)
                {
                    continue;
                }

                for (int i = 0; i < partners.Count; i++)
                {
                    Pawn other = partners[i].otherPawn;
                    if (other == null || other.Dead || !visited.Add(other))
                    {
                        continue;
                    }

                    if (other == b)
                    {
                        return true;
                    }

                    if (HasPolyarmory(other))
                    {
                        queue.Enqueue(other);
                    }
                }
            }

            return false;
        }

        public static bool AcceptsSharingBedWith(Pawn pawn, Pawn other)
        {
            return AreInSamePolycule(pawn, other);
        }
    }

    /// <summary>
    /// Ideology / assignment: polyarmory polycule members are willing to share a bed (incl. Polyamory Beds).
    /// </summary>
    [HarmonyPatch(typeof(BedUtility), nameof(BedUtility.WillingToShareBed))]
    public static class Patch_Polyarmory_WillingToShareBed
    {
        public static void Postfix(Pawn pawn1, Pawn pawn2, ref bool __result)
        {
            if (__result || pawn1 == null || pawn2 == null)
            {
                return;
            }

            if (PolyarmoryUtility.AcceptsSharingBedWith(pawn1, pawn2))
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// SharedBed thought uses this — treat polycule metamours as acceptable bedmates.
    /// </summary>
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.GetMostDislikedNonPartnerBedOwner))]
    public static class Patch_Polyarmory_MostDislikedBedOwner
    {
        public static void Postfix(Pawn p, ref Pawn __result)
        {
            if (__result == null || p == null || !PolyarmoryUtility.HasPolyarmory(p))
            {
                return;
            }

            Building_Bed bed = p.ownership?.OwnedBed;
            if (bed == null)
            {
                __result = null;
                return;
            }

            Pawn worst = null;
            int worstOpinion = 0;
            List<Pawn> owners = bed.OwnersForReading;
            for (int i = 0; i < owners.Count; i++)
            {
                Pawn other = owners[i];
                if (other == p)
                {
                    continue;
                }

                if (LovePartnerRelationUtility.LovePartnerRelationExists(p, other))
                {
                    continue;
                }

                if (PolyarmoryUtility.AcceptsSharingBedWith(p, other))
                {
                    continue;
                }

                int opinion = p.relations.OpinionOf(other);
                if (worst == null || opinion < worstOpinion)
                {
                    worst = other;
                    worstOpinion = opinion;
                }
            }

            __result = worst;
        }
    }
}

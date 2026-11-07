#nullable enable
using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Biotech "trying for a baby" Lovin mode (AZR-574). While a couple is trying
    /// and can conceive, Lovin comes sooner and the short cooldown applies even in
    /// pregnancy-safe mode. It ends by itself once a partner is pregnant.
    /// Without Biotech every check here is false.
    /// </summary>
    public static class DateNightBaby
    {
        public static bool IsPregnant(Pawn? pawn)
        {
            return ModsConfig.BiotechActive
                && pawn?.health?.hediffSet != null
                && PregnancyUtility.GetPregnancyHediff(pawn) != null;
        }

        /// <summary>Opposite sexes, neither pregnant, and vanilla gives a pregnancy chance above 0.</summary>
        public static bool CanConceive(Pawn a, Pawn b)
        {
            if (!ModsConfig.BiotechActive || a == null || b == null || a.Dead || b.Dead)
            {
                return false;
            }
            Pawn? woman = a.gender == Gender.Female ? a : b.gender == Gender.Female ? b : null;
            Pawn? man = a.gender == Gender.Male ? a : b.gender == Gender.Male ? b : null;
            if (woman == null || man == null || IsPregnant(woman))
            {
                return false;
            }
            if (!LovePartnerRelationUtility.LovePartnerRelationExists(a, b))
            {
                return false;
            }
            try
            {
                return PregnancyUtility.PregnancyChanceForPartners(woman, man) > 0f;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsTrying(Pawn a, Pawn b)
        {
            CoupleHistory? h = DateNightHistory.Get(a, b);
            return h != null && h.tryingForBaby;
        }

        /// <summary>Trying and still able to conceive with the current love partner.</summary>
        public static bool ActiveFor(Pawn pawn)
        {
            Pawn? partner = DateNightPawns.LovePartner(pawn);
            return partner != null && IsTrying(pawn, partner) && CanConceive(pawn, partner);
        }

        /// <summary>Gizmo entry point; synced in Multiplayer.</summary>
        public static void SetTrying(Pawn a, Pawn b, bool on)
        {
            if (a == null || b == null)
            {
                return;
            }
            DateNightHistory.GetOrCreate(a, b).tryingForBaby = on && CanConceive(a, b);
        }
    }
}

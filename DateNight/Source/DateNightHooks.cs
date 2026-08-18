using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Ideology / Biotech gates. Missing DLC or defs fail open (never block).
    /// </summary>
    public static class DateNightHooks
    {
        public static bool IdeologyAllowsLovin(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }
            if (!ModsConfig.IdeologyActive)
            {
                return true;
            }

            try
            {
                HistoryEventDef evDef = HistoryEventDefOf.InitiatedLovin;
                if (evDef == null)
                {
                    return true;
                }
                return new HistoryEvent(evDef, pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo();
            }
            catch
            {
                return true;
            }
        }

        public static bool BiotechBlocksForcedLovin(Pawn pawn)
        {
            if (pawn == null || !ModsConfig.BiotechActive)
            {
                return false;
            }

            try
            {
                return pawn.Sterile();
            }
            catch
            {
                return false;
            }
        }

        public static bool CanForceCoupleLovin(Pawn pawn, Pawn partner)
        {
            if (pawn == null || partner == null)
            {
                return false;
            }
            if (!IdeologyAllowsLovin(pawn) || !IdeologyAllowsLovin(partner))
            {
                return false;
            }
            if (BiotechBlocksForcedLovin(pawn) || BiotechBlocksForcedLovin(partner))
            {
                return false;
            }

            try
            {
                return LovePartnerRelationUtility.GetLovinMtbHours(pawn, partner) > 0f;
            }
            catch
            {
                return true;
            }
        }
    }
}

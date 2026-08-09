using Verse;

namespace Nemesis
{
    /// <summary>
    /// Stable soft-compat surface for Rimesis / Back for Vengeance / other
    /// antagonist mods. Prefer these over digging into GameComponent_Nemesis.
    /// Fail-open: all members are safe when Nemesis is loaded solo.
    /// </summary>
    public static class NemesisCompatApi
    {
        public static bool IsActive => true;

        public static bool HasActiveHunt
        {
            get
            {
                GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
                return comp != null && comp.IsEngaged;
            }
        }

        public static Pawn ActiveNemesisPawn
        {
            get
            {
                GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
                return comp?.FindNemesisPawn();
            }
        }

        public static bool IsNemesisPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp?.Data == null || !comp.IsEngaged)
            {
                return false;
            }

            return pawn.thingIDNumber == comp.Data.nemesisPawnId;
        }

        public static bool WouldClaim(Pawn pawn) => IsNemesisPawn(pawn) || HasActiveHunt;

        /// <summary>
        /// True when Rimesis should treat this pawn as Missing (cannot call to
        /// action / hunt down) because Nemesis has exclusive claim on them.
        /// Prefer this over <see cref="WouldClaim"/> for Availability handshakes:
        /// WouldClaim is broader (any active hunt). Fail-open; no Rimesis dependency.
        /// Spec: docs/ideas/nemesis-rimesis-compat.md
        /// </summary>
        public static bool ShouldReportMissingToRimesis(Pawn pawn) => IsNemesisPawn(pawn);
    }
}

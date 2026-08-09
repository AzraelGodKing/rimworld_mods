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
    }
}

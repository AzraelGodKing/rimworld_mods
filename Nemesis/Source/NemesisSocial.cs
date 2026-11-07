#nullable enable
using Verse;

namespace Nemesis
{
    /// <summary>Opinion / social-fight seasoning between nemesis and fixation target.</summary>
    public static class NemesisSocial
    {
        /// <summary>Extra social-fight weight when one pawn is a hunt's nemesis and the other its fixation target.</summary>
        public static float SocialFightMultiplier(Pawn a, Pawn b)
        {
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
            if (comp == null || a == null || b == null || comp.Hunts.Count == 0) return 1f;
            if (Fixated(comp.HuntForNemesis(a), b) || Fixated(comp.HuntForNemesis(b), a))
                return 2.4f;
            return 1f;
        }

        private static bool Fixated(NemesisData? h, Pawn target) =>
            h != null && h.targetMode == NemesisTargetMode.Pawn && target.thingIDNumber == h.targetPawnId;
    }
}

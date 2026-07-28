using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>Opinion / social-fight seasoning between nemesis and fixation target.</summary>
    public static class NemesisSocial
    {
        /// <summary>Extra social-fight weight when the other pawn is the hunt's fixation/nemesis.</summary>
        public static float SocialFightMultiplier(Pawn a, Pawn b)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsEngaged) return 1f;
            NemesisData data = comp.Data;
            if (data == null || data.targetMode != NemesisTargetMode.Pawn) return 1f;

            bool aNem = comp.IsNemesisPawn(a);
            bool bNem = comp.IsNemesisPawn(b);
            bool aTgt = a != null && a.thingIDNumber == data.targetPawnId;
            bool bTgt = b != null && b.thingIDNumber == data.targetPawnId;
            if ((aNem && bTgt) || (bNem && aTgt))
                return 2.4f;
            return 1f;
        }
    }
}

using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>Opinion / social-fight seasoning between nemesis and fixation target.</summary>
    public static class NemesisSocial
    {
        public static void EnsureRivalry(NemesisData data, Pawn nemesis, Pawn target)
        {
            if (data == null || nemesis == null || target == null) return;
            if (data.socialSeeded) return;
            data.socialSeeded = true;

            try
            {
                ThoughtDef hate = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_PersonalRivalry");
                if (hate != null)
                {
                    if (nemesis.needs?.mood?.thoughts?.memories != null)
                        nemesis.needs.mood.thoughts.memories.TryGainMemory(hate, target);
                    if (target.needs?.mood?.thoughts?.memories != null)
                        target.needs.mood.thoughts.memories.TryGainMemory(hate, nemesis);
                }
            }
            catch
            {
                // fail-open
            }
        }

        /// <summary>Extra social-fight weight when the other pawn is the hunt's fixation/nemesis.</summary>
        public static float SocialFightMultiplier(Pawn initiator, Pawn recipient)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsEngaged) return 1f;
            NemesisData data = comp.Data;
            if (data == null || data.targetMode != NemesisTargetMode.Pawn) return 1f;

            bool aNem = comp.IsNemesisPawn(initiator);
            bool bNem = comp.IsNemesisPawn(recipient);
            bool aTgt = initiator != null && initiator.thingIDNumber == data.targetPawnId;
            bool bTgt = recipient != null && recipient.thingIDNumber == data.targetPawnId;
            if ((aNem && bTgt) || (bNem && aTgt))
                return 2.4f;
            return 1f;
        }
    }
}

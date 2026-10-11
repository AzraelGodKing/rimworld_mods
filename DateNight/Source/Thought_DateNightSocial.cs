using RimWorld;
using Verse;

namespace DateNight
{
    // Vanilla fills {0} in a thought description with the pawn who has the thought,
    // so "That place with {0} is ours" named the pawn instead of their partner.
    // Date Night descriptions use {0} for the other pawn; {PAWN_*} still means the owner.
    public class Thought_DateNightSocial : Thought_MemorySocial
    {
        public override string Description
        {
            get
            {
                string text = CurStage?.description;
                if (text.NullOrEmpty())
                {
                    text = def.description;
                }
                if (text.NullOrEmpty() || otherPawn == null)
                {
                    return base.Description;
                }
                return text.Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).Resolve();
            }
        }
    }
}

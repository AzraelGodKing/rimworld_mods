using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Defines a specific kind of psychological trauma a pawn can suffer.
    /// The actual mood effect comes from <see cref="thoughtDef"/> (a ThoughtDef using
    /// Thought_Trauma as its class).
    /// </summary>
    public class TraumaDef : Def
    {
        /// <summary>The Thought_Trauma ThoughtDef applied to the sufferer.</summary>
        public ThoughtDef thoughtDef;

        /// <summary>How many ticks therapy advances thought.age toward expiry per session.</summary>
        public int therapyHealingPerSession = 60000; // ~1 in-game day

        /// <summary>Descriptive label shown when the trauma is triggered.</summary>
        public string triggerMessage;
    }
}

using RimWorld;
using Verse;

namespace DeepColony
{
    public class ArchetypeDef : Def
    {
        public SkillDef skillA;
        public SkillDef skillB;
        /// <summary>Minimum unlocked perk tier required in each skill (2 = L15).</summary>
        public int requiredTier = 2;
        public HediffDef hediff;
    }
}

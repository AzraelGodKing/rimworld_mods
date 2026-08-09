using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Defines one node in a skill's perk tree. The actual game-mechanical bonus is on
    /// <see cref="hediff"/> (a permanent HediffDef with stat offsets).
    /// </summary>
    public class PerkDef : Def
    {
        public SkillDef skill;
        public int requiredLevel = 5;
        public string prerequisitePerk;
        public HediffDef hediff;

        /// <summary>A02 — mutually exclusive sibling perk defNames (same tier).</summary>
        public List<string> exclusiveWith;

        /// <summary>A02 — alternate L15 branch; hidden unless branching perks enabled.</summary>
        public bool alternateBranch;

        /// <summary>A01 — L20 capstone; hidden unless capstones enabled.</summary>
        public bool capstone;

        public bool HasPrerequisite => !prerequisitePerk.NullOrEmpty();
    }
}

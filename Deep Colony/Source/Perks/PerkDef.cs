using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Defines one node in a skill's perk tree. The actual game-mechanical bonus is on
    /// <see cref="hediff"/> (a permanent HediffDef with stat offsets) so vanilla handles
    /// all the numbers. PerkDef is purely the unlock gate + tree structure.
    /// </summary>
    public class PerkDef : Def
    {
        /// <summary>The skill this perk belongs to.</summary>
        public SkillDef skill;

        /// <summary>Minimum skill level required to unlock this perk.</summary>
        public int requiredLevel = 5;

        /// <summary>defName of a PerkDef that must be unlocked first (leave blank for root perks).</summary>
        public string prerequisitePerk;

        /// <summary>The permanent hediff applied to the pawn when this perk is unlocked.</summary>
        public HediffDef hediff;

        public bool HasPrerequisite => !prerequisitePerk.NullOrEmpty();
    }
}

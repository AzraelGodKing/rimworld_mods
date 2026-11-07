#nullable enable
using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>
    /// What a couple likes, as either partner having a trait or passion (AZR-559).
    /// A plain struct so the weights and the quality score can be unit tested.
    /// </summary>
    public struct CoupleTaste
    {
        public bool gourmand;
        public bool ascetic;
        public bool nightOwl;
        public bool artistic;
        public bool animals;
        public bool kind;
        public bool abrasive;

        public static CoupleTaste Of(Pawn a, Pawn b)
        {
            CoupleTaste t = default;
            Add(ref t, a);
            Add(ref t, b);
            return t;
        }

        private static void Add(ref CoupleTaste t, Pawn p)
        {
            TraitSet? traits = p?.story?.traits;
            if (traits != null)
            {
                t.gourmand |= Has(traits, Traits.Gourmand);
                t.ascetic |= Has(traits, Traits.Ascetic);
                t.nightOwl |= Has(traits, Traits.NightOwl);
                t.kind |= Has(traits, Traits.Kind);
                t.abrasive |= Has(traits, Traits.Abrasive);
            }
            t.artistic |= Passionate(p, SkillDefOf.Artistic);
            t.animals |= Passionate(p, SkillDefOf.Animals);
        }

        private static bool Has(TraitSet traits, TraitDef? def)
        {
            return def != null && traits.HasTrait(def);
        }

        private static bool Passionate(Pawn? p, SkillDef def)
        {
            SkillRecord? skill = p?.skills?.GetSkill(def);
            return skill != null && !skill.TotallyDisabled && skill.passion != Passion.None;
        }

        /// <summary>Looked up by name once; a mod that removes one just drops that taste.</summary>
        private static class Traits
        {
            public static readonly TraitDef? Gourmand = DefDatabase<TraitDef>.GetNamedSilentFail("Gourmand");
            public static readonly TraitDef? Ascetic = DefDatabase<TraitDef>.GetNamedSilentFail("Ascetic");
            public static readonly TraitDef? NightOwl = DefDatabase<TraitDef>.GetNamedSilentFail("NightOwl");
            public static readonly TraitDef? Kind = DefDatabase<TraitDef>.GetNamedSilentFail("Kind");
            public static readonly TraitDef? Abrasive = DefDatabase<TraitDef>.GetNamedSilentFail("Abrasive");
        }
    }
}

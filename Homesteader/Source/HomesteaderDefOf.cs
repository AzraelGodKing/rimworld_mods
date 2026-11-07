#nullable enable
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>Homesteader defs read on hot paths, bound once at startup (AZR-561).</summary>
    [DefOf]
    public static class HomesteaderDefOf
    {
        public static TraitDef Homesteader_Polyarmory = null!;

        public static ThoughtDef Homesteader_AteFavoriteFood = null!;

        static HomesteaderDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HomesteaderDefOf));
        }
    }
}

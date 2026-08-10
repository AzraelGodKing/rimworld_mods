using RimWorld;
using Verse;

namespace Wardrobe
{
    [DefOf]
    public static class WardrobeDefOf
    {
        public static JobDef Wardrobe_ChangeOutfit;

        static WardrobeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WardrobeDefOf));
        }
    }
}

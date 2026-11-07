using RimWorld;
using Verse;

namespace NanoHiveReforged
{
    [DefOf]
    public static class NanoHiveDefOf
    {
        public static ThingDef NanoHiveReforged;

        static NanoHiveDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(NanoHiveDefOf));
        }
    }
}

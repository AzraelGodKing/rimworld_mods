using RimWorld;
using Verse;

namespace ShiftChange
{
    [DefOf]
    public static class ShiftChangeDefOf
    {
        public static JobDef ShiftChange_Apply;
        public static JobDef ShiftChange_Restore;

        static ShiftChangeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ShiftChangeDefOf));
        }
    }
}

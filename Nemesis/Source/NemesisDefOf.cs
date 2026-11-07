using RimWorld;
using Verse;

namespace Nemesis
{
    [DefOf]
    public static class NemesisDefOf
    {
        public static JobDef Nemesis_UseCommsConsole;

        static NemesisDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(NemesisDefOf));
        }
    }
}

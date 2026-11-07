#nullable enable
using RimWorld;
using Verse;

namespace Nemesis
{
    [DefOf]
    public static class NemesisDefOf
    {
        public static JobDef Nemesis_UseCommsConsole = null!;
        public static HediffDef Nemesis_BattleHardened = null!;
        public static ThoughtDef Nemesis_EscapedAgain = null!;
        public static ThingDef Nemesis_CallingCard = null!;

        static NemesisDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(NemesisDefOf));
        }
    }
}

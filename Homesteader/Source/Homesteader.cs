#nullable enable
using System.Collections.Generic;
using AzraelCommon;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
    [StaticConstructorOnStartup]
    public static class HomesteaderHarmony
    {
        // Def flags by ThingDef.index, built once after defs load (AZR-561). Hot
        // patches check these before any comp lookup, so most things leave at once.
        private static bool[] rottable = new bool[0];
        private static bool[] landrace = new bool[0];
        private static bool[] mapleSap = new bool[0];
        private static bool[] preserveAging = new bool[0];

        static HomesteaderHarmony()
        {
            SafePatchAll.Apply(new Harmony(PatchHealth.HarmonyId), "[Homesteader]");
            PatchHealth.Check();
            StatPart_PreserveAging.Register();
            BuildDefFlags();

            HomesteaderGameplayDefs.Apply(HomesteaderMod.Settings.gameplay);
            HomesteaderMultiplayer.RegisterAll();
        }

        internal static bool IsRottable(ThingDef? def) => Flag(rottable, def);

        internal static bool HasLandrace(ThingDef? def) => Flag(landrace, def);

        internal static bool HasMapleSap(ThingDef? def) => Flag(mapleSap, def);

        internal static bool HasPreserveAging(ThingDef? def) => Flag(preserveAging, def);

        private static bool Flag(bool[] flags, ThingDef? def)
        {
            return def != null && def.index < flags.Length && flags[def.index];
        }

        private static void BuildDefFlags()
        {
            List<ThingDef> all = DefDatabase<ThingDef>.AllDefsListForReading;
            int size = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].index >= size)
                {
                    size = all[i].index + 1;
                }
            }

            rottable = new bool[size];
            landrace = new bool[size];
            mapleSap = new bool[size];
            preserveAging = new bool[size];
            for (int i = 0; i < all.Count; i++)
            {
                ThingDef def = all[i];
                rottable[def.index] = def.HasComp(typeof(CompRottable));
                landrace[def.index] = def.HasComp(typeof(CompLandrace));
                mapleSap[def.index] = def.HasComp(typeof(CompMapleSap));
                preserveAging[def.index] = def.HasComp(typeof(CompPreserveAging));
            }
        }
    }
}

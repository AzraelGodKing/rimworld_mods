using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Niceties
{
    internal static class CryptosleepBar
    {
        internal static void MarkDirty()
        {
            Find.ColonistBar?.MarkColonistsDirty();
        }

        internal static bool ShouldHide(Pawn pawn)
        {
            if (pawn == null || NicetiesMod.Settings == null || !NicetiesMod.Settings.hideCryptosleep)
            {
                return false;
            }

            return pawn.InCryptosleep;
        }

        internal static void FilterEntries(ColonistBar bar)
        {
            if (bar == null || NicetiesMod.Settings == null || !NicetiesMod.Settings.hideCryptosleep)
            {
                return;
            }

            List<ColonistBar.Entry> entries = AccessTools.Field(typeof(ColonistBar), "cachedEntries")
                ?.GetValue(bar) as List<ColonistBar.Entry>;
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (ShouldHide(entries[i].pawn))
                {
                    entries.RemoveAt(i);
                }
            }
        }
    }

    // Runs after entries are filled and before draw locations are computed, so the
    // bar layout matches the filtered list (mutating cachedEntries after layout is what
    // crashed older hide-from-bar mods).
    [HarmonyPatch(typeof(ColonistBarDrawLocsFinder), "CalculateDrawLocs")]
    [HarmonyPatch(new[] { typeof(List<Vector2>), typeof(float), typeof(int) },
        new[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    internal static class Patch_ColonistBarDrawLocs
    {
        private static void Prefix()
        {
            CryptosleepBar.FilterEntries(Find.ColonistBar);
        }
    }

    [HarmonyPatch(typeof(Building_CryptosleepCasket), nameof(Building_CryptosleepCasket.TryAcceptThing))]
    internal static class Patch_Casket_TryAcceptThing
    {
        private static void Postfix(bool __result)
        {
            if (__result && NicetiesMod.Settings != null && NicetiesMod.Settings.hideCryptosleep)
            {
                CryptosleepBar.MarkDirty();
            }
        }
    }

    [HarmonyPatch(typeof(Building_CryptosleepCasket), nameof(Building_CryptosleepCasket.EjectContents))]
    internal static class Patch_Casket_EjectContents
    {
        private static void Postfix()
        {
            if (NicetiesMod.Settings != null && NicetiesMod.Settings.hideCryptosleep)
            {
                CryptosleepBar.MarkDirty();
            }
        }
    }
}

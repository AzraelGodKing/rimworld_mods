using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Niceties
{
    internal static class CryptosleepBar
    {
        private static bool entriesMissingLogged;

        internal static void MarkDirty()
        {
            Find.ColonistBar?.MarkColonistsDirty();
        }

        internal static bool ShouldHide(Pawn pawn)
        {
            NicetiesSettings settings = NicetiesSim.Settings;
            if (pawn == null || settings == null || !settings.hideCryptosleep)
            {
                return false;
            }

            return pawn.InCryptosleep;
        }

        internal static void FilterEntries(ColonistBar bar)
        {
            NicetiesSettings settings = NicetiesSim.Settings;
            if (bar == null || settings == null || !settings.hideCryptosleep)
            {
                return;
            }

            // Prefer the public Entries property over AccessTools.Field("cachedEntries")
            // so a Ludeon rename fails loudly once instead of silently (AZR-291).
            List<ColonistBar.Entry> entries = bar.Entries;
            if (entries == null)
            {
                if (!entriesMissingLogged)
                {
                    entriesMissingLogged = true;
                    Log.Warning("[Niceties] ColonistBar.Entries was null; hide-cryptosleep filter skipped.");
                }

                return;
            }

            if (entries.Count == 0)
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
            NicetiesSettings settings = NicetiesSim.Settings;
            if (__result && settings != null && settings.hideCryptosleep)
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
            NicetiesSettings settings = NicetiesSim.Settings;
            if (settings != null && settings.hideCryptosleep)
            {
                CryptosleepBar.MarkDirty();
            }
        }
    }
}

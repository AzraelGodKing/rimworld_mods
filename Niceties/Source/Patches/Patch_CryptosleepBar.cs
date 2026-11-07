#nullable enable
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Niceties
{
    internal static class CryptosleepBar
    {
        private static readonly List<ColonistBar.Entry> Scratch = new List<ColonistBar.Entry>();
        private static readonly HashSet<int> VisibleGroups = new HashSet<int>();
        private static readonly HashSet<int> PlaceholderGroups = new HashSet<int>();

        private static bool entriesMissingLogged;
        private static int hiddenCount;
        private static int lastFilteredCount = -1;
        private static List<ColonistBar.Entry>? lastFilteredList;

        private static readonly List<Pawn> HiddenPawns = new List<Pawn>();
        private static bool allInCaskets = true;

        private static int labelCount = -1;
        private static bool labelAllCaskets = true;
        private static LoadedLanguage? labelLanguage;
        private static string label = "";
        private static string tooltip = "";
        private static Vector2 labelSize;

        internal static int HiddenCount => hiddenCount;

        internal static void MarkDirty()
        {
            labelCount = -1;
            Find.ColonistBar?.MarkColonistsDirty();
        }

        /// <summary>
        /// Runs from a prefix on <c>CalculateDrawLocs</c>, after the entries are filled and
        /// before the layout reads them (AZR-547). Settings are read once and the list is
        /// rebuilt in one pass. A map group left with no visible colonist gets the same
        /// pawn-less entry vanilla uses for an empty map, so the group count the layout was
        /// given stays right.
        /// </summary>
        internal static void FilterEntries(ColonistBar? bar)
        {
            if (bar == null)
            {
                return;
            }

            if (!NicetiesSim.Settings.hideCryptosleep)
            {
                hiddenCount = 0;
                return;
            }

            List<ColonistBar.Entry>? entries = bar.Entries;
            if (entries == null)
            {
                if (!entriesMissingLogged)
                {
                    entriesMissingLogged = true;
                    Log.Warning("[Niceties] ColonistBar.Entries was null; hide-cryptosleep filter skipped.");
                }

                return;
            }

            VisibleGroups.Clear();
            int removed = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (Hides(entries[i].pawn))
                {
                    removed++;
                }
                else
                {
                    VisibleGroups.Add(entries[i].group);
                }
            }

            if (removed == 0)
            {
                // A second layout pass over the list we already filtered finds nothing to
                // remove; keep the count so "+N" does not vanish on a resize.
                if (!ReferenceEquals(entries, lastFilteredList) || entries.Count != lastFilteredCount)
                {
                    hiddenCount = 0;
                    HiddenPawns.Clear();
                }

                return;
            }

            Scratch.Clear();
            PlaceholderGroups.Clear();
            HiddenPawns.Clear();
            allInCaskets = true;
            for (int i = 0; i < entries.Count; i++)
            {
                ColonistBar.Entry entry = entries[i];
                if (!Hides(entry.pawn))
                {
                    Scratch.Add(entry);
                    continue;
                }

                HiddenPawns.Add(entry.pawn!);
                allInCaskets &= entry.pawn!.InCryptosleep;

                if (entry.map != null && !VisibleGroups.Contains(entry.group) && PlaceholderGroups.Add(entry.group))
                {
                    entry.pawn = null;
                    Scratch.Add(entry);
                }
            }

            entries.Clear();
            entries.AddRange(Scratch);
            Scratch.Clear();
            hiddenCount = removed;
            lastFilteredList = entries;
            lastFilteredCount = entries.Count;
        }

        // ColonistBar.Visible is private. Bound once; if a RimWorld update renames it,
        // the note simply follows the setting as it did in 1.x.
        private static readonly System.Func<ColonistBar, bool>? VisibleGetter = BindVisible();

        private static System.Func<ColonistBar, bool>? BindVisible()
        {
            System.Reflection.MethodInfo? getter = AccessTools.PropertyGetter(typeof(ColonistBar), "Visible");
            return getter == null ? null : AccessTools.MethodDelegate<System.Func<ColonistBar, bool>>(getter);
        }

        private static bool IsVisible(ColonistBar bar)
        {
            return VisibleGetter == null || VisibleGetter(bar);
        }

        /// <summary>
        /// Casket pawns always (when the nicety is on). Growth-vat children and pawns in a
        /// biosculpter pod or subcore scanner only when their own local option is ticked (AZR-568).
        /// </summary>
        private static bool Hides(Pawn? pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.InCryptosleep)
            {
                return true;
            }

            if (pawn.Spawned)
            {
                return false;
            }

            NicetiesSettings local = NicetiesMod.Settings;
            IThingHolder? holder = pawn.ParentHolder;
            if (local.ui.hideGrowthVats && holder is Building_GrowthVat)
            {
                return true;
            }

            return local.ui.hidePods && (holder is CompBiosculpterPod || holder is Building_SubcoreScanner);
        }

        /// <summary>The spawned building holding a hidden pawn: casket, vat, pod or scanner.</summary>
        private static Thing? HolderThing(Pawn pawn)
        {
            IThingHolder? holder = pawn.ParentHolder;
            Thing? thing = holder as Thing ?? (holder as ThingComp)?.parent;
            return thing != null && thing.Spawned ? thing : null;
        }

        /// <summary>Click "+N": a list of the hidden pawns; picking one jumps to their casket (AZR-568).</summary>
        private static void OpenRoster()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < HiddenPawns.Count; i++)
            {
                Pawn pawn = HiddenPawns[i];
                Thing? holder = pawn.Destroyed ? null : HolderThing(pawn);
                if (holder == null)
                {
                    continue;
                }

                options.Add(new FloatMenuOption(pawn.LabelShort + " (" + holder.LabelCap + ")",
                    () => CameraJumper.TryJumpAndSelect(holder)));
            }

            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        internal static void DrawHiddenCount(ColonistBar bar)
        {
            if (hiddenCount <= 0 || !IsVisible(bar) || !NicetiesMod.Settings.ui.showCryptosleepCount
                || !NicetiesSim.Settings.hideCryptosleep)
            {
                return;
            }

            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            Color color = GUI.color;
            try
            {
                Text.Font = GameFont.Tiny;
                RefreshLabel();
                Rect rect = LabelRect(bar);
                if (Event.current.type == EventType.Repaint)
                {
                    Text.Anchor = TextAnchor.MiddleLeft;
                    GUI.color = new Color(0.75f, 0.85f, 1f);
                    Widgets.Label(rect, label);
                }

                TooltipHandler.TipRegion(rect, tooltip);
                if (NicetiesMod.Settings.ui.cryptosleepRoster)
                {
                    Widgets.DrawHighlightIfMouseover(rect);
                    if (Widgets.ButtonInvisible(rect))
                    {
                        OpenRoster();
                    }
                }
            }
            finally
            {
                GUI.color = color;
                Text.Anchor = anchor;
                Text.Font = font;
            }
        }

        private static void RefreshLabel()
        {
            if (labelCount == hiddenCount && labelAllCaskets == allInCaskets
                && ReferenceEquals(labelLanguage, LanguageDatabase.activeLanguage))
            {
                return;
            }

            labelCount = hiddenCount;
            labelAllCaskets = allInCaskets;
            labelLanguage = LanguageDatabase.activeLanguage;
            label = allInCaskets
                ? "Niceties_CryptoHiddenCount".Translate(hiddenCount)
                : "Niceties_HiddenCount".Translate(hiddenCount);
            tooltip = "Niceties_CryptoHiddenCountTip".Translate();
            if (NicetiesMod.Settings.ui.cryptosleepRoster)
            {
                tooltip += "\n\n" + "Niceties_CryptoRosterTip".Translate();
            }
            labelSize = Text.CalcSize(label);
        }

        /// <summary>Right of the last portrait, sized to the text and kept on screen.</summary>
        private static Rect LabelRect(ColonistBar bar)
        {
            float width = labelSize.x + 4f;
            List<Vector2> locs = bar.DrawLocs;
            Vector2 size = bar.Size;
            float x;
            float y;
            float height;
            if (locs != null && locs.Count > 0)
            {
                Vector2 last = locs[locs.Count - 1];
                x = last.x + size.x + 6f;
                y = last.y;
                height = size.y;
            }
            else
            {
                x = UI.screenWidth / 2f - width / 2f;
                y = 12f;
                height = 24f;
            }

            if (x + width > UI.screenWidth - 4f)
            {
                x = UI.screenWidth - 4f - width;
                y += height;
                height = labelSize.y + 4f;
            }

            return new Rect(x < 0f ? 0f : x, y, width, height);
        }
    }

    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI))]
    internal static class Patch_ColonistBarOnGUI
    {
        private static void Postfix(ColonistBar __instance)
        {
            CryptosleepBar.DrawHiddenCount(__instance);
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
        private static bool Prepare()
        {
            return PatchTargets.Exists(typeof(ColonistBarDrawLocsFinder), "CalculateDrawLocs",
                new[] { typeof(List<Vector2>), typeof(float).MakeByRefType(), typeof(int) },
                PatchTargets.HideCryptosleep);
        }

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
            if (__result && NicetiesSim.Settings.hideCryptosleep)
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
            if (NicetiesSim.Settings.hideCryptosleep)
            {
                CryptosleepBar.MarkDirty();
            }
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Pawn lists for Work / Schedule tabs across portal-linked colony levels.
    public static class StrataColonyPawnsUtility
    {
        private static readonly List<Map> tmpMaps = new List<Map>();
        private static readonly List<Pawn> tmpPawns = new List<Pawn>();
        private static readonly HashSet<Pawn> tmpSeen = new HashSet<Pawn>();

        public static bool ShowLinkedColonists =>
            StrataMod.Settings?.showLinkedColonistsInWorkScheduleTabs == true;

        public static bool CanOfferLinkedColonistToggle(Map map)
        {
            if (!ColonyBedUtility.MapInStrataColony(map))
            {
                return false;
            }
            Map home = ColonyBedUtility.FindSurfacePlayerHome(map);
            return home != null && LevelGraph.AnyLinkFrom(home);
        }

        public static IEnumerable<Pawn> WorkTabPawns()
        {
            return ColonistsForTab(includeSubhumans: false);
        }

        public static IEnumerable<Pawn> ScheduleTabPawns()
        {
            return ColonistsForTab(includeSubhumans: true);
        }

        // Call after camera jumps floors so Work/Schedule refresh if still open.
        public static void NotifyCurrentMapChanged()
        {
            MainTabWindow_PawnTable work = Find.WindowStack?.WindowOfType<MainTabWindow_Work>();
            work?.Notify_PawnsChanged();
            MainTabWindow_PawnTable schedule = Find.WindowStack?.WindowOfType<MainTabWindow_Schedule>();
            schedule?.Notify_PawnsChanged();
        }

        public static void DrawLinkedColonistsToggle(Rect rect, MainTabWindow_PawnTable tab)
        {
            if (!CanOfferLinkedColonistToggle(Find.CurrentMap))
            {
                return;
            }

            bool show = ShowLinkedColonists;
            Widgets.CheckboxLabeled(rect, "Strata Levels", ref show);
            if (show != ShowLinkedColonists)
            {
                StrataMod.Settings.showLinkedColonistsInWorkScheduleTabs = show;
                tab.Notify_PawnsChanged();
            }
        }

        private static IEnumerable<Pawn> ColonistsForTab(bool includeSubhumans)
        {
            Map current = Find.CurrentMap;
            if (current == null)
            {
                return System.Array.Empty<Pawn>();
            }

            if (!ShowLinkedColonists || !CanOfferLinkedColonistToggle(current))
            {
                return SnapshotCurrentMapPawns(current, includeSubhumans);
            }

            tmpPawns.Clear();
            tmpSeen.Clear();
            ColonyBedUtility.GetColonyNetworkMaps(current, tmpMaps);
            for (int m = 0; m < tmpMaps.Count; m++)
            {
                Map map = tmpMaps[m];
                foreach (Pawn pawn in map.mapPawns.FreeColonists)
                {
                    if (pawn == null || tmpSeen.Contains(pawn) || pawn.DevelopmentalStage.Baby())
                    {
                        continue;
                    }
                    tmpSeen.Add(pawn);
                    tmpPawns.Add(pawn);
                }
                if (includeSubhumans)
                {
                    foreach (Pawn pawn in map.mapPawns.ColonySubhumansControllable)
                    {
                        if (pawn == null || tmpSeen.Contains(pawn) || pawn.DevelopmentalStage.Baby())
                        {
                            continue;
                        }
                        tmpSeen.Add(pawn);
                        tmpPawns.Add(pawn);
                    }
                }
            }

            tmpPawns.Sort(CompareTabPawns);
            // Snapshot: PawnTable may re-enter this getter while AddRange walks the
            // prior enumerable; a shared static list would clear mid-enumeration.
            return new List<Pawn>(tmpPawns);
        }

        private static List<Pawn> SnapshotCurrentMapPawns(Map map, bool includeSubhumans)
        {
            tmpPawns.Clear();
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                if (!pawn.DevelopmentalStage.Baby())
                {
                    tmpPawns.Add(pawn);
                }
            }
            if (includeSubhumans)
            {
                foreach (Pawn pawn in map.mapPawns.ColonySubhumansControllable)
                {
                    if (!pawn.DevelopmentalStage.Baby())
                    {
                        tmpPawns.Add(pawn);
                    }
                }
            }
            return new List<Pawn>(tmpPawns);
        }

        private static int CompareTabPawns(Pawn a, Pawn b)
        {
            int depthA = a?.Map != null && StrataMapUtility.IsUnderground(a.Map) ? StrataDepth.Of(a.Map) : 0;
            int depthB = b?.Map != null && StrataMapUtility.IsUnderground(b.Map) ? StrataDepth.Of(b.Map) : 0;
            int depthCmp = depthA.CompareTo(depthB);
            if (depthCmp != 0)
            {
                return depthCmp;
            }
            return (a?.LabelShortCap ?? string.Empty).CompareTo(b?.LabelShortCap ?? string.Empty);
        }
    }
}

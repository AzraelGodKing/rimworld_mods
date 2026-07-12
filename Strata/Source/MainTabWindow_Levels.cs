using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Bottom-bar "Levels" tab: one row per level of the colony with colonist,
    // hostile, and temperature readouts, and a one-click camera jump. The
    // button stays hidden until at least one underground level exists.
    public class MainButtonWorker_Levels : MainButtonWorker_ToggleTab
    {
        public override bool Visible
        {
            get
            {
                if (!base.Visible)
                {
                    return false;
                }
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    if (StrataMapUtility.IsUnderground(maps[i]))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }

    public class MainTabWindow_Levels : MainTabWindow
    {
        private struct Row
        {
            public Map map;
            public int depth;
            public bool stackHeader; // first row of a colony's level stack
        }

        private const float RowHeight = 30f;
        private const float HeaderHeight = 26f;
        private const float ViewButtonWidth = 64f;
        private const float RenameButtonWidth = 72f;

        private readonly List<Row> rows = new List<Row>();

        // Player-chosen size survives closing the tab (but not the session).
        private static Vector2 savedSize = Vector2.zero;
        private Vector2 openedSize;
        private Vector2 scroll;

        public MainTabWindow_Levels()
        {
            resizeable = true;
        }

        public override Vector2 RequestedTabSize
        {
            get
            {
                Vector2 computed = new Vector2(640f, HeaderHeight + Mathf.Max(rows.Count, 1) * RowHeight + Margin * 2f + 8f);
                return savedSize == Vector2.zero
                    ? computed
                    : new Vector2(Mathf.Max(savedSize.x, 420f), Mathf.Max(savedSize.y, 120f));
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            BuildRows();
            openedSize = new Vector2(windowRect.width, windowRect.height);
        }

        private void BuildRows()
        {
            rows.Clear();
            foreach (Map surface in Find.Maps)
            {
                if (!StrataMapUtility.IsSurfacePlayerHome(surface) || !LevelGraph.AnyLinkFrom(surface))
                {
                    continue;
                }
                rows.Add(new Row { map = surface, depth = 0, stackHeader = true });
                foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(surface))
                {
                    if (StrataMapUtility.IsUnderground(link.map))
                    {
                        rows.Add(new Row { map = link.map, depth = link.depth });
                    }
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Keep the tab glued to the bottom bar while the resize handle
            // drags, and remember the size the player settles on.
            Vector2 size = new Vector2(windowRect.width, windowRect.height);
            if (size != openedSize)
            {
                savedSize = size;
                windowRect.y = UI.screenHeight - 35f - windowRect.height;
            }

            // Levels can open or collapse while the tab sits open.
            BuildRows();

            Text.Font = GameFont.Small;
            float contentWidth = inRect.width - 16f; // leave room for a scrollbar
            float colLevel = 0f;
            float colColonists = contentWidth * 0.42f;
            float colHostiles = contentWidth * 0.58f;
            float colTemp = contentWidth * 0.74f;

            Rect header = new Rect(inRect.x, inRect.y, contentWidth, HeaderHeight);
            GUI.color = Color.gray;
            Widgets.Label(new Rect(header.x + colLevel + 4f, header.y, colColonists - 8f, HeaderHeight), "Level");
            Widgets.Label(new Rect(header.x + colColonists, header.y, colHostiles - colColonists, HeaderHeight), "Colonists");
            Widgets.Label(new Rect(header.x + colHostiles, header.y, colTemp - colHostiles, HeaderHeight), "Hostiles");
            Widgets.Label(new Rect(header.x + colTemp, header.y, contentWidth - colTemp - ViewButtonWidth - RenameButtonWidth, HeaderHeight), "Temp");
            GUI.color = Color.white;
            Widgets.DrawLineHorizontal(inRect.x, header.yMax - 2f, inRect.width);

            if (rows.Count == 0)
            {
                Widgets.Label(new Rect(inRect.x + 4f, header.yMax + 4f, inRect.width, RowHeight), "No excavated levels.");
                return;
            }

            Rect scrollOut = new Rect(inRect.x, header.yMax + 2f, inRect.width, inRect.yMax - header.yMax - 2f);
            Rect scrollView = new Rect(0f, 0f, contentWidth, rows.Count * RowHeight);
            Widgets.BeginScrollView(scrollOut, ref scroll, scrollView);
            float y = 0f;
            foreach (Row row in rows)
            {
                Rect rowRect = new Rect(0f, y, contentWidth, RowHeight);
                if (row.map == Find.CurrentMap)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y, colColonists - 8f, RowHeight), LevelLabel(row));
                Widgets.Label(new Rect(rowRect.x + colColonists, rowRect.y, colHostiles - colColonists, RowHeight),
                    row.map.mapPawns.FreeColonistsSpawnedCount.ToString());
                int hostiles = HostileCount(row.map);
                GUI.color = hostiles > 0 ? ColorLibrary.RedReadable : Color.white;
                Widgets.Label(new Rect(rowRect.x + colHostiles, rowRect.y, colTemp - colHostiles, RowHeight),
                    hostiles.ToString());
                GUI.color = Color.white;
                Widgets.Label(new Rect(rowRect.x + colTemp, rowRect.y, contentWidth - colTemp - ViewButtonWidth - RenameButtonWidth, RowHeight),
                    row.map.mapTemperature.OutdoorTemp.ToStringTemperature("F0"));
                Text.Anchor = TextAnchor.UpperLeft;

                Rect renameRect = new Rect(rowRect.xMax - ViewButtonWidth - RenameButtonWidth, rowRect.y + 3f, RenameButtonWidth - 4f, RowHeight - 6f);
                if (Widgets.ButtonText(renameRect, "Rename"))
                {
                    Find.WindowStack.Add(new Dialog_RenameLevel(row.map, StrataLevelLabels.Get?.GetLabel(row.map) ?? LevelLabel(row)));
                }
                Rect viewRect = new Rect(rowRect.xMax - ViewButtonWidth, rowRect.y + 3f, ViewButtonWidth - 4f, RowHeight - 6f);
                if (row.map != Find.CurrentMap && Widgets.ButtonText(viewRect, "View"))
                {
                    JumpTo(row.map);
                }
                if (Widgets.ButtonInvisible(new Rect(rowRect.x, rowRect.y, rowRect.width - ViewButtonWidth - RenameButtonWidth, RowHeight)))
                {
                    JumpTo(row.map);
                }
                y += RowHeight;
            }
            Widgets.EndScrollView();
        }

        private static string LevelLabel(Row row)
        {
            string custom = StrataLevelLabels.Get?.GetLabel(row.map);
            if (!custom.NullOrEmpty())
            {
                return custom;
            }
            if (row.depth == 0)
            {
                string name = row.map.Parent?.LabelCap;
                return name.NullOrEmpty() ? "Surface" : "Surface \u2014 " + name;
            }
            return "Level -" + row.depth;
        }

        private static int HostileCount(Map map)
        {
            int count = 0;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].HostileTo(Faction.OfPlayer) && !pawns[i].Downed)
                {
                    count++;
                }
            }
            return count;
        }

        private void JumpTo(Map map)
        {
            IntVec3 cell = map.Center;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing.def.defName == "Strata_StairsDown" || thing.def.defName == "Strata_ElevatorDown")
                {
                    cell = thing.Position;
                    break;
                }
            }
            CameraJumper.TryJump(new GlobalTargetInfo(cell, map));
        }
    }
}

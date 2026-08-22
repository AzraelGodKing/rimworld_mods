using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    public static class FamilyTreeDrawer
    {
        public const float NodeW = 118f;
        public const float NodeH = 40f;
        public const float GapX = 8f;
        public const float GapY = 18f;
        public const int MaxPerRow = 8;

        public static float MeasureHeight(FamilyTreeSnapshot snap)
        {
            if (snap == null) return 80f;
            float h = 28f;
            if (!FamilyTreeUtility.HasAnyKin(snap)) return h + 40f;
            if (snap.grandparents.Count > 0) h += NodeH + GapY + 18f;
            if (snap.parents.Count > 0) h += NodeH + GapY + 18f;
            h += NodeH + GapY + 18f; // self row
            if (snap.children.Count > 0) h += NodeH + GapY + 18f;
            if (snap.grandchildren.Count > 0) h += NodeH + GapY + 18f;
            if (snap.mentor != null || snap.apprentices.Count > 0) h += NodeH + 40f;
            return h + 12f;
        }

        public static void Draw(Rect rect, FamilyTreeSnapshot snap, Action<Pawn> onClick)
        {
            if (snap?.focus == null) return;
            float y = rect.y;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, y, rect.width, 24f),
                "DC_FamilyTree_Title".Translate(snap.focus.LabelShort.Named("PAWN")));
            y += 26f;

            if (!FamilyTreeUtility.HasAnyKin(snap))
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x, y, rect.width, 24f),
                    "DC_FamilyTree_Empty".Translate());
                GUI.color = Color.white;
                return;
            }

            y = DrawGeneration(rect, y, "DC_FamilyTree_Grandparents".Translate(), snap.grandparents, snap.focus, onClick);
            y = DrawGeneration(rect, y, "DC_FamilyTree_Parents".Translate(), snap.parents, snap.focus, onClick);
            y = DrawSelfRow(rect, y, snap, onClick);
            y = DrawGeneration(rect, y, "DC_FamilyTree_Children".Translate(), snap.children, snap.focus, onClick);
            y = DrawGeneration(rect, y, "DC_FamilyTree_Grandchildren".Translate(), snap.grandchildren, snap.focus, onClick);
            DrawTeaching(rect, y, snap, onClick);
        }

        private static float DrawGeneration(
            Rect rect, float y, string header, List<Pawn> pawns, Pawn focus, Action<Pawn> onClick)
        {
            if (pawns == null || pawns.Count == 0) return y;
            Widgets.Label(new Rect(rect.x, y, rect.width, 18f), header);
            y += 18f;
            y = DrawCenteredNodes(rect, y, pawns, focus, onClick);
            return y + GapY;
        }

        private static float DrawSelfRow(Rect rect, float y, FamilyTreeSnapshot snap, Action<Pawn> onClick)
        {
            Widgets.Label(new Rect(rect.x, y, rect.width, 18f),
                "DC_FamilyTree_Generation".Translate());
            y += 18f;
            var row = new List<Pawn>();
            for (int i = 0; i < snap.siblings.Count; i++)
                row.Add(snap.siblings[i]);
            row.Add(snap.focus);
            for (int i = 0; i < snap.partners.Count; i++)
                row.Add(snap.partners[i]);
            y = DrawCenteredNodes(rect, y, row, snap.focus, onClick);
            return y + GapY;
        }

        private static float DrawTeaching(Rect rect, float y, FamilyTreeSnapshot snap, Action<Pawn> onClick)
        {
            if (snap.mentor == null && snap.apprentices.Count == 0) return y;
            Widgets.Label(new Rect(rect.x, y, rect.width, 18f),
                "DC_FamilyTree_Teaching".Translate());
            y += 18f;
            var row = new List<Pawn>();
            if (snap.mentor != null) row.Add(snap.mentor);
            for (int i = 0; i < snap.apprentices.Count; i++)
                row.Add(snap.apprentices[i]);
            return DrawCenteredNodes(rect, y, row, snap.focus, onClick);
        }

        private static float DrawCenteredNodes(
            Rect rect, float y, List<Pawn> pawns, Pawn focus, Action<Pawn> onClick)
        {
            int count = Math.Min(pawns.Count, MaxPerRow);
            float extra = pawns.Count > MaxPerRow ? NodeW * 0.55f : 0f;
            float totalW = count * NodeW + Math.Max(0, count - 1) * GapX + extra;
            float x = rect.x + Math.Max(0f, (rect.width - totalW) / 2f);
            for (int i = 0; i < count; i++)
            {
                DrawNode(new Rect(x, y, NodeW, NodeH), pawns[i], focus, onClick);
                x += NodeW + GapX;
            }
            if (pawns.Count > MaxPerRow)
            {
                Rect more = new Rect(x, y, extra, NodeH);
                Widgets.DrawHighlight(more);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(more, "+" + (pawns.Count - MaxPerRow));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            return y + NodeH;
        }

        private static void DrawNode(Rect rect, Pawn pawn, Pawn focus, Action<Pawn> onClick)
        {
            if (pawn == null) return;
            bool self = pawn == focus;
            if (self)
                Widgets.DrawHighlightSelected(rect);
            else if (Mouse.IsOver(rect))
                Widgets.DrawHighlight(rect);
            else
                Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.12f, 0.12f, 0.45f));
            Widgets.DrawBox(rect);

            Rect icon = new Rect(rect.x + 4f, rect.y + 8f, 24f, 24f);
            Widgets.ThingIcon(icon, pawn);

            Rect text = new Rect(rect.x + 30f, rect.y + 2f, rect.width - 34f, rect.height - 4f);
            string name = pawn.LabelShortCap;
            if (pawn.Dead) name += " " + "DC_FamilyTree_Dead".Translate();
            string rel = FamilyTreeUtility.RelationLabel(focus, pawn);
            GUI.color = pawn.Dead ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
            Text.Font = GameFont.Tiny;
            Widgets.Label(text, name + "\n" + rel);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            TooltipHandler.TipRegion(rect, () => Tip(focus, pawn), pawn.thingIDNumber ^ 0x46A11);

            if (!self && Widgets.ButtonInvisible(rect))
                onClick?.Invoke(pawn);
        }

        private static string Tip(Pawn focus, Pawn pawn)
        {
            string rel = FamilyTreeUtility.RelationLabel(focus, pawn);
            string loc;
            if (pawn.Dead)
                loc = "DC_FamilyTree_Dead".Translate();
            else if (pawn.Spawned)
                loc = pawn.Map?.Parent?.LabelCap ?? pawn.Map?.ToString() ?? "";
            else
                loc = "DC_FamilyTree_Away".Translate();
            string click = pawn == focus
                ? ""
                : "\n" + "DC_FamilyTree_ClickTip".Translate();
            return pawn.NameFullColored + "\n" + rel + "\n" + loc + click;
        }
    }
}

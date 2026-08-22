using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    public static class FamilyTreeDrawer
    {
        public const float NodeW = 124f;
        public const float NodeH = 48f;
        public const float GapX = 8f;
        public const float GapY = 12f;
        public const float PadX = 10f;
        public const float HeaderH = 24f;
        public const int MaxPerRow = 8;
        public const float TitleRowH = 26f;

        public static void DrawHeader(Rect rect, Pawn focus, ref Vector2 scrollPos)
        {
            if (focus == null) return;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            float btnW = 118f;
            Rect label = new Rect(rect.x, rect.y, Mathf.Max(40f, rect.width - btnW - 6f), rect.height);
            Widgets.Label(label, "DC_FamilyTree_Title".Translate(focus.LabelShort.Named("PAWN")));
            Text.Anchor = TextAnchor.UpperLeft;

            var settings = DeepColonyMod.Settings ?? DeepColonySettings.Get;
            Rect btn = new Rect(rect.xMax - btnW, rect.y, btnW, rect.height);
            string cap = settings.familyTreePedigreeStyle
                ? "DC_FamilyTree_StyleRows".Translate()
                : "DC_FamilyTree_StylePedigree".Translate();
            if (Widgets.ButtonText(btn, cap))
            {
                settings.familyTreePedigreeStyle = !settings.familyTreePedigreeStyle;
                settings.Write();
                scrollPos = Vector2.zero;
            }
            if (Mouse.IsOver(btn))
                TooltipHandler.TipRegion(btn, "DC_Settings_FamilyTreePedigreeTip".Translate());
        }

        public static Vector2 MeasureSize(FamilyTreeSnapshot snap, bool includeTitle = true)
        {
            if (DeepColonySettings.Get.familyTreePedigreeStyle)
                return FamilyTreePedigreeDrawer.Measure(snap, includeTitle);
            float h = MeasureHeight(snap);
            if (!includeTitle) h = Mathf.Max(24f, h - 28f);
            return new Vector2(0f, h);
        }

        public static float MeasureHeight(FamilyTreeSnapshot snap)
        {
            if (snap == null) return 80f;
            float h = 28f;
            if (!FamilyTreeUtility.HasAnyKin(snap)) return h + 40f;
            if (snap.grandparents.Count > 0) h += NodeH + GapY + HeaderH;
            if (snap.parents.Count > 0) h += NodeH + GapY + HeaderH;
            h += NodeH + GapY + HeaderH; // self row
            if (snap.children.Count > 0) h += NodeH + GapY + HeaderH;
            if (snap.grandchildren.Count > 0) h += NodeH + GapY + HeaderH;
            if (snap.mentor != null || snap.apprentices.Count > 0) h += NodeH + HeaderH + 16f;
            return h + 16f;
        }

        public static void Draw(Rect rect, FamilyTreeSnapshot snap, Action<Pawn> onClick, bool drawTitle = true)
        {
            if (snap?.focus == null) return;
            if (DeepColonySettings.Get.familyTreePedigreeStyle)
            {
                FamilyTreePedigreeDrawer.Draw(rect, snap, onClick, drawTitle);
                return;
            }
            rect = new Rect(rect.x + PadX, rect.y, Mathf.Max(1f, rect.width - PadX * 2f), rect.height);
            float y = rect.y;
            if (drawTitle)
            {
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(rect.x, y, rect.width, 24f),
                    "DC_FamilyTree_Title".Translate(snap.focus.LabelShort.Named("PAWN")));
                y += 26f;
            }

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
            DrawHeader(rect, y, header);
            y += HeaderH;
            y = DrawCenteredNodes(rect, y, pawns, focus, onClick);
            return y + GapY;
        }

        private static float DrawSelfRow(Rect rect, float y, FamilyTreeSnapshot snap, Action<Pawn> onClick)
        {
            DrawHeader(rect, y, "DC_FamilyTree_Generation".Translate());
            y += HeaderH;
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
            DrawHeader(rect, y, "DC_FamilyTree_Teaching".Translate());
            y += HeaderH;
            var row = new List<Pawn>();
            if (snap.mentor != null) row.Add(snap.mentor);
            for (int i = 0; i < snap.apprentices.Count; i++)
                row.Add(snap.apprentices[i]);
            return DrawCenteredNodes(rect, y, row, snap.focus, onClick);
        }

        private static void DrawHeader(Rect rect, float y, string header)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, y, rect.width, HeaderH), header);
            Text.Anchor = TextAnchor.UpperLeft;
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

        internal static void DrawNode(Rect rect, Pawn pawn, Pawn focus, Action<Pawn> onClick)
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

            Rect icon = new Rect(rect.x + 4f, rect.y + (rect.height - 24f) / 2f, 24f, 24f);
            Widgets.ThingIcon(icon, pawn);

            Rect text = new Rect(rect.x + 32f, rect.y + 4f, rect.width - 36f, rect.height - 8f);
            string name = pawn.LabelShortCap;
            if (pawn.Dead) name += " " + "DC_FamilyTree_Dead".Translate();
            string rel = FamilyTreeUtility.RelationLabel(focus, pawn);
            GUI.color = pawn.Dead ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(text, name + "\n" + rel);
            Text.Anchor = TextAnchor.UpperLeft;
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

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    public class MainTabWindow_DeepColonyPerks : MainTabWindow
    {
        private Vector2 scrollPos;

        public override Vector2 RequestedTabSize => new Vector2(720f, 540f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "DC_PerkOverviewTitle".Translate());
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(inRect.x, inRect.y + 36f, inRect.width, inRect.height - 36f);
            var rows = new List<Pawn>();
            if (Find.CurrentMap != null)
            {
                foreach (Pawn p in Find.CurrentMap.mapPawns.FreeColonists)
                    rows.Add(p);
            }
            // Include caravan / other maps lightly
            foreach (Map map in Find.Maps)
            {
                if (map == Find.CurrentMap) continue;
                foreach (Pawn p in map.mapPawns.FreeColonists)
                    rows.Add(p);
            }

            rows = rows.OrderByDescending(p =>
            {
                var c = p.TryGetComp<Comp_DeepColony>();
                return c?.availablePerkPoints ?? 0;
            }).ThenBy(p => p.LabelShort).ToList();

            float rowH = 28f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(rows.Count * rowH + 8f, outRect.height));
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            float y = 0f;
            Widgets.Label(new Rect(4f, y, 180f, rowH), "DC_PerkOverview_ColName".Translate());
            Widgets.Label(new Rect(190f, y, 80f, rowH), "DC_PerkOverview_ColPoints".Translate());
            Widgets.Label(new Rect(280f, y, 80f, rowH), "DC_PerkOverview_ColUnlocked".Translate());
            Widgets.Label(new Rect(370f, y, 200f, rowH), "DC_PerkOverview_ColStatus".Translate());
            y += rowH;
            Widgets.DrawLineHorizontal(0f, y, viewRect.width);
            y += 4f;

            foreach (Pawn p in rows)
            {
                var comp = p.TryGetComp<Comp_DeepColony>();
                int points = comp?.availablePerkPoints ?? 0;
                int unlocked = comp?.unlockedPerkDefNames?.Count ?? 0;

                if (points > 0)
                    GUI.color = new Color(0.95f, 0.8f, 0.2f);
                Widgets.Label(new Rect(4f, y, 180f, rowH), p.LabelShort);
                Widgets.Label(new Rect(190f, y, 80f, rowH), points.ToString());
                Widgets.Label(new Rect(280f, y, 80f, rowH), unlocked.ToString());
                GUI.color = Color.white;

                string status = points > 0
                    ? "DC_PerkOverview_Unspent".Translate()
                    : "DC_PerkOverview_CaughtUp".Translate();
                Widgets.Label(new Rect(370f, y, 200f, rowH), status);

                Rect btn = new Rect(viewRect.width - 110f, y + 2f, 100f, rowH - 4f);
                if (comp != null && Widgets.ButtonText(btn, "DC_ViewPerks".Translate()))
                    Find.WindowStack.Add(new Window_PerkTree(p));

                y += rowH;
            }

            Widgets.EndScrollView();
        }
    }
}

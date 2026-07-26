using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Dialog showing a pawn's perk tree organized by skill. Each skill row has up to 2 perk
    /// slots (level 5 and level 15). Unlocked perks are tinted green, available ones are
    /// highlighted in gold, and locked ones are grey.
    /// </summary>
    public class Window_PerkTree : Window
    {
        private readonly Pawn pawn;
        private readonly Comp_DeepColony comp;
        private Vector2 scrollPos;

        private static readonly Color ColorUnlocked = new Color(0.4f, 0.8f, 0.4f, 1f);
        private static readonly Color ColorAvailable = new Color(0.9f, 0.75f, 0.2f, 1f);
        private static readonly Color ColorLocked = new Color(0.4f, 0.4f, 0.4f, 1f);

        private const float WindowWidth = 860f;
        private const float WindowHeight = 680f;
        private const float HeaderHeight = 48f;
        private const float RowHeight = 90f;
        private const float PerkNodeW = 190f;
        private const float PerkNodeH = 74f;
        private const float SkillLabelW = 120f;
        private const float ArrowW = 24f;

        public override Vector2 InitialSize => new Vector2(WindowWidth, WindowHeight);

        public Window_PerkTree(Pawn pawn)
        {
            this.pawn = pawn;
            comp = pawn.TryGetComp<Comp_DeepColony>();
            doCloseButton = true;
            doCloseX = true;
            absorbInputAroundWindow = false;
            draggable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (comp == null) { Close(); return; }

            // ── Header ─────────────────────────────────────────────────────────────
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 200f, HeaderHeight),
                "DC_PerkTreeTitle".Translate(pawn.LabelShort.Named("PAWN")));
            Text.Font = GameFont.Small;

            Rect pointsRect = new Rect(inRect.xMax - 200f, inRect.y, 195f, HeaderHeight);
            string pointsText = "DC_PerkPoints".Translate(comp.availablePerkPoints);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(pointsRect, pointsText);
            Text.Anchor = TextAnchor.UpperLeft;

            // Divider
            float divY = inRect.y + HeaderHeight + 4f;
            Widgets.DrawLineHorizontal(inRect.x, divY, inRect.width);

            // ── Scroll view ────────────────────────────────────────────────────────
            float scrollY = divY + 6f;
            Rect outRect = new Rect(inRect.x, scrollY, inRect.width, inRect.yMax - scrollY - 40f);

            // Group perks by skill
            var skillGroups = DefDatabase<PerkDef>.AllDefs
                .Where(p => p.skill != null)
                .GroupBy(p => p.skill)
                .OrderBy(g => g.Key.listOrder)
                .ToList();

            float viewH = skillGroups.Count * RowHeight + 10f;
            Rect viewRect = new Rect(0, 0, outRect.width - 16f, viewH);

            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
            float y = 0f;
            foreach (var group in skillGroups)
            {
                DrawSkillRow(viewRect.width, y, group.Key,
                    group.OrderBy(p => p.requiredLevel).ToList());
                y += RowHeight;
            }
            Widgets.EndScrollView();
        }

        private void DrawSkillRow(float rowWidth, float y, SkillDef skill, List<PerkDef> perks)
        {
            // Skill label
            Rect skillRect = new Rect(4f, y + (RowHeight - 22f) / 2f, SkillLabelW, 22f);
            Text.Font = GameFont.Small;
            Widgets.Label(skillRect, skill.LabelCap);

            float xCursor = SkillLabelW + 8f;
            for (int i = 0; i < perks.Count; i++)
            {
                PerkDef perk = perks[i];

                // Arrow connector
                if (i > 0)
                {
                    Rect arrowRect = new Rect(xCursor, y + (RowHeight - 16f) / 2f, ArrowW, 16f);
                    GUI.color = Color.gray;
                    Widgets.Label(arrowRect, "→");
                    GUI.color = Color.white;
                    xCursor += ArrowW;
                }

                Rect nodeRect = new Rect(xCursor, y + (RowHeight - PerkNodeH) / 2f,
                    PerkNodeW, PerkNodeH);
                DrawPerkNode(nodeRect, perk);
                xCursor += PerkNodeW + 4f;
            }
        }

        private void DrawPerkNode(Rect r, PerkDef perk)
        {
            bool unlocked = comp.HasPerk(perk);
            bool canUnlock = comp.CanUnlock(perk);
            bool meetsLevel = pawn.skills?.GetSkill(perk.skill)?.Level >= perk.requiredLevel;

            Color bgColor = unlocked ? ColorUnlocked
                : canUnlock ? ColorAvailable
                : ColorLocked;

            Widgets.DrawBoxSolid(r, bgColor * 0.3f);
            Widgets.DrawBox(r, unlocked ? 2 : 1);
            GUI.color = bgColor;
            Widgets.DrawBox(r, 1);
            GUI.color = Color.white;

            // Perk name
            Text.Font = GameFont.Tiny;
            Rect nameRect = new Rect(r.x + 4f, r.y + 4f, r.width - 8f, 20f);
            Widgets.Label(nameRect, perk.LabelCap);

            // Level requirement
            Text.Font = GameFont.Tiny;
            Rect reqRect = new Rect(r.x + 4f, r.y + 22f, r.width - 8f, 18f);
            GUI.color = meetsLevel ? Color.white : new Color(1f, 0.4f, 0.4f);
            Widgets.Label(reqRect,
                "DC_PerkRequires".Translate(perk.skill.LabelCap, perk.requiredLevel));
            GUI.color = Color.white;

            // Unlock button or status
            Rect btnRect = new Rect(r.x + 4f, r.yMax - 24f, r.width - 8f, 20f);
            if (unlocked)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = ColorUnlocked;
                Widgets.Label(btnRect, "DC_PerkStatus_Unlocked".Translate());
                GUI.color = Color.white;
            }
            else if (canUnlock)
            {
                Text.Font = GameFont.Tiny;
                if (Widgets.ButtonText(btnRect, "DC_PerkUnlockBtn".Translate()))
                    comp.UnlockPerk(perk);
            }
            else
            {
                Text.Font = GameFont.Tiny;
                GUI.color = ColorLocked;
                Widgets.Label(btnRect, meetsLevel
                    ? "DC_PerkStatus_NeedPrereq".Translate()
                    : "DC_PerkStatus_NeedLevel".Translate());
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;

            // Tooltip
            if (Mouse.IsOver(r))
            {
                string tip = perk.description.NullOrEmpty()
                    ? perk.LabelCap.ToString()
                    : perk.description;
                TooltipHandler.TipRegion(r, new TipSignal(tip));
            }
        }
    }
}

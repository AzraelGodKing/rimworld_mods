using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Dialog showing a pawn's perk tree organized by skill.
    /// Supports auto-granted L5 → L10 → L15 (optional branch) → L20 capstone.
    /// </summary>
    public class Window_PerkTree : Window
    {
        private readonly Pawn pawn;
        private readonly Comp_DeepColony comp;
        private Vector2 scrollPos;

        private static readonly Color ColorUnlocked = new Color(0.4f, 0.8f, 0.4f, 1f);
        private static readonly Color ColorAvailable = new Color(0.9f, 0.75f, 0.2f, 1f);
        private static readonly Color ColorLocked = new Color(0.4f, 0.4f, 0.4f, 1f);

        private const float WindowWidth = 1180f;
        private const float WindowHeight = 720f;
        private const float HeaderHeight = 48f;
        private const float RowHeight = 96f;
        private const float PerkNodeW = 170f;
        private const float PerkNodeH = 80f;
        private const float SkillLabelW = 110f;
        private const float ArrowW = 20f;

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

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 200f, HeaderHeight),
                "DC_PerkTreeTitle".Translate(pawn.LabelShort.Named("PAWN")));
            Text.Font = GameFont.Small;

            Rect pointsRect = new Rect(inRect.xMax - 280f, inRect.y, 275f, HeaderHeight);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(pointsRect, "DC_PerkAutoHint".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            float divY = inRect.y + HeaderHeight + 4f;
            Widgets.DrawLineHorizontal(inRect.x, divY, inRect.width);

            float scrollY = divY + 6f;
            Rect outRect = new Rect(inRect.x, scrollY, inRect.width, inRect.yMax - scrollY - 40f);

            var skillGroups = DefDatabase<PerkDef>.AllDefs
                .Where(p => p.skill != null && Comp_DeepColony.PerkVisible(p))
                .GroupBy(p => p.skill)
                .OrderBy(g => g.Key.listOrder)
                .ToList();

            float viewH = skillGroups.Count * RowHeight + 10f;
            Rect viewRect = new Rect(0, 0, outRect.width - 16f, viewH);

            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
            float y = 0f;
            foreach (var group in skillGroups)
            {
                DrawSkillRow(viewRect.width, y, group.Key, OrderPerks(group.ToList()));
                y += RowHeight;
            }
            Widgets.EndScrollView();
        }

        private static List<PerkDef> OrderPerks(List<PerkDef> perks)
        {
            return perks
                .OrderBy(p => p.requiredLevel)
                .ThenBy(p => p.alternateBranch ? 1 : 0)
                .ThenBy(p => p.defName)
                .ToList();
        }

        private void DrawSkillRow(float rowWidth, float y, SkillDef skill, List<PerkDef> perks)
        {
            Rect skillRect = new Rect(4f, y + (RowHeight - 22f) / 2f, SkillLabelW, 22f);
            Text.Font = GameFont.Small;
            Widgets.Label(skillRect, skill.LabelCap);

            float xCursor = SkillLabelW + 8f;
            int lastLevel = -1;
            for (int i = 0; i < perks.Count; i++)
            {
                PerkDef perk = perks[i];
                if (i > 0)
                {
                    Rect arrowRect = new Rect(xCursor, y + (RowHeight - 16f) / 2f, ArrowW, 16f);
                    GUI.color = Color.gray;
                    // Branch siblings at same level get "/" instead of arrow.
                    Widgets.Label(arrowRect, perk.requiredLevel == lastLevel ? "/" : "→");
                    GUI.color = Color.white;
                    xCursor += ArrowW;
                }

                Rect nodeRect = new Rect(xCursor, y + (RowHeight - PerkNodeH) / 2f,
                    PerkNodeW, PerkNodeH);
                DrawPerkNode(nodeRect, perk);
                xCursor += PerkNodeW + 4f;
                lastLevel = perk.requiredLevel;
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

            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(r.x + 4f, r.y + 3f, r.width - 8f, 18f), perk.LabelCap);

            GUI.color = meetsLevel ? Color.white : new Color(1f, 0.4f, 0.4f);
            Widgets.Label(new Rect(r.x + 4f, r.y + 20f, r.width - 8f, 16f),
                "DC_PerkRequires".Translate(perk.skill.LabelCap, perk.requiredLevel));
            GUI.color = Color.white;

            Rect btnRect = new Rect(r.x + 4f, r.yMax - 22f, r.width - 8f, 18f);
            if (unlocked)
            {
                GUI.color = ColorUnlocked;
                Widgets.Label(btnRect, "DC_PerkStatus_Unlocked".Translate());
                GUI.color = Color.white;
            }
            else if (comp.CanSwitchTo(perk))
            {
                if (Widgets.ButtonText(btnRect, "DC_PerkSwitchBtn".Translate()))
                    comp.SwitchToPerk(perk);
            }
            else
            {
                GUI.color = ColorLocked;
                Widgets.Label(btnRect, meetsLevel
                    ? "DC_PerkStatus_NeedPrereq".Translate()
                    : "DC_PerkStatus_NeedLevel".Translate());
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;
            if (Mouse.IsOver(r))
                TooltipHandler.TipRegion(r, new TipSignal(PerkTipUtility.TipFor(perk)));
        }
    }
}

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
        private int filterMode; // 0 all, 1 unspent, 2 hard-only
        private SkillDef filterSkill;

        public override Vector2 RequestedTabSize => new Vector2(760f, 580f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 28f),
                "DC_PerkOverviewTitle".Translate());
            Text.Font = GameFont.Small;

            float fy = inRect.y + 30f;
            float bw = 110f;
            if (Widgets.ButtonText(new Rect(inRect.x, fy, bw, 26f),
                    FilterLabel(0)))
                filterMode = 0;
            if (Widgets.ButtonText(new Rect(inRect.x + bw + 6f, fy, bw, 26f),
                    FilterLabel(1)))
                filterMode = 1;
            if (Widgets.ButtonText(new Rect(inRect.x + 2f * (bw + 6f), fy, bw + 20f, 26f),
                    FilterLabel(2)))
                filterMode = 2;

            Rect skillRect = new Rect(inRect.x + 3f * (bw + 6f) + 24f, fy, 180f, 26f);
            string skillLabel = filterSkill == null
                ? "DC_PerkFilter_AllSkills".Translate().ToString()
                : filterSkill.LabelCap.ToString();
            if (Widgets.ButtonText(skillRect, skillLabel))
            {
                var opts = new List<FloatMenuOption>
                {
                    new FloatMenuOption("DC_PerkFilter_AllSkills".Translate(), () => filterSkill = null)
                };
                foreach (SkillDef s in DefDatabase<SkillDef>.AllDefs.OrderBy(d => d.listOrder))
                {
                    SkillDef local = s;
                    opts.Add(new FloatMenuOption(local.LabelCap, () => filterSkill = local));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }

            Rect outRect = new Rect(inRect.x, fy + 32f, inRect.width, inRect.yMax - (fy + 32f));
            var rows = new List<Pawn>();
            if (Find.CurrentMap != null)
            {
                foreach (Pawn p in Find.CurrentMap.mapPawns.FreeColonists)
                    rows.Add(p);
            }
            foreach (Map map in Find.Maps)
            {
                if (map == Find.CurrentMap) continue;
                foreach (Pawn p in map.mapPawns.FreeColonists)
                    rows.Add(p);
            }

            rows = rows.Where(MatchesFilter).OrderByDescending(p =>
            {
                var c = p.TryGetComp<Comp_DeepColony>();
                return c?.availablePerkPoints ?? 0;
            }).ThenBy(p => p.LabelShort).ToList();

            float rowH = 28f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(rows.Count * rowH + 40f, outRect.height));
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

        private string FilterLabel(int mode)
        {
            string key = mode == 1 ? "DC_PerkFilter_Unspent"
                : mode == 2 ? "DC_PerkFilter_Hard"
                : "DC_PerkFilter_All";
            string label = key.Translate();
            return filterMode == mode ? "[" + label + "]" : label;
        }

        private bool MatchesFilter(Pawn p)
        {
            var comp = p.TryGetComp<Comp_DeepColony>();
            if (comp == null) return false;
            if (filterMode == 1 && comp.availablePerkPoints <= 0) return false;
            if (filterMode == 2 && !HasHardNode(comp)) return false;
            if (filterSkill != null && !TouchesSkill(comp, filterSkill)) return false;
            return true;
        }

        private static bool HasHardNode(Comp_DeepColony comp)
        {
            if (comp.unlockedPerkDefNames == null) return false;
            for (int i = 0; i < comp.unlockedPerkDefNames.Count; i++)
            {
                PerkDef perk = DefDatabase<PerkDef>.GetNamedSilentFail(comp.unlockedPerkDefNames[i]);
                if (perk != null && IsHard(perk)) return true;
            }
            foreach (PerkDef perk in DefDatabase<PerkDef>.AllDefsListForReading)
            {
                if (perk != null && IsHard(perk) && Comp_DeepColony.PerkVisible(perk) && comp.CanUnlock(perk))
                    return true;
            }
            return false;
        }

        private static bool IsHard(PerkDef perk)
        {
            return perk.capstone || perk.alternateBranch || perk.requiredLevel >= 20;
        }

        private static bool TouchesSkill(Comp_DeepColony comp, SkillDef skill)
        {
            if (comp.unlockedPerkDefNames != null)
            {
                for (int i = 0; i < comp.unlockedPerkDefNames.Count; i++)
                {
                    PerkDef perk = DefDatabase<PerkDef>.GetNamedSilentFail(comp.unlockedPerkDefNames[i]);
                    if (perk?.skill == skill) return true;
                }
            }
            var pawn = comp.Pawn;
            if (pawn?.skills == null) return false;
            SkillRecord rec = pawn.skills.GetSkill(skill);
            return rec != null && rec.Level >= 5;
        }
    }
}

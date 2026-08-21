using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    public class MainTabWindow_DeepColonyLegacy : MainTabWindow
    {
        private Vector2 scrollPos;

        public override Vector2 RequestedTabSize => new Vector2(640f, 480f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "DC_LegacyTitle".Translate());
            Text.Font = GameFont.Small;

            var gameComp = GameComp_DeepColony.Instance;
            string surname = gameComp?.GetFounderSurname() ?? "—";

            float y = inRect.y + 36f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "DC_LegacySurname".Translate(surname));
            y += 26f;

            var founders = new List<Pawn>();
            var living = new List<Pawn>();
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonists)
                {
                    living.Add(p);
                    if (p.Name is NameTriple t
                        && !surname.NullOrEmpty()
                        && surname != "—"
                        && t.Last == surname)
                        founders.Add(p); // living with founder surname (proxy)
                }
            }

            // Founders: earliest colonists approx — those who match surname OR were first
            int withPerks = living.Count(p =>
            {
                var c = p.TryGetComp<Comp_DeepColony>();
                return c != null && c.unlockedPerkDefNames.Count > 0;
            });
            int apprentices = living.Count(p => p.TryGetComp<Comp_DeepColony>()?.mentor != null);
            int elders = living.Count(ElderUtility.IsElder);

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "DC_LegacyStats".Translate(living.Count, withPerks, apprentices, elders));
            y += 28f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "DC_LegacyColonists".Translate());
            y += 24f;

            var gcLetters = gameComp?.familyLetters;
            if (gcLetters != null && gcLetters.Count > 0)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                    "DC_LegacyLetters".Translate());
                y += 22f;
                int shown = 0;
                for (int i = gcLetters.Count - 1; i >= 0 && shown < 4; i--, shown++)
                {
                    FamilyLetterEntry e = gcLetters[i];
                    Widgets.Label(new Rect(inRect.x, y, inRect.width, 20f),
                        "  " + e.title);
                    y += 20f;
                }
                y += 6f;
            }

            Rect outRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            var rows = living.OrderBy(p => p.LabelShort).ToList();
            float rowH = 24f;
            Rect view = new Rect(0, 0, outRect.width - 16f, Mathf.Max(rows.Count * rowH, outRect.height));
            Widgets.BeginScrollView(outRect, ref scrollPos, view);
            float ry = 0f;
            foreach (Pawn p in rows)
            {
                var comp = p.TryGetComp<Comp_DeepColony>();
                var sb = new StringBuilder();
                sb.Append(p.LabelShort);
                if (p.Name is NameTriple nt && !nt.Last.NullOrEmpty())
                    sb.Append(" (").Append(nt.Last).Append(")");
                if (ElderUtility.IsElder(p)) sb.Append(" [elder]");
                if (comp?.familyTraditionSkillDefName != null)
                {
                    var skill = DefDatabase<SkillDef>.GetNamedSilentFail(comp.familyTraditionSkillDefName);
                    if (skill != null) sb.Append(" · tradition: ").Append(skill.label);
                }
                if (comp != null && comp.unlockedPerkDefNames.Count > 0)
                    sb.Append(" · perks ").Append(comp.unlockedPerkDefNames.Count);
                Widgets.Label(new Rect(4f, ry, view.width - 8f, rowH), sb.ToString());
                ry += rowH;
            }
            Widgets.EndScrollView();
        }
    }
}

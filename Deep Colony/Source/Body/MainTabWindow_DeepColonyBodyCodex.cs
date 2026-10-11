using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    public class MainButtonWorker_DeepColonyBodyCodex : MainButtonWorker_ToggleTab
    {
        public override bool Visible =>
            base.Visible && (DeepColonySettings.Get.enableBody || DeepColonySettings.Get.enableCodex);
    }

    /// <summary>Colony-wide view of body health and Codex notes, like the Perks/Legacy/Reputation tabs.</summary>
    public class MainTabWindow_DeepColonyBodyCodex : MainTabWindow
    {
        private const float LineH = 22f;

        private Vector2 scrollPos;
        private float lastViewH = 400f;
        private readonly List<Pawn> atRisk = new List<Pawn>();

        public override Vector2 RequestedTabSize => new Vector2(680f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "DC_BodyCodexTitle".Translate());
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(inRect.x, inRect.y + 36f, inRect.width, inRect.height - 36f);
            Rect view = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(lastViewH, outRect.height));
            Widgets.BeginScrollView(outRect, ref scrollPos, view);
            float y = 0f;
            DrawBody(view.width, ref y);
            y += 10f;
            DrawCodex(view.width, ref y);
            lastViewH = y + 10f;
            Widgets.EndScrollView();
        }

        private static void Heading(float width, ref float y, string label)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, width, 28f), label);
            Text.Font = GameFont.Small;
            y += 28f;
            Widgets.DrawLineHorizontal(0f, y, width);
            y += 4f;
        }

        private static void Line(float width, ref float y, string text)
        {
            Widgets.Label(new Rect(4f, y, width - 4f, LineH), text);
            y += LineH;
        }

        private static void DrawBody(float width, ref float y)
        {
            Heading(width, ref y, "DC_BodyCodex_Body".Translate());
            if (!DeepColonySettings.Get.enableBody)
            {
                Line(width, ref y, "DC_BodyCodex_BodyOff".Translate());
                return;
            }

            HediffDef conval = DefDatabase<HediffDef>.GetNamedSilentFail("DC_Hediff_Convalescence");
            HediffDef weakness = DefDatabase<HediffDef>.GetNamedSilentFail("DC_Hediff_LingeringWeakness");
            int shown = 0;
            foreach (Pawn p in ColonyPawns())
            {
                var parts = new List<string>();
                var hediffs = p.health?.hediffSet?.hediffs;
                if (hediffs == null) continue;
                for (int i = 0; i < hediffs.Count; i++)
                {
                    Hediff h = hediffs[i];
                    if (MapComponent_DeepColonyBody.IsContagious(h.def))
                        parts.Add("DC_BodyCodex_Contagious".Translate(h.LabelBase, h.Severity.ToStringPercent()));
                    else if (h.def == conval)
                        parts.Add("DC_BodyCodex_Convalescing".Translate(h.Severity.ToStringPercent()));
                    else if (h.def == weakness)
                        parts.Add("DC_BodyCodex_Weakness".Translate());
                }
                if (parts.Count == 0) continue;
                if (p.Spawned && BodyRooms.IsInfirmary(p.GetRoom()))
                    parts.Add("DC_BodyCodex_InInfirmary".Translate());

                FamilyTreeUtility.DrawClickablePawnName(new Rect(4f, y, 160f, LineH), p);
                Widgets.Label(new Rect(170f, y, width - 170f, LineH), string.Join(", ", parts));
                y += LineH;
                shown++;
            }
            if (shown == 0)
                Line(width, ref y, "DC_BodyCodex_BodyNone".Translate());
        }

        private void DrawCodex(float width, ref float y)
        {
            Heading(width, ref y, "DC_BodyCodex_Codex".Translate());
            if (!DeepColonySettings.Get.enableCodex)
            {
                Line(width, ref y, "DC_BodyCodex_CodexOff".Translate());
                return;
            }

            ResearchProjectDef proj = CodexArchive.ProjectAtRisk();
            Line(width, ref y, proj != null
                ? "DC_BodyCodex_Project".Translate(proj.LabelCap)
                : "DC_BodyCodex_NoProject".Translate());

            ThingDef archive = DefDatabase<ThingDef>.GetNamedSilentFail("DC_Archive");
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome) continue;
                int count = archive != null ? map.listerBuildings.AllBuildingsColonistOfDef(archive).Count() : 0;
                Line(width, ref y, "DC_BodyCodex_Archives".Translate(map.Parent?.LabelCap ?? "—", count));
            }

            CodexArchive.AtRiskResearchers(atRisk);
            if (atRisk.Count > 0)
            {
                GUI.color = new Color(1f, 0.75f, 0.4f);
                Line(width, ref y, "DC_BodyCodex_AtRisk".Translate(
                    string.Join(", ", atRisk.Select(p => p.LabelShort))));
                GUI.color = Color.white;
            }

            y += 6f;
            Widgets.Label(new Rect(0f, y, width, LineH), "DC_BodyCodex_Notebooks".Translate());
            y += LineH;
            ThingDef notebookDef = DefDatabase<ThingDef>.GetNamedSilentFail("DC_CodexNotebook");
            int shown = 0;
            if (notebookDef != null)
            {
                foreach (Map map in Find.Maps)
                {
                    if (!map.IsPlayerHome) continue;
                    foreach (Thing t in map.listerThings.ThingsOfDef(notebookDef))
                    {
                        string holds = t.TryGetComp<CompCodexNotebook>()?.CompInspectStringExtra() ?? "";
                        Rect r = new Rect(4f, y, width - 4f, LineH);
                        Widgets.Label(r, t.LabelCap + " — " + holds);
                        if (Widgets.ButtonInvisible(r))
                            CameraJumper.TryJumpAndSelect(t);
                        y += LineH;
                        shown++;
                    }
                }
            }
            if (shown == 0)
                Line(width, ref y, "DC_BodyCodex_NoNotebooks".Translate());
        }

        private static IEnumerable<Pawn> ColonyPawns()
        {
            var all = new List<Pawn>();
            foreach (Map map in Find.Maps)
                all.AddRange(map.mapPawns.FreeColonists);
            return all.OrderBy(p => p.LabelShort);
        }
    }
}

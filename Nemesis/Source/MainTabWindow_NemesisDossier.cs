using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public class MainTabWindow_NemesisDossier : MainTabWindow
    {
        private Vector2 scrollPos;

        public override Vector2 RequestedTabSize => new Vector2(620f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            NemesisData data = comp?.Data;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Nemesis_Dossier_Title".Translate());
            Text.Font = GameFont.Small;

            float y = inRect.y + 36f;
            if (data == null || (!data.active && data.truceUntilTick <= 0))
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 44f),
                    "Nemesis_Dossier_Empty".Translate());
                y += 52f;
                DrawEpitaphs(comp, inRect, ref y);
                return;
            }

            string name = data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Identity".Translate(
                    name,
                    data.factionName ?? "—",
                    data.FocusLabelKey.Translate()));
            y += 22f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Target".Translate(
                    data.targetPawnName ?? "Nemesis_Phrase_YourColony".Translate(),
                    data.EffectiveAggression.ToString("F1"),
                    data.escapeCount,
                    data.progressionLevel));
            y += 22f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 40f),
                "Nemesis_Dossier_Tells".Translate(
                    NemesisTells.VoiceKey(data.voice).Translate(),
                    NemesisTells.WeaponLabel(data),
                    NemesisTells.MarkKey(data.mark).Translate(),
                    NemesisTells.HabitKey(data.habit).Translate()));
            y += 42f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Tile".Translate(data.lastKnownTileLabel ?? "—"));
            y += 22f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Gear".Translate(data.lastGearSeen ?? "—"));
            y += 22f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Bounty".Translate(comp.bountySilver));
            y += 28f;

            if (data.active && (NemesisMod.Settings?.enableInformants ?? true))
            {
                int cost = NemesisInformants.LeadCost(data);
                if (Widgets.ButtonText(new Rect(inRect.x, y, 200f, 28f),
                        "Nemesis_Dossier_BuyLead".Translate(cost)))
                    NemesisInformants.TryBuyLead(Find.CurrentMap, out _);
                if (Widgets.ButtonText(new Rect(inRect.x + 210f, y, 200f, 28f),
                        "Nemesis_Dossier_PostBounty".Translate(cost)))
                    NemesisInformants.TryPostBounty(Find.CurrentMap, cost, out _);
                y += 36f;
            }

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Notes".Translate());
            y += 24f;

            Rect view = new Rect(inRect.x, y, inRect.width, inRect.yMax - y - 90f);
            List<NemesisNote> notes = data.notes;
            if (notes == null || notes.Count == 0)
            {
                Widgets.Label(view, "Nemesis_Dossier_NoNotes".Translate());
            }
            else
            {
                float innerH = notes.Count * 36f;
                Rect inner = new Rect(0f, 0f, view.width - 16f, innerH);
                Widgets.BeginScrollView(view, ref scrollPos, inner);
                float rowY = 0f;
                for (int i = notes.Count - 1; i >= 0; i--)
                {
                    Widgets.Label(new Rect(0f, rowY, inner.width, 34f), notes[i].text);
                    rowY += 36f;
                }
                Widgets.EndScrollView();
            }

            y = inRect.yMax - 86f;
            DrawEpitaphs(comp, inRect, ref y);
        }

        static void DrawEpitaphs(GameComponent_Nemesis comp, Rect inRect, ref float y)
        {
            List<NemesisEpitaph> list = comp?.epitaphs;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Epitaphs".Translate());
            y += 22f;
            if (list == null || list.Count == 0)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                    "Nemesis_Dossier_NoEpitaphs".Translate());
                return;
            }
            int shown = 0;
            for (int i = list.Count - 1; i >= 0 && shown < 3; i--, shown++)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 20f),
                    "  " + list[i].SummaryLine());
                y += 20f;
            }
        }
    }
}

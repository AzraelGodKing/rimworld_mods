using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public class MainTabWindow_NemesisDossier : MainTabWindow
    {
        /// <summary>Hunt shown in the dossier; the dev-mode actions use it too.</summary>
        public static int SelectedHuntId = -1;

        private Vector2 scrollPos;

        public override Vector2 RequestedTabSize => new Vector2(620f, 580f);

        public override void DoWindowContents(Rect inRect)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Nemesis_Dossier_Title".Translate());
            Text.Font = GameFont.Small;

            float y = inRect.y + 36f;
            NemesisData data = PickHunt(comp);
            if (data == null)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 44f),
                    "Nemesis_Dossier_Empty".Translate());
                y += 52f;
                DrawEpitaphs(comp, inRect, ref y);
                return;
            }

            DrawHuntTabs(comp, inRect, ref y);

            if (data.pendingResolution)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 44f),
                    "Nemesis_Dossier_PendingResolution".Translate(
                        data.nemesisName ?? "Nemesis_Phrase_Someone".Translate()));
                y += 52f;
                if (Widgets.ButtonText(new Rect(inRect.x, y, 240f, 28f),
                        "Nemesis_Dossier_OpenResolution".Translate())
                    && !comp.TryOpenResolutionDialog(data))
                    Messages.Message("Nemesis_Dossier_ResolutionUnavailable".Translate(),
                        MessageTypeDefOf.RejectInput, historical: false);
                y += 36f;
                DrawEpitaphs(comp, inRect, ref y);
                return;
            }

            string name = data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            if (!data.active)
            {
                // Truce (AZR-704): say so, with the countdown, instead of the live-hunt panel.
                int left = Mathf.Max(0, data.truceUntilTick - Find.TickManager.TicksGame);
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 44f),
                    "Nemesis_Dossier_Truce".Translate(name, left.ToStringTicksToPeriod()));
                y += 52f;
                DrawEpitaphs(comp, inRect, ref y);
                return;
            }

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Identity".Translate(
                    data.ColoredName,
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
            DrawLastKnownTile(data, new Rect(inRect.x, y, inRect.width, 22f));
            y += 22f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Gear".Translate(data.lastGearSeen ?? "—"));
            y += 22f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Bounty".Translate(data.bountySilver));
            y += 28f;

            if (NemesisMod.Settings?.enableInformants ?? true)
            {
                int cost = NemesisInformants.LeadCost(data);
                Map map = Find.CurrentMap;
                if (Widgets.ButtonText(new Rect(inRect.x, y, 200f, 28f),
                        "Nemesis_Dossier_BuyLead".Translate(cost)))
                    NemesisPlayerCommand.BuyLead(data.huntId, map);
                if (Widgets.ButtonText(new Rect(inRect.x + 210f, y, 200f, 28f),
                        "Nemesis_Dossier_PostBounty".Translate(cost)))
                    NemesisPlayerCommand.PostBounty(data.huntId, map, cost);
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

        /// <summary>The selected engaged hunt, else the first; null when none.</summary>
        private static NemesisData PickHunt(GameComponent_Nemesis comp)
        {
            if (comp == null) return null;
            NemesisData picked = comp.HuntById(SelectedHuntId);
            if (picked != null && picked.Engaged) return picked;
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            for (int i = 0; i < hunts.Count; i++)
            {
                if (hunts[i].Engaged)
                {
                    SelectedHuntId = hunts[i].huntId;
                    return hunts[i];
                }
            }
            return null;
        }

        /// <summary>One button per engaged hunt, in its faction's colour. Hidden with a single hunt.</summary>
        private static void DrawHuntTabs(GameComponent_Nemesis comp, Rect inRect, ref float y)
        {
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            int engaged = 0;
            for (int i = 0; i < hunts.Count; i++)
            {
                if (hunts[i].Engaged) engaged++;
            }
            if (engaged < 2) return;

            float x = inRect.x;
            const float w = 180f;
            for (int i = 0; i < hunts.Count; i++)
            {
                NemesisData h = hunts[i];
                if (!h.Engaged) continue;
                Rect r = new Rect(x, y, w, 26f);
                bool selected = h.huntId == SelectedHuntId;
                if (selected)
                    Widgets.DrawHighlightSelected(r);
                if (Widgets.ButtonText(r, h.ColoredName, drawBackground: !selected))
                    SelectedHuntId = h.huntId;
                x += w + 6f;
            }
            y += 32f;
        }

        static void DrawLastKnownTile(NemesisData data, Rect rect)
        {
            string text = "Nemesis_Dossier_Tile".Translate(data.lastKnownTileLabel ?? "—");
            PlanetTile tile = data.lastKnownTile;
            if (!tile.Valid || Find.WorldGrid == null)
            {
                Widgets.Label(rect, text);
                return;
            }

            Rect clickRect = new Rect(rect.x, rect.y, Mathf.Min(rect.width, Text.CalcSize(text).x + 8f), rect.height);
            Widgets.DrawHighlightIfMouseover(clickRect);
            TooltipHandler.TipRegion(clickRect, "Nemesis_Dossier_TileJumpTip".Translate());
            GUI.color = Widgets.NormalOptionColor;
            Widgets.Label(rect, text);
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(clickRect))
                CameraJumper.TryJump(new GlobalTargetInfo(tile));
        }

        static void DrawEpitaphs(GameComponent_Nemesis comp, Rect inRect, ref float y)
        {
            List<NemesisEpitaph> list = comp?.epitaphs;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Nemesis_Dossier_Epitaphs".Translate());
            if (list != null && list.Count > 3)
            {
                const float buttonWidth = 160f;
                if (Widgets.ButtonText(new Rect(inRect.xMax - buttonWidth, y, buttonWidth, 22f),
                        "Nemesis_Dossier_EpitaphArchive".Translate(list.Count)))
                    Find.WindowStack.Add(new Dialog_NemesisEpitaphs(list));
            }
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

using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace LivingWorld
{
    public class MainTabWindow_LivingWorldChronicle : MainTabWindow
    {
        private Vector2 scrollPos;
        private Vector2 stateScrollPos;
        private string factionFilter = string.Empty;
        private int channelFilter; // 0 all, 1 rumour, 2 proximity, 3 radio
        private int severityFilter; // 0 all, 1 normal+, 2 major only
        private bool showWorldState;

        public override Vector2 RequestedTabSize => new Vector2(720f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 170f, 28f),
                showWorldState
                    ? "LivingWorld_Chronicle_ViewWorldState".Translate()
                    : "LivingWorld_Chronicle_Title".Translate());
            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(new Rect(inRect.xMax - 160f, inRect.y, 160f, 28f),
                    showWorldState
                        ? "LivingWorld_Chronicle_ViewChronicle".Translate()
                        : "LivingWorld_Chronicle_ViewWorldState".Translate()))
            {
                showWorldState = !showWorldState;
            }

            float y = inRect.y + 32f;
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (showWorldState)
            {
                DrawWorldState(new Rect(inRect.x, y, inRect.width, inRect.yMax - y), comp);
                return;
            }

            Widgets.Label(new Rect(inRect.x, y, 80f, 24f), "LivingWorld_Chronicle_Filter".Translate());
            factionFilter = Widgets.TextField(new Rect(inRect.x + 84f, y, 220f, 24f), factionFilter ?? string.Empty);
            if (Widgets.ButtonText(new Rect(inRect.x + 314f, y, 140f, 24f), ChannelLabel()))
            {
                channelFilter = (channelFilter + 1) % 4;
            }
            if (Widgets.ButtonText(new Rect(inRect.x + 462f, y, 160f, 24f), SeverityLabel()))
            {
                severityFilter = (severityFilter + 1) % 3;
            }
            y += 32f;

            IReadOnlyList<WorldEvent> all = comp?.Chronicle;
            if (all == null || all.Count == 0)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 40f),
                    "LivingWorld_Chronicle_Empty".Translate());
                return;
            }

            List<WorldEvent> shown = new List<WorldEvent>();
            for (int i = all.Count - 1; i >= 0; i--)
            {
                WorldEvent ev = all[i];
                if (ev == null)
                {
                    continue;
                }
                if (!MatchesFaction(ev) || !MatchesChannel(ev) || !MatchesSeverity(ev))
                {
                    continue;
                }
                shown.Add(ev);
            }

            Rect outRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            float viewH = shown.Count * 72f + 8f;
            Widgets.BeginScrollView(outRect, ref scrollPos, new Rect(0f, 0f, outRect.width - 16f, viewH));
            float ly = 0f;
            for (int i = 0; i < shown.Count; i++)
            {
                WorldEvent ev = shown[i];
                Rect row = new Rect(0f, ly, outRect.width - 20f, 70f);
                // AZR-333 — clickable row jumps the world camera to the event tile.
                if (ev.tile >= 0)
                {
                    Widgets.DrawHighlightIfMouseover(row);
                    if (Widgets.ButtonInvisible(row))
                    {
                        CameraJumper.TryJump(new GlobalTargetInfo(ev.tile));
                    }
                }
                string when = (Find.TickManager.TicksGame - ev.tick).ToStringTicksToPeriod();
                string head = LivingWorldLetters.LabelFor(ev.kind) + "  (" + when + ")";
                if (ev.corrected)
                {
                    head += "  " + "LivingWorld_Chronicle_Corrected".Translate();
                }
                Widgets.Label(new Rect(0f, ly, outRect.width - 20f, 22f), head);
                ly += 22f;
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                Widgets.Label(new Rect(0f, ly, outRect.width - 20f, 44f),
                    LivingWorldLetters.PrefixFor(ev) + "  " + LivingWorldLetters.TextFor(ev));
                GUI.color = Color.white;
                ly += 48f;
            }
            Widgets.EndScrollView();
        }

        // Read-only: EnsurePairs mutates sim state, so the UI only shows what the sim already tracks.
        private void DrawWorldState(Rect rect, GameComponent_LivingWorld comp)
        {
            IReadOnlyList<FactionPairState> pairs = comp?.Pairs;
            if (pairs == null || pairs.Count == 0)
            {
                Widgets.Label(new Rect(rect.x, rect.y, rect.width, 40f),
                    "LivingWorld_WorldState_Empty".Translate());
                return;
            }

            int wars = 0, tensions = 0, alliances = 0, peace = 0;
            List<FactionPairState> shown = new List<FactionPairState>();
            for (int i = 0; i < pairs.Count; i++)
            {
                FactionPairState p = pairs[i];
                if (p == null)
                {
                    continue;
                }
                switch (p.tone)
                {
                    case FactionRelationTone.War:
                        wars++;
                        break;
                    case FactionRelationTone.Tension:
                        tensions++;
                        break;
                    case FactionRelationTone.Alliance:
                        alliances++;
                        break;
                    default:
                        peace++;
                        continue;
                }
                shown.Add(p);
            }
            shown.Sort((a, b) =>
            {
                int byTone = ToneOrder(a.tone).CompareTo(ToneOrder(b.tone));
                return byTone != 0 ? byTone : b.intensity.CompareTo(a.intensity);
            });

            float y = rect.y;
            Widgets.Label(new Rect(rect.x, y, rect.width, 24f),
                "LivingWorld_WorldState_Summary".Translate(wars, tensions, alliances, peace));
            y += 24f;
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "LivingWorld_WorldState_PeaceHidden".Translate());
            GUI.color = Color.white;
            y += 28f;

            Rect outRect = new Rect(rect.x, y, rect.width, rect.yMax - y);
            float viewH = shown.Count * 26f + 8f;
            Widgets.BeginScrollView(outRect, ref stateScrollPos, new Rect(0f, 0f, outRect.width - 16f, viewH));
            float ly = 0f;
            for (int i = 0; i < shown.Count; i++)
            {
                FactionPairState p = shown[i];
                string a = p.FactionA()?.Name ?? "LivingWorld_UnknownFaction".Translate().ToString();
                string b = p.FactionB()?.Name ?? "LivingWorld_UnknownFaction".Translate().ToString();
                string line = a + " — " + b + ": " + ToneLabel(p.tone);
                if (p.tone != FactionRelationTone.Alliance)
                {
                    line += ", " + "LivingWorld_WorldState_Intensity".Translate(p.intensity);
                }
                if (p.TradeBlackoutActive)
                {
                    line += ", " + "LivingWorld_WorldState_Blackout".Translate();
                }
                GUI.color = ToneColor(p.tone);
                Widgets.Label(new Rect(0f, ly, outRect.width - 20f, 24f), line);
                GUI.color = Color.white;
                ly += 26f;
            }
            Widgets.EndScrollView();
        }

        private static int ToneOrder(FactionRelationTone tone)
        {
            switch (tone)
            {
                case FactionRelationTone.War:
                    return 0;
                case FactionRelationTone.Tension:
                    return 1;
                case FactionRelationTone.Alliance:
                    return 2;
                default:
                    return 3;
            }
        }

        private static string ToneLabel(FactionRelationTone tone)
        {
            return ("LivingWorld_Tone_" + tone).Translate();
        }

        private static Color ToneColor(FactionRelationTone tone)
        {
            switch (tone)
            {
                case FactionRelationTone.War:
                    return new Color(0.95f, 0.55f, 0.5f);
                case FactionRelationTone.Tension:
                    return new Color(0.95f, 0.85f, 0.5f);
                case FactionRelationTone.Alliance:
                    return new Color(0.6f, 0.9f, 0.6f);
                default:
                    return Color.white;
            }
        }

        private bool MatchesFaction(WorldEvent ev)
        {
            if (string.IsNullOrEmpty(factionFilter))
            {
                return true;
            }
            string f = factionFilter.ToLowerInvariant();
            return (ev.factionAName ?? string.Empty).ToLowerInvariant().Contains(f)
                || (ev.factionBName ?? string.Empty).ToLowerInvariant().Contains(f)
                || (ev.settlementLabel ?? string.Empty).ToLowerInvariant().Contains(f);
        }

        private bool MatchesChannel(WorldEvent ev)
        {
            if (channelFilter == 0)
            {
                return true;
            }
            HearChannel want = channelFilter == 1 ? HearChannel.Rumour
                : channelFilter == 2 ? HearChannel.Proximity
                : HearChannel.Radio;
            return ev.channel == want;
        }

        private bool MatchesSeverity(WorldEvent ev)
        {
            switch (severityFilter)
            {
                case 1:
                    return ev.severity >= NewsSeverity.Normal;
                case 2:
                    return ev.severity == NewsSeverity.Major;
                default:
                    return true;
            }
        }

        private string ChannelLabel()
        {
            switch (channelFilter)
            {
                case 1:
                    return "LivingWorld_Channel_Rumour".Translate();
                case 2:
                    return "LivingWorld_Channel_Proximity".Translate();
                case 3:
                    return "LivingWorld_Channel_Radio".Translate();
                default:
                    return "LivingWorld_Chronicle_AllChannels".Translate();
            }
        }

        private string SeverityLabel()
        {
            switch (severityFilter)
            {
                case 1:
                    return "LivingWorld_Chronicle_SeverityNormal".Translate();
                case 2:
                    return "LivingWorld_Chronicle_SeverityMajor".Translate();
                default:
                    return "LivingWorld_Chronicle_SeverityAll".Translate();
            }
        }
    }
}

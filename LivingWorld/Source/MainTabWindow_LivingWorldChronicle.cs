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
        private bool heardOnly;
        private bool showWorldState;
        private bool jumpToB = true;

        // Filtered rows, rebuilt only when the chronicle or a filter changes.
        private readonly List<WorldEvent> shown = new List<WorldEvent>();
        private readonly List<float> rowTops = new List<float>();
        private readonly List<string> rowHeads = new List<string>();
        private readonly List<string> rowBodies = new List<string>();
        private float shownHeight;
        private int builtCount = -1;
        private int builtLastTick = -1;
        private int builtCorrections = -1;
        private string builtFilter;
        private int builtChannel = -1;
        private int builtSeverity = -1;
        private bool builtHeard;
        private float builtWidth = -1f;
        private int builtHour = -1;

        private readonly List<FactionPairState> statePairs = new List<FactionPairState>();

        public override Vector2 RequestedTabSize => new Vector2(760f, 600f);

        public override void PreOpen()
        {
            base.PreOpen();
            builtCount = -1;
        }

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

            Widgets.Label(new Rect(inRect.x, y, 70f, 24f), "LivingWorld_Chronicle_Filter".Translate());
            factionFilter = Widgets.TextField(new Rect(inRect.x + 72f, y, 170f, 24f), factionFilter ?? string.Empty);
            if (Widgets.ButtonText(new Rect(inRect.x + 246f, y, 30f, 24f), "▾"))
            {
                OpenFactionMenu();
            }
            if (Widgets.ButtonText(new Rect(inRect.x + 282f, y, 130f, 24f), ChannelLabel()))
            {
                channelFilter = (channelFilter + 1) % 4;
            }
            if (Widgets.ButtonText(new Rect(inRect.x + 416f, y, 150f, 24f), SeverityLabel()))
            {
                severityFilter = (severityFilter + 1) % 3;
            }
            Widgets.CheckboxLabeled(new Rect(inRect.x + 574f, y, inRect.width - 574f, 24f),
                "LivingWorld_Chronicle_HeardOnly".Translate(), ref heardOnly);
            y += 32f;

            IReadOnlyList<WorldEvent> all = comp?.Chronicle;
            if (all == null || all.Count == 0)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 40f),
                    "LivingWorld_Chronicle_Empty".Translate());
                return;
            }

            Rect outRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            float rowWidth = outRect.width - 20f;
            RebuildIfNeeded(all, rowWidth);
            if (shown.Count == 0)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 40f),
                    "LivingWorld_Chronicle_NoMatch".Translate());
                return;
            }

            Widgets.BeginScrollView(outRect, ref scrollPos, new Rect(0f, 0f, outRect.width - 16f, shownHeight));
            float top = scrollPos.y;
            float bottom = scrollPos.y + outRect.height;
            for (int i = 0; i < shown.Count; i++)
            {
                float ry = rowTops[i];
                float rh = (i + 1 < rowTops.Count ? rowTops[i + 1] : shownHeight) - ry;
                if (ry + rh < top || ry > bottom)
                {
                    continue; // off screen
                }
                WorldEvent ev = shown[i];
                Rect row = new Rect(0f, ry, rowWidth, rh - 4f);
                PlanetTile where = ev.Where;
                if (where.Valid)
                {
                    Widgets.DrawHighlightIfMouseover(row);
                    if (Widgets.ButtonInvisible(row))
                    {
                        CameraJumper.TryJump(new GlobalTargetInfo(where));
                    }
                }
                Widgets.Label(new Rect(0f, ry, rowWidth, 22f), rowHeads[i]);
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                Widgets.Label(new Rect(0f, ry + 22f, rowWidth, rh - 26f), rowBodies[i]);
                GUI.color = Color.white;
            }
            Widgets.EndScrollView();
        }

        private void RebuildIfNeeded(IReadOnlyList<WorldEvent> all, float rowWidth)
        {
            int lastTick = all[all.Count - 1]?.tick ?? 0;
            int corrections = CountCorrected(all);
            // "x days ago" text changes slowly; refresh it once per in-game hour.
            int hour = Find.TickManager.TicksGame / 2500;
            if (all.Count == builtCount && lastTick == builtLastTick && corrections == builtCorrections
                && factionFilter == builtFilter && channelFilter == builtChannel && severityFilter == builtSeverity
                && heardOnly == builtHeard && Mathf.Approximately(rowWidth, builtWidth) && hour == builtHour)
            {
                return;
            }
            builtCount = all.Count;
            builtLastTick = lastTick;
            builtCorrections = corrections;
            builtFilter = factionFilter;
            builtChannel = channelFilter;
            builtSeverity = severityFilter;
            builtHeard = heardOnly;
            builtWidth = rowWidth;
            builtHour = hour;

            shown.Clear();
            rowTops.Clear();
            rowHeads.Clear();
            rowBodies.Clear();
            string needle = string.IsNullOrEmpty(factionFilter) ? null : factionFilter;
            float y = 0f;
            int now = Find.TickManager.TicksGame;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                WorldEvent ev = all[i];
                if (ev == null || !MatchesFaction(ev, needle) || !MatchesChannel(ev) || !MatchesSeverity(ev)
                    || (heardOnly && !ev.seenByPlayer))
                {
                    continue;
                }
                string head = LivingWorldLetters.LabelFor(ev.kind) + "  ("
                    + (now - ev.tick).ToStringTicksToPeriod() + ")";
                if (ev.corrected)
                {
                    head += "  " + "LivingWorld_Chronicle_Corrected".Translate();
                }
                string body = LivingWorldLetters.PrefixFor(ev) + "  " + LivingWorldLetters.TextFor(ev);
                float bodyH = Mathf.Max(22f, Text.CalcHeight(body, rowWidth));
                shown.Add(ev);
                rowTops.Add(y);
                rowHeads.Add(head);
                rowBodies.Add(body);
                y += 22f + bodyH + 8f;
            }
            shownHeight = y + 8f;
        }

        private static int CountCorrected(IReadOnlyList<WorldEvent> all)
        {
            int n = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].corrected)
                {
                    n++;
                }
            }
            return n;
        }

        private void OpenFactionMenu()
        {
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("LivingWorld_Chronicle_AllFactions".Translate(), () => factionFilter = string.Empty),
            };
            List<Faction> factions = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < factions.Count; i++)
            {
                Faction f = factions[i];
                if (f == null || f.IsPlayer || f.Hidden || f.def == null || !f.def.humanlikeFaction)
                {
                    continue;
                }
                string name = f.Name;
                options.Add(new FloatMenuOption(name, () => factionFilter = name));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        // Read-only: EnsurePairs mutates sim state, so the UI only shows what the sim already tracks.
        private void DrawWorldState(Rect rect, GameComponent_LivingWorld comp)
        {
            float y = rect.y;
            if (LivingWorldMod.Global != null)
            {
                bool on = LivingWorldMod.Global.overlaysEnabled;
                Widgets.CheckboxLabeled(new Rect(rect.x, y, 260f, 24f), "LivingWorld_Overlay_Toggle".Translate(), ref on);
                if (on != LivingWorldMod.Global.overlaysEnabled)
                {
                    LivingWorldMod.Global.overlaysEnabled = on;
                    LivingWorldMod.Global.Write();
                }
                y += 26f;
            }
            if (comp != null)
            {
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "LivingWorld_WorldState_Status".Translate(
                    comp.PendingFalloutCount,
                    RoadTileCount(),
                    LivingWorldWorldObjectIndex.TrafficCount));
                GUI.color = Color.white;
                y += 26f;
            }

            IReadOnlyList<FactionPairState> pairs = comp?.Pairs;
            if (pairs == null || pairs.Count == 0)
            {
                Widgets.Label(new Rect(rect.x, y, rect.width, 40f),
                    "LivingWorld_WorldState_Empty".Translate());
                return;
            }

            int wars = 0, tensions = 0, alliances = 0, peace = 0;
            statePairs.Clear();
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
                statePairs.Add(p);
            }
            statePairs.Sort((a, b) =>
            {
                int byTone = ToneOrder(a.tone).CompareTo(ToneOrder(b.tone));
                return byTone != 0 ? byTone : b.intensity.CompareTo(a.intensity);
            });

            Widgets.Label(new Rect(rect.x, y, rect.width, 24f),
                "LivingWorld_WorldState_Summary".Translate(wars, tensions, alliances, peace));
            y += 24f;
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "LivingWorld_WorldState_PeaceHidden".Translate());
            GUI.color = Color.white;
            y += 28f;

            Rect outRect = new Rect(rect.x, y, rect.width, rect.yMax - y);
            float viewH = statePairs.Count * 26f + 8f;
            Widgets.BeginScrollView(outRect, ref stateScrollPos, new Rect(0f, 0f, outRect.width - 16f, viewH));
            float ly = 0f;
            for (int i = 0; i < statePairs.Count; i++)
            {
                FactionPairState p = statePairs[i];
                Faction fa = p.FactionA();
                Faction fb = p.FactionB();
                string a = fa?.Name ?? "LivingWorld_UnknownFaction".Translate().ToString();
                string b = fb?.Name ?? "LivingWorld_UnknownFaction".Translate().ToString();
                string line = "LivingWorld_PairJoin".Translate(a, b) + ": " + ToneLabel(p.tone);
                if (p.tone != FactionRelationTone.Alliance)
                {
                    line += ", " + "LivingWorld_WorldState_Intensity".Translate(p.intensity);
                }
                if (p.TradeBlackoutActive)
                {
                    line += ", " + "LivingWorld_WorldState_Blackout".Translate();
                }
                Rect row = new Rect(0f, ly, outRect.width - 20f, 24f);
                Widgets.DrawHighlightIfMouseover(row);
                TooltipHandler.TipRegion(row, "LivingWorld_WorldState_JumpTip".Translate());
                if (Widgets.ButtonInvisible(row))
                {
                    // Each click jumps to the other side: A's settlement nearest B, then B's nearest A.
                    jumpToB = !jumpToB;
                    Settlement target = NearestSettlementOf(jumpToB ? fb : fa, jumpToB ? fa : fb);
                    if (target != null)
                    {
                        CameraJumper.TryJumpAndSelect(target);
                    }
                }
                GUI.color = ToneColor(p.tone);
                Widgets.Label(row, line);
                GUI.color = Color.white;
                ly += 26f;
            }
            Widgets.EndScrollView();
        }

        private static int RoadTileCount()
        {
            return Current.Game?.GetComponent<GameComponent_LivingWorldRoad>()?.Tiles.Count ?? 0;
        }

        /// <summary>The settlement of <paramref name="f"/> closest to any settlement of <paramref name="other"/>.</summary>
        private static Settlement NearestSettlementOf(Faction f, Faction other)
        {
            if (f == null)
            {
                return null;
            }
            List<Settlement> all = Find.WorldObjects.Settlements;
            Settlement best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                Settlement s = all[i];
                if (s?.Faction != f)
                {
                    continue;
                }
                best ??= s;
                if (other == null)
                {
                    break;
                }
                for (int j = 0; j < all.Count; j++)
                {
                    Settlement o = all[j];
                    if (o?.Faction != other || o.Tile.Layer != s.Tile.Layer)
                    {
                        continue;
                    }
                    float d = Find.WorldGrid.ApproxDistanceInTiles(s.Tile, o.Tile);
                    if (d < bestD)
                    {
                        bestD = d;
                        best = s;
                    }
                }
            }
            return best;
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

        private static bool MatchesFaction(WorldEvent ev, string needle)
        {
            if (needle == null)
            {
                return true;
            }
            return Contains(ev.factionAName, needle)
                || Contains(ev.factionBName, needle)
                || Contains(ev.settlementLabel, needle);
        }

        private static bool Contains(string hay, string needle) =>
            hay != null && hay.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;

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

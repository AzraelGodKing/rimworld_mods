using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace LivingWorld
{
    public class MainTabWindow_LivingWorldChronicle : MainTabWindow
    {
        private Vector2 scrollPos;
        private string factionFilter = string.Empty;
        private int channelFilter; // 0 all, 1 rumour, 2 proximity, 3 radio

        public override Vector2 RequestedTabSize => new Vector2(720f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 28f),
                "LivingWorld_Chronicle_Title".Translate());
            Text.Font = GameFont.Small;

            float y = inRect.y + 32f;
            Widgets.Label(new Rect(inRect.x, y, 80f, 24f), "LivingWorld_Chronicle_Filter".Translate());
            factionFilter = Widgets.TextField(new Rect(inRect.x + 84f, y, 220f, 24f), factionFilter ?? string.Empty);
            if (Widgets.ButtonText(new Rect(inRect.x + 314f, y, 140f, 24f), ChannelLabel()))
            {
                channelFilter = (channelFilter + 1) % 4;
            }
            y += 32f;

            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
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
                if (!MatchesFaction(ev) || !MatchesChannel(ev))
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
    }
}

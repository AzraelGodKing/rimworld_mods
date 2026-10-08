using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    public class Dialog_Almanac : Window
    {
        private readonly MapComponent_Stormproof component;
        private Vector2 scroll;

        public Dialog_Almanac(MapComponent_Stormproof component)
        {
            this.component = component;
            doCloseX = true;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(640f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Stormproof_Almanac_Title".Translate());
            Text.Font = GameFont.Small;

            IReadOnlyList<AlmanacEntry> entries = component.Almanac;
            SumLifetime(entries, out int strikesCaught, out int strikesMissed,
                out int zzztAbsorbed, out int zzztSuffered, out int firesSnuffed, out int wearHits);
            bool hasTotals = strikesCaught + strikesMissed + zzztAbsorbed + zzztSuffered
                + firesSnuffed + wearHits > 0;
            float headerH = hasTotals ? 28f : 0f;
            if (hasTotals)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 36f, inRect.width, 24f),
                    "Stormproof_Almanac_Lifetime".Translate(
                        strikesCaught, strikesMissed,
                        zzztAbsorbed, zzztSuffered,
                        firesSnuffed, wearHits));
            }

            Rect listRect = new Rect(inRect.x, inRect.y + 40f + headerH, inRect.width,
                inRect.height - 40f - headerH);
            float viewH = 8f;
            for (int i = 0; i < entries.Count; i++)
            {
                viewH += entries[i].HasLedger ? 48f : 26f;
            }
            viewH = Mathf.Max(listRect.height, viewH);
            Rect view = new Rect(0f, 0f, listRect.width - 16f, viewH);
            Widgets.BeginScrollView(listRect, ref scroll, view);
            if (entries.Count == 0)
            {
                Widgets.Label(view, "Stormproof_Almanac_Empty".Translate());
            }
            else
            {
                float y = 0f;
                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    AlmanacEntry e = entries[i];
                    string season = ((Quadrum)e.quadrum).Label();
                    string dur = e.durationTicks > 0
                        ? e.durationTicks.ToStringTicksToPeriod()
                        : "Stormproof_Almanac_Ongoing".Translate().ToString();
                    string line = "Stormproof_Almanac_Line".Translate(
                        e.year.ToString(), season, e.label, dur);
                    float h = e.HasLedger ? 46f : 26f;
                    Widgets.Label(new Rect(0f, y, view.width, 24f), line);
                    if (e.HasLedger)
                    {
                        Widgets.Label(new Rect(8f, y + 22f, view.width - 8f, 22f),
                            "Stormproof_Almanac_Ledger".Translate(
                                e.strikesCaught, e.strikesMissed,
                                e.zzztAbsorbed, e.zzztSuffered,
                                e.firesSnuffed, e.wearHits));
                    }
                    y += h;
                }
            }
            Widgets.EndScrollView();
        }

        private static void SumLifetime(IReadOnlyList<AlmanacEntry> entries,
            out int strikesCaught, out int strikesMissed,
            out int zzztAbsorbed, out int zzztSuffered,
            out int firesSnuffed, out int wearHits)
        {
            strikesCaught = strikesMissed = 0;
            zzztAbsorbed = zzztSuffered = 0;
            firesSnuffed = wearHits = 0;
            if (entries == null)
            {
                return;
            }
            for (int i = 0; i < entries.Count; i++)
            {
                AlmanacEntry e = entries[i];
                if (e == null)
                {
                    continue;
                }
                strikesCaught += e.strikesCaught;
                strikesMissed += e.strikesMissed;
                zzztAbsorbed += e.zzztAbsorbed;
                zzztSuffered += e.zzztSuffered;
                firesSnuffed += e.firesSnuffed;
                wearHits += e.wearHits;
            }
        }
    }
}

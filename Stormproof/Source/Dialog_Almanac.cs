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

        public override Vector2 InitialSize => new Vector2(520f, 520f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Stormproof_Almanac_Title".Translate());
            Text.Font = GameFont.Small;
            Rect listRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
            IReadOnlyList<AlmanacEntry> entries = component.Almanac;
            float viewH = Mathf.Max(listRect.height, entries.Count * 28f + 8f);
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
                    Rect row = new Rect(0f, y, view.width, 26f);
                    Widgets.Label(row, line);
                    y += 26f;
                }
            }
            Widgets.EndScrollView();
        }
    }
}

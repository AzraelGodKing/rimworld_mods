#nullable enable
using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DateNight
{
    /// <summary>Searchable list of ingestible ThingDefs for the gift list (AZR-524).</summary>
    public class Dialog_DateNightGiftPicker : Window
    {
        private readonly Action<ThingDef> onPick;
        private readonly List<ThingDef> all = new List<ThingDef>();
        private readonly List<ThingDef> shown = new List<ThingDef>();
        private string search = "";
        private string lastSearch = "\0";
        private Vector2 scroll;

        public Dialog_DateNightGiftPicker(Action<ThingDef> onPick)
        {
            this.onPick = onPick;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.ingestible != null && def.category == ThingCategory.Item && !def.IsCorpse)
                {
                    all.Add(def);
                }
            }
            all.Sort((a, b) => string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase));
        }

        public override Vector2 InitialSize => new Vector2(420f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "DateNight_GiftPicker_Title".Translate());
            Text.Font = GameFont.Small;

            Rect searchRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, 28f);
            search = Widgets.TextField(searchRect, search);
            if (search.NullOrEmpty())
            {
                GUI.color = Color.gray;
                Widgets.Label(searchRect.ContractedBy(4f, 2f), "DateNight_GiftPicker_Search".Translate());
                GUI.color = Color.white;
            }
            Filter();

            Rect outRect = new Rect(inRect.x, searchRect.yMax + 6f, inRect.width, inRect.yMax - searchRect.yMax - 6f);
            const float rowH = 28f;
            Rect view = new Rect(0f, 0f, outRect.width - 16f, shown.Count * rowH);
            Widgets.BeginScrollView(outRect, ref scroll, view);
            for (int i = 0; i < shown.Count; i++)
            {
                ThingDef def = shown[i];
                Rect row = new Rect(0f, i * rowH, view.width, rowH);
                if (i % 2 == 1)
                {
                    Widgets.DrawLightHighlight(row);
                }
                Widgets.DefIcon(new Rect(row.x + 2f, row.y + 2f, 24f, 24f), def);
                Widgets.Label(new Rect(row.x + 32f, row.y + 3f, row.width - 32f, row.height),
                    def.LabelCap + "  (" + def.defName + ")");
                if (Widgets.ButtonInvisible(row))
                {
                    onPick(def);
                    Close();
                }
            }
            Widgets.EndScrollView();
        }

        private void Filter()
        {
            if (search == lastSearch)
            {
                return;
            }
            lastSearch = search;
            shown.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                ThingDef def = all[i];
                if (search.NullOrEmpty()
                    || def.label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || def.defName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    shown.Add(def);
                }
            }
        }
    }
}

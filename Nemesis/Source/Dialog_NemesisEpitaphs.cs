using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>AZR-390 — full scrollable epitaph history; the dossier only previews the latest three.</summary>
    public class Dialog_NemesisEpitaphs : Window
    {
        private const float RowHeight = 24f;

        private readonly List<NemesisEpitaph> _epitaphs;
        private Vector2 _scroll;

        public override Vector2 InitialSize => new Vector2(620f, 520f);

        public Dialog_NemesisEpitaphs(List<NemesisEpitaph> epitaphs)
        {
            _epitaphs = epitaphs;
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Nemesis_Dossier_EpitaphArchiveTitle".Translate());
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(inRect.x, inRect.y + 38f, inRect.width, inRect.height - 38f - CloseButSize.y - 8f);
            int count = _epitaphs?.Count ?? 0;
            if (count == 0)
            {
                Widgets.Label(outRect, "Nemesis_Dossier_NoEpitaphs".Translate());
                return;
            }

            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, count * RowHeight);
            Widgets.BeginScrollView(outRect, ref _scroll, viewRect);
            float y = 0f;
            for (int i = count - 1; i >= 0; i--)
            {
                Rect row = new Rect(0f, y, viewRect.width, RowHeight);
                if ((count - 1 - i) % 2 == 1)
                    Widgets.DrawLightHighlight(row);
                Widgets.Label(row, _epitaphs[i].SummaryLine());
                y += RowHeight;
            }
            Widgets.EndScrollView();
        }
    }
}

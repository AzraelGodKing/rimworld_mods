using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class Dialog_RenameLevel : Window
    {
        private readonly Map map;
        private string curName;

        public override Vector2 InitialSize => new Vector2(400f, 175f);

        public Dialog_RenameLevel(Map map, string initialName)
        {
            this.map = map;
            curName = initialName ?? string.Empty;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "Strata_RenameLevelPrompt".Translate());
            curName = Widgets.TextField(new Rect(0f, 36f, inRect.width, 30f), curName);
            if (Widgets.ButtonText(new Rect(0f, inRect.height - 35f, 120f, 35f), "OK".Translate()))
            {
                StrataLevelLabels.Get?.SetLabel(map, curName);
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 120f, inRect.height - 35f, 120f, 35f), "Cancel".Translate()))
            {
                Close();
            }
        }
    }
}

using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class Dialog_RenameLevel : Window
    {
        private readonly Map map;
        private string curName;
        private LevelRole curRole;

        public override Vector2 InitialSize => new Vector2(400f, 230f);

        public Dialog_RenameLevel(Map map, string initialName)
        {
            this.map = map;
            curName = initialName ?? string.Empty;
            curRole = LevelRoleUtility.GetRole(map);
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "Strata_RenameLevelPrompt".Translate());
            curName = Widgets.TextField(new Rect(0f, 36f, inRect.width, 30f), curName);

            Widgets.Label(new Rect(0f, 76f, inRect.width, 24f), "Strata_PurposePrompt".Translate());
            if (Widgets.ButtonText(new Rect(0f, 100f, inRect.width, 30f), LevelRoleUtility.Label(curRole)))
            {
                curRole = NextRole(curRole);
            }

            if (Widgets.ButtonText(new Rect(0f, inRect.height - 35f, 120f, 35f), "OK".Translate()))
            {
                StrataLevelLabels.Get?.SetLabel(map, curName);
                LevelRoleUtility.SetRole(map, curRole);
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 120f, inRect.height - 35f, 120f, 35f), "Cancel".Translate()))
            {
                Close();
            }
        }

        private static LevelRole NextRole(LevelRole role)
        {
            bool found = false;
            foreach (LevelRole candidate in LevelRoleUtility.AllRolesInOrder())
            {
                if (found)
                {
                    return candidate;
                }
                if (candidate == role)
                {
                    found = true;
                }
            }
            return LevelRole.None;
        }
    }
}

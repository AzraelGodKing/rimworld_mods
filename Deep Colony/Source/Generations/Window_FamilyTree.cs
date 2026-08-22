using UnityEngine;
using Verse;

namespace DeepColony
{
    public class Window_FamilyTree : Window
    {
        private Pawn root;
        private Vector2 scrollPos;

        public Window_FamilyTree(Pawn pawn)
        {
            root = pawn;
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = true;
            preventCameraMotion = false;
        }

        public override Vector2 InitialSize =>
            DeepColonySettings.Get.familyTreePedigreeStyle
                ? new Vector2(840f, 620f)
                : new Vector2(720f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            if (root == null)
            {
                Close();
                return;
            }

            Rect header = new Rect(inRect.x, inRect.y, inRect.width, FamilyTreeDrawer.TitleRowH);
            FamilyTreeDrawer.DrawHeader(header, root, ref scrollPos);

            FamilyTreeSnapshot snap = FamilyTreeUtility.Build(root);
            Rect outRect = inRect;
            outRect.yMin += FamilyTreeDrawer.TitleRowH + 4f;
            outRect.yMax -= 40f;
            Vector2 need = FamilyTreeDrawer.MeasureSize(snap, includeTitle: false);
            float viewH = Mathf.Max(outRect.height, need.y + 8f);
            float viewW = Mathf.Max(outRect.width - 16f, need.x);
            Rect view = new Rect(0f, 0f, viewW, viewH);
            Widgets.BeginScrollView(outRect, ref scrollPos, view);
            FamilyTreeDrawer.Draw(view, snap, OnClick, drawTitle: false);
            Widgets.EndScrollView();
        }

        private void OnClick(Pawn pawn)
        {
            if (pawn == null) return;
            root = pawn;
            FamilyTreeUtility.JumpTo(pawn);
        }
    }
}

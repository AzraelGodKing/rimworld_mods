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

        public override Vector2 InitialSize => new Vector2(720f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            if (root == null)
            {
                Close();
                return;
            }

            FamilyTreeSnapshot snap = FamilyTreeUtility.Build(root);
            Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 40f);
            float viewH = Mathf.Max(outRect.height, FamilyTreeDrawer.MeasureHeight(snap) + 8f);
            Rect view = new Rect(0f, 0f, outRect.width - 16f, viewH);
            Widgets.BeginScrollView(outRect, ref scrollPos, view);
            FamilyTreeDrawer.Draw(view, snap, OnClick);
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

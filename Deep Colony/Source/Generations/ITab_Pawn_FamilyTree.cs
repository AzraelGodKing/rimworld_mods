using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    public class ITab_Pawn_FamilyTree : ITab
    {
        private Vector2 scrollPos;

        public ITab_Pawn_FamilyTree()
        {
            size = new Vector2(540f, 520f);
            labelKey = "DC_TabFamily";
        }

        private Pawn SelPawnForTree => SelPawn ?? (SelThing as Corpse)?.InnerPawn;

        public override bool IsVisible
        {
            get
            {
                return FamilyTreeUtility.IsVisibleFor(SelPawnForTree);
            }
        }

        protected override void FillTab()
        {
            Pawn pawn = SelPawnForTree;
            if (pawn == null) return;

            FamilyTreeSnapshot snap = FamilyTreeUtility.Build(pawn);
            Rect outRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            float viewH = Mathf.Max(outRect.height, FamilyTreeDrawer.MeasureHeight(snap) + 8f);
            Rect view = new Rect(0f, 0f, outRect.width - 16f, viewH);
            Widgets.BeginScrollView(outRect, ref scrollPos, view);
            FamilyTreeDrawer.Draw(view, snap, FamilyTreeUtility.JumpTo);
            Widgets.EndScrollView();
        }
    }
}

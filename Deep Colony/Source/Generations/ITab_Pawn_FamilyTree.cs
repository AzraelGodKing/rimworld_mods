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
            size = new Vector2(500f, 460f);
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

            bool pedigree = DeepColonySettings.Get.familyTreePedigreeStyle;
            size = pedigree ? new Vector2(560f, 500f) : new Vector2(500f, 460f);

            Rect inner = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Rect header = new Rect(inner.x, inner.y, inner.width, FamilyTreeDrawer.TitleRowH);
            FamilyTreeDrawer.DrawHeader(header, pawn, ref scrollPos);

            pedigree = DeepColonySettings.Get.familyTreePedigreeStyle;
            size = pedigree ? new Vector2(560f, 500f) : new Vector2(500f, 460f);
            inner = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

            FamilyTreeSnapshot snap = FamilyTreeUtility.Build(pawn);
            Rect outRect = new Rect(inner.x, inner.y + FamilyTreeDrawer.TitleRowH + 2f,
                inner.width, inner.height - FamilyTreeDrawer.TitleRowH - 6f);
            Vector2 need = FamilyTreeDrawer.MeasureSize(snap, includeTitle: false);
            float viewH = Mathf.Max(outRect.height, need.y + 8f);
            float viewW = Mathf.Max(outRect.width - 16f, need.x);
            Rect view = new Rect(0f, 0f, viewW, viewH);
            Widgets.BeginScrollView(outRect, ref scrollPos, view);
            FamilyTreeDrawer.Draw(view, snap, FamilyTreeUtility.JumpTo, drawTitle: false);
            Widgets.EndScrollView();
        }
    }
}

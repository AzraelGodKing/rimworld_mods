using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class Dialog_StampLevel : Window
    {
        private readonly Map source;
        private readonly Map dest;
        private Rot4 rot = Rot4.North;
        private int offsetX;
        private int offsetZ;
        private string offsetXBuf = "0";
        private string offsetZBuf = "0";
        private bool includeFloors = true;
        private bool includeStockpiles;

        public override Vector2 InitialSize => new Vector2(460f, 320f);

        public Dialog_StampLevel(Map source, Map dest)
        {
            this.source = source;
            this.dest = dest;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            float y = 0f;
            Widgets.Label(new Rect(0f, y, inRect.width, 48f),
                "Strata_StampPrompt".Translate(
                    LevelStampUtility.LabelFor(source),
                    LevelStampUtility.LabelFor(dest)));
            y += 52f;

            if (Widgets.ButtonText(new Rect(0f, y, 140f, 28f), "Strata_StampRotate".Translate(rot.ToStringHuman())))
            {
                rot = new Rot4((rot.AsInt + 1) & 3);
            }
            y += 36f;

            Widgets.Label(new Rect(0f, y, 80f, 28f), "Strata_StampOffsetX".Translate());
            Widgets.TextFieldNumeric(new Rect(90f, y, 80f, 28f), ref offsetX, ref offsetXBuf, -200, 200);
            Widgets.Label(new Rect(190f, y, 80f, 28f), "Strata_StampOffsetZ".Translate());
            Widgets.TextFieldNumeric(new Rect(280f, y, 80f, 28f), ref offsetZ, ref offsetZBuf, -200, 200);
            y += 36f;

            Widgets.CheckboxLabeled(new Rect(0f, y, inRect.width, 28f),
                "Strata_StampFloors".Translate(), ref includeFloors);
            y += 28f;
            Widgets.CheckboxLabeled(new Rect(0f, y, inRect.width, 28f),
                "Strata_StampStockpiles".Translate(), ref includeStockpiles);

            if (Widgets.ButtonText(new Rect(0f, inRect.height - 35f, 140f, 35f), "Strata_StampConfirm".Translate()))
            {
                LevelStampUtility.StampResult result = LevelStampUtility.Stamp(
                    source, dest, rot, new IntVec3(offsetX, 0, offsetZ), includeFloors, includeStockpiles);
                Messages.Message(
                    "Strata_StampDone".Translate(result.placed, result.zones, result.skipped),
                    MessageTypeDefOf.TaskCompletion);
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 140f, inRect.height - 35f, 140f, 35f), "Cancel".Translate()))
            {
                Close();
            }
        }
    }
}

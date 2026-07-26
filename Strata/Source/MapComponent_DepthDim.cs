using UnityEngine;
using Verse;

namespace Strata
{
    // Soft full-screen darken when viewing an A+/upper deck — mood cue on top of
    // (or instead of) live see-below rendering through open-sky holes.
    public class MapComponent_DepthDim : MapComponent
    {
        public MapComponent_DepthDim(Map map) : base(map)
        {
        }

        public override void MapComponentOnGUI()
        {
            StrataSettings settings = StrataMod.Settings;
            if (settings == null || !settings.depthDimEnabled || settings.depthDimStrength <= 0.01f)
            {
                return;
            }
            if (Find.CurrentMap != map || !StrataMapUtility.IsUpperLevel(map))
            {
                return;
            }

            float alpha = Mathf.Clamp01(settings.depthDimStrength) * 0.65f;
            Widgets.DrawBoxSolid(
                new Rect(0f, 0f, UI.screenWidth, UI.screenHeight),
                new Color(0.02f, 0.03f, 0.06f, alpha));
        }
    }
}

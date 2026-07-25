using UnityEngine;
using Verse;

namespace Strata
{
    // Soft darken when viewing an A+/upper deck — suggests looking down without
    // rendering the surface (that is A1). Strength is a settings slider.
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

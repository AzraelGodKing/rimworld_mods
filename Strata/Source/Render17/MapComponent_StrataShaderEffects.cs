#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace Strata
{
    // 1.7 depth effects on underground levels: screen-edge darkening that
    // grows with depth, and moving caustics on water. Each has a setting, and
    // nothing is built or drawn while it is off.
    public class MapComponent_StrataShaderEffects : MapComponent
    {
        private const int WaterRescanTicks = 2500;

        private StrataMapTexture caustics;
        private int lastWaterScanTick = -WaterRescanTicks;
        private bool anyWater;
        private Material vignette;
        private bool vignetteFailed;

        public MapComponent_StrataShaderEffects(Map map) : base(map)
        {
        }

        public override void MapComponentUpdate()
        {
            StrataSettings settings = StrataMod.Settings;
            if (settings == null || Find.CurrentMap != map || !StrataMapViewUtility.IsColonyMapView())
            {
                return;
            }
            if (settings.waterCaustics)
            {
                DrawCaustics();
            }
            if (settings.depthVignette)
            {
                DrawVignette();
            }
        }

        private void DrawCaustics()
        {
            caustics ??= new StrataMapTexture(StrataShaders17.Caustics, new Color(0.55f, 0.75f, 0.85f, 0.35f));
            if (!caustics.Available)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (!caustics.HasTexture || now - lastWaterScanTick >= WaterRescanTicks)
            {
                lastWaterScanTick = now;
                anyWater = WriteWaterMask();
            }
            if (anyWater)
            {
                caustics.Draw(map, AltitudeLayer.TerrainScatter.AltitudeFor(), 1f);
            }
        }

        // Water and flood cells that are not fogged.
        private bool WriteWaterMask()
        {
            Color32[] pixels = caustics.BeginWrite(map);
            TerrainGrid terrain = map.terrainGrid;
            FogGrid fog = map.fogGrid;
            CellIndices indices = map.cellIndices;
            bool found = false;
            for (int i = 0; i < pixels.Length; i++)
            {
                IntVec3 cell = indices.IndexToCell(i);
                TerrainDef t = terrain.TerrainAt(i);
                if (t == null || !(t.IsWater || FloodUtility.IsFlooded(t)) || fog.IsFogged(cell))
                {
                    continue;
                }
                pixels[i] = new Color32(255, 255, 255, 255);
                found = true;
            }
            caustics.Upload(map);
            return found;
        }

        private void DrawVignette()
        {
            if (vignette == null && !vignetteFailed)
            {
                vignette = StrataShaders17.Make(StrataShaders17.DepthVignette, Color.black);
                vignetteFailed = vignette == null;
            }
            if (vignette == null)
            {
                return;
            }
            int depth = StrataDepth.Of(map);
            if (depth <= 0)
            {
                return;
            }
            vignette.color = new Color(0.01f, 0.01f, 0.02f, Mathf.Clamp(0.2f + 0.08f * (depth - 1), 0f, 0.55f));

            // A quad over the camera's view, so the edges sit at the screen edges.
            CellRect view = Find.CameraDriver.CurrentViewRect;
            Vector3 center = new Vector3((view.minX + view.maxX + 1) * 0.5f,
                AltitudeLayer.MetaOverlays.AltitudeFor() + 0.1f,
                (view.minZ + view.maxZ + 1) * 0.5f);
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(view.Width, 1f, view.Height));
            Graphics.DrawMesh(MeshPool.plane10, matrix, vignette, 0);
        }
    }
}
#endif

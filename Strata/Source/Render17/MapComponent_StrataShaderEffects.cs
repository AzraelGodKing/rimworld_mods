#if RIMWORLD_1_7
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // 1.7 depth effects on underground levels: screen-edge darkening that
    // grows with depth, moving caustics on water, and heat shimmer over magma
    // and geothermal sources. Each has a setting, and nothing is built or
    // drawn while it is off.
    public class MapComponent_StrataShaderEffects : MapComponent
    {
        private const int WaterRescanTicks = 2500;

        private StrataMapTexture caustics;
        private int lastWaterScanTick = -WaterRescanTicks;
        private bool anyWater;
        private StrataMapTexture shimmer;
        private int lastHeatScanTick = -WaterRescanTicks;
        private bool anyHeat;
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
            if (settings.heatShimmer)
            {
                DrawShimmer();
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

        // Unfogged flood cells on any level, plus ordinary water on underground
        // levels only. Surface lakes, rivers and marsh stay untouched (AZR-749).
        private bool WriteWaterMask()
        {
            Color32[] pixels = caustics.BeginWrite(map);
            TerrainGrid terrain = map.terrainGrid;
            FogGrid fog = map.fogGrid;
            CellIndices indices = map.cellIndices;
            bool underground = StrataMapUtility.IsUnderground(map);
            bool found = false;
            for (int i = 0; i < pixels.Length; i++)
            {
                IntVec3 cell = indices.IndexToCell(i);
                TerrainDef t = terrain.TerrainAt(i);
                if (t == null || !((underground && t.IsWater) || FloodUtility.IsFlooded(t)) || fog.IsFogged(cell))
                {
                    continue;
                }
                pixels[i] = new Color32(255, 255, 255, 255);
                found = true;
            }
            caustics.Upload(map);
            return found;
        }

        private const int ShimmerRadius = 3;

        private void DrawShimmer()
        {
            if (!StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            shimmer ??= new StrataMapTexture(StrataShaders17.HeatShimmer, new Color(1f, 0.55f, 0.25f, 0.22f));
            if (!shimmer.Available)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (!shimmer.HasTexture || now - lastHeatScanTick >= WaterRescanTicks)
            {
                lastHeatScanTick = now;
                anyHeat = WriteHeatMask();
            }
            if (anyHeat)
            {
                shimmer.Draw(map, AltitudeLayer.Gas.AltitudeFor(), 1f);
            }
        }

        // Unfogged magma pool cells at full heat, and a soft disc around
        // magma vents, geothermal taps and steam geysers.
        private bool WriteHeatMask()
        {
            Color32[] pixels = shimmer.BeginWrite(map);
            TerrainGrid terrain = map.terrainGrid;
            FogGrid fog = map.fogGrid;
            CellIndices indices = map.cellIndices;
            TerrainDef magma = StrataMagmaDefOf.Strata_MagmaPool;
            bool found = false;
            if (magma != null)
            {
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (terrain.TerrainAt(i) == magma && !fog.IsFogged(indices.IndexToCell(i)))
                    {
                        pixels[i] = new Color32(255, 255, 255, 255);
                        found = true;
                    }
                }
            }
            found |= MarkSources(pixels, StrataMagmaDefOf.Strata_MagmaVent);
            found |= MarkSources(pixels, StrataMagmaDefOf.Strata_GeothermalTap);
            found |= MarkSources(pixels, ThingDefOf.SteamGeyser);
            shimmer.Upload(map);
            return found;
        }

        private bool MarkSources(Color32[] pixels, ThingDef def)
        {
            if (def == null)
            {
                return false;
            }
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            bool found = false;
            for (int t = 0; t < things.Count; t++)
            {
                Thing thing = things[t];
                if (!thing.Spawned || map.fogGrid.IsFogged(thing.Position))
                {
                    continue;
                }
                found = true;
                Vector3 center = thing.TrueCenter();
                CellRect area = thing.OccupiedRect().ExpandedBy(ShimmerRadius).ClipInsideMap(map);
                foreach (IntVec3 cell in area)
                {
                    float dx = cell.x + 0.5f - center.x;
                    float dz = cell.z + 0.5f - center.z;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    float heat = Mathf.Clamp01(1f - dist / (ShimmerRadius + 1f));
                    int i = map.cellIndices.CellToIndex(cell);
                    byte a = (byte)(heat * 255f);
                    if (a > pixels[i].a)
                    {
                        pixels[i] = new Color32(255, 255, 255, a);
                    }
                }
            }
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

using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Paints the map-below terrain onto A+ roof-deck pads (and open-sky holes when
    // live see-below is off) so marble/slate/floors match instead of white concrete.
    //
    // Roof deck stays local floor for selection/pathing — only the *look* is sampled
    // from below. Live see-below still owns OpenSky holes when enabled.
    public class SectionLayer_GroundBelow : SectionLayer
    {
        // Full brightness on walkable pads so marble/slate read correctly.
        private static readonly Color32 DeckMatchColor = new Color32(255, 255, 255, byte.MaxValue);
        // Slightly dimmed open-sky underlay when live see-below is off.
        private static readonly Color32 OpenSkyDimColor = new Color32(150, 150, 150, byte.MaxValue);

        public SectionLayer_GroundBelow(Section section) : base(section)
        {
            relevantChangeTypes = MapMeshFlagDefOf.Terrain;
        }

        public override void Regenerate()
        {
            ClearSubMeshes(MeshParts.All);

            Map upper = Map;
            if (!StrataMapUtility.IsUpperLevel(upper))
            {
                return;
            }

            Map surface = UpperDeckUtility.SourceMapFor(upper) ?? FindSurface(upper);
            if (surface == null || surface.Disposed)
            {
                return;
            }

            bool liveSeeBelow = StrataBelowRenderer.Enabled;
            CellRect rect = section.CellRect;
            float y = AltitudeLayer.Terrain.AltitudeFor() + Altitudes.AltInc;
            bool any = false;

            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(upper))
                {
                    continue;
                }

                TerrainDef upperTerrain = upper.terrainGrid.TerrainAt(cell);
                if (upperTerrain == null)
                {
                    continue;
                }

                string name = upperTerrain.defName;
                bool isDeck = name == UpperDeckUtility.RoofDeckDefName;
                bool isSky = name == UpperDeckUtility.OpenSkyDefName;
                if (!isDeck && !isSky)
                {
                    continue;
                }
                // Live see-below draws OpenSky; this layer only fills roof-deck pads then.
                if (isSky && liveSeeBelow)
                {
                    continue;
                }

                IntVec3 below = StrataBelowRenderer.SkyToLowerCell(upper, surface, cell);
                if (!below.InBounds(surface))
                {
                    continue;
                }

                TerrainDef surfaceTerrain = surface.terrainGrid.TerrainAt(below);
                if (surfaceTerrain == null || surfaceTerrain.graphic == null || surfaceTerrain.dontRender)
                {
                    continue;
                }

                Material mat = surfaceTerrain.graphic.MatSingle;
                if (mat == null)
                {
                    continue;
                }

                Color32 tint = isDeck ? DeckMatchColor : OpenSkyDimColor;
                LayerSubMesh sub = GetSubMesh(mat);
                int idx = sub.verts.Count;

                sub.verts.Add(new Vector3(cell.x, y, cell.z));
                sub.verts.Add(new Vector3(cell.x, y, cell.z + 1));
                sub.verts.Add(new Vector3(cell.x + 1, y, cell.z + 1));
                sub.verts.Add(new Vector3(cell.x + 1, y, cell.z));

                sub.colors.Add(tint);
                sub.colors.Add(tint);
                sub.colors.Add(tint);
                sub.colors.Add(tint);

                sub.tris.Add(idx);
                sub.tris.Add(idx + 1);
                sub.tris.Add(idx + 2);
                sub.tris.Add(idx);
                sub.tris.Add(idx + 2);
                sub.tris.Add(idx + 3);
                any = true;
            }

            if (any)
            {
                FinalizeMesh(MeshParts.All);
            }
        }

        private static Map FindSurface(Map upper)
        {
            Map current = upper;
            int guard = 0;
            while (current?.Parent is PocketMapParent p
                   && p.sourceMap != null
                   && guard++ < 8)
            {
                current = p.sourceMap;
                if (!StrataMapUtility.IsUpperLevel(current))
                {
                    return current;
                }
            }
            return null;
        }
    }
}

using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Renders a dimmed snapshot of the surface-map terrain through
    // Strata_OpenSky "void" cells on A+ upper-deck maps so the player gets
    // the visual sense of standing on a roof looking down at the ground below.
    //
    // HOW IT WORKS
    // The Section constructor iterates every concrete SectionLayer subclass via
    // reflection and auto-instantiates one per Section on every map.  No Harmony
    // patch is required.  For non-upper-deck maps Regenerate() returns after
    // ClearSubMeshes(), costing essentially nothing.
    //
    // DEPTH ORDERING
    // SectionLayer_Terrain draws OpenSky terrain at AltitudeLayer.Terrain.AltitudeFor().
    // Our quads sit at that altitude + Altitudes.AltInc (one step higher = closer
    // to the camera in RimWorld's top-down projection), so they WIN the depth test
    // and replace the dark-blue void with the surface terrain colours below.
    //
    // UVS
    // Vanilla SectionLayer_Terrain does NOT add UVs for main terrain quads —
    // terrain shaders generate tiling coordinates from world position instead.
    // We follow the same convention and skip explicit UVs.
    public class SectionLayer_GroundBelow : SectionLayer
    {
        // 60 % brightness to suggest distance/depth.  Alpha = 255 (fully opaque)
        // so depth-testing replaces OpenSky, not blends over it.
        private static readonly Color32 DimColor = new Color32(150, 150, 150, byte.MaxValue);

        public SectionLayer_GroundBelow(Section section) : base(section)
        {
            // Rebuild when upper-deck terrain changes (OpenSky cells appear / vanish
            // as the player expands or shrinks the roof below).
            relevantChangeTypes = MapMeshFlagDefOf.Terrain;
        }

        public override void Regenerate()
        {
            ClearSubMeshes(MeshParts.All);

            // Live see-below renderer owns this view when enabled.
            if (StrataBelowRenderer.Enabled)
                return;

            Map upper = Map;
            if (!StrataMapUtility.IsUpperLevel(upper))
                return;

            Map surface = FindSurface(upper);
            if (surface == null)
                return;

            CellRect rect = section.CellRect;
            float y = AltitudeLayer.Terrain.AltitudeFor() + Altitudes.AltInc;

            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(upper) || !cell.InBounds(surface))
                    continue;

                // Open sky + roof deck are both dontRender — paint the surface under them
                // when live see-below is off.
                TerrainDef upperTerrain = upper.terrainGrid.TerrainAt(cell);
                if (upperTerrain == null)
                    continue;
                string name = upperTerrain.defName;
                if (name != UpperDeckUtility.OpenSkyDefName && name != UpperDeckUtility.RoofDeckDefName)
                    continue;

                IntVec3 below = cell;
                if (upper.Size != surface.Size)
                    below = StrataMapUtility.ProportionalCell(cell, upper, surface);
                if (!below.InBounds(surface))
                    continue;

                TerrainDef surfaceTerrain = surface.terrainGrid.TerrainAt(below);
                if (surfaceTerrain == null || surfaceTerrain.graphic == null || surfaceTerrain.dontRender)
                    continue;

                Material mat = surfaceTerrain.graphic.MatSingle;
                if (mat == null)
                    continue;

                LayerSubMesh sub = GetSubMesh(mat);
                int idx = sub.verts.Count;

                // Simple quad — one cell, terrain altitude + 1 step.
                sub.verts.Add(new Vector3(cell.x,     y, cell.z));
                sub.verts.Add(new Vector3(cell.x,     y, cell.z + 1));
                sub.verts.Add(new Vector3(cell.x + 1, y, cell.z + 1));
                sub.verts.Add(new Vector3(cell.x + 1, y, cell.z));

                sub.colors.Add(DimColor);
                sub.colors.Add(DimColor);
                sub.colors.Add(DimColor);
                sub.colors.Add(DimColor);

                sub.tris.Add(idx);
                sub.tris.Add(idx + 1);
                sub.tris.Add(idx + 2);
                sub.tris.Add(idx);
                sub.tris.Add(idx + 2);
                sub.tris.Add(idx + 3);
            }

            FinalizeMesh(MeshParts.All);
        }

        // Walk the pocket-map parent chain upward until we find a non-upper-level
        // map (the surface, or a colony map the tower sits on).
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
                    return current;
            }
            return null;
        }
    }
}

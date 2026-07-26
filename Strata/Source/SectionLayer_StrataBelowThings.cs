using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Prints MapMesh things from the map below into open-sky cells on A+ decks.
    public class SectionLayer_StrataBelowThings : SectionLayer
    {
        private readonly List<int> vertCountsBefore = new List<int>();

        public override bool Visible => StrataBelowRenderer.Enabled;

        public SectionLayer_StrataBelowThings(Section section) : base(section)
        {
            relevantChangeTypes = StrataDefOf.Strata_BelowThings | MapMeshFlagDefOf.Terrain;
        }

        public override void Regenerate()
        {
            ClearSubMeshes(MeshParts.All);
            if (!StrataBelowRenderer.Enabled) return;

            Map sky = Map;
            if (!StrataMapUtility.IsUpperLevel(sky)) return;

            Map lower = UpperDeckUtility.SourceMapFor(sky);
            if (lower == null || lower.Disposed) return;

            try
            {
                TerrainGrid skyTerrain = sky.terrainGrid;
                FogGrid fog = lower.fogGrid;
                float scale = Mathf.Clamp(StrataMod.Settings?.belowThingScale ?? 0.85f, 0.5f, 1f);
                bool doScale = scale < 0.999f;
                bool any = false;

                foreach (IntVec3 skyCell in section.CellRect)
                {
                    if (!skyCell.InBounds(sky)) continue;
                    TerrainDef skyT = skyTerrain.TerrainAt(skyCell);
                    if (skyT == null || !UpperDeckUtility.IsSeeThroughGap(skyT)) continue;

                    IntVec3 lowerCell = StrataBelowRenderer.SkyToLowerCell(sky, lower, skyCell);
                    if (!lowerCell.InBounds(lower)) continue;

                    List<Thing> things = lower.thingGrid.ThingsListAtFast(lowerCell);
                    for (int i = 0; i < things.Count; i++)
                    {
                        Thing thing = things[i];
                        DrawerType drawer = thing.def.drawerType;
                        if (drawer != DrawerType.MapMeshOnly && drawer != DrawerType.MapMeshAndRealTime)
                        {
                            continue;
                        }

                        IntVec3 pos = thing.Position;
                        if (thing.def.size.x != 1 || thing.def.size.z != 1)
                        {
                            if (!IsBelowPrintAnchor(thing, skyCell, sky, skyTerrain)) continue;
                        }
                        else if (pos.x != lowerCell.x || pos.z != lowerCell.z)
                        {
                            continue;
                        }

                        if (!thing.def.seeThroughFog && fog.IsFogged(pos)) continue;

                        try
                        {
                            if (doScale && CanScale(thing))
                            {
                                SnapshotVertCounts();
                                thing.Print(this);
                                ScaleNewVerts(GenThing.TrueCenter(thing), scale);
                            }
                            else
                            {
                                thing.Print(this);
                            }
                            any = true;
                        }
                        catch (Exception e)
                        {
                            Log.WarningOnce("[Strata] Below print failed for " + thing.LabelCap + ": " + e.Message,
                                thing.thingIDNumber ^ 0x5B71002);
                        }
                    }
                }

                if (any)
                {
                    FinalizeMesh(MeshParts.All);
                }
            }
            catch (Exception e)
            {
                StrataBelowRenderer.DisableSession(e, "below things layer");
            }
        }

        public override void DrawLayer()
        {
            if (!Visible) return;
            List<LayerSubMesh> meshes = subMeshes;
            for (int i = 0; i < meshes.Count; i++)
            {
                LayerSubMesh sub = meshes[i];
                if (sub.finalized && !sub.disabled)
                {
                    StrataBelowRenderer.DrawBelowSubMesh(sub);
                }
            }
        }

        private static bool IsBelowPrintAnchor(Thing t, IntVec3 skyCell, Map sky, TerrainGrid skyTerrain)
        {
            CellRect occupied = t.OccupiedRect();
            foreach (IntVec3 c in occupied)
            {
                IntVec3 skyC = StrataBelowRenderer.LowerToSkyCell(t.Map, sky, c);
                if (skyC.InBounds(sky)
                    && UpperDeckUtility.IsSeeThroughGap(skyTerrain.TerrainAt(skyC)))
                {
                    return skyC.x == skyCell.x && skyC.z == skyCell.z;
                }
            }
            return false;
        }

        private static bool CanScale(Thing t)
        {
            ThingDef def = t.def;
            if (def.mineable || (def.building != null && def.building.isNaturalRock)) return false;
            GraphicData gd = def.graphicData;
            return gd == null || gd.linkType == LinkDrawerType.None;
        }

        private void SnapshotVertCounts()
        {
            vertCountsBefore.Clear();
            List<LayerSubMesh> meshes = subMeshes;
            for (int i = 0; i < meshes.Count; i++)
            {
                vertCountsBefore.Add(meshes[i].verts.Count);
            }
        }

        private void ScaleNewVerts(Vector3 center, float scale)
        {
            List<LayerSubMesh> meshes = subMeshes;
            for (int i = 0; i < meshes.Count; i++)
            {
                List<Vector3> verts = meshes[i].verts;
                int start = i < vertCountsBefore.Count ? vertCountsBefore[i] : 0;
                for (int j = start; j < verts.Count; j++)
                {
                    Vector3 v = verts[j];
                    verts[j] = new Vector3(
                        center.x + (v.x - center.x) * scale,
                        v.y,
                        center.z + (v.z - center.z) * scale);
                }
            }
        }
    }
}

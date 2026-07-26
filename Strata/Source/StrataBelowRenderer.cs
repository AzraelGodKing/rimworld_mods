using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Draws the map below through Strata_OpenSky holes on A+ decks (AASB-style).
    [StaticConstructorOnStartup]
    public static class StrataBelowRenderer
    {
        public const float BelowOffset = -2.5f;
        private const float MaxSubMeshAltitude = 10f;
        private const float MaskAltitude = -0.1f;
        private const int MaskRebuildIntervalFrames = 15;
        private const int MaskPadCells = 8;
        private const int BelowThingsOffset = 200;
        private const int BelowDefaultOffset = 150;

        public static volatile bool OffsetActive;
        public static float BelowThingScale = 0.85f;

        private static bool sessionDisabled;
        private static bool warnedDisable;

        private static readonly Matrix4x4 BelowMatrixConst =
            Matrix4x4.Translate(new Vector3(0f, BelowOffset, 0f));

        private static readonly AccessTools.FieldRef<Section, List<SectionLayer>> LayersRef =
            AccessTools.FieldRefAccess<Section, List<SectionLayer>>("layers");

        private static readonly AccessTools.FieldRef<MapDrawer, Section[,]> SectionsRef =
            AccessTools.FieldRefAccess<MapDrawer, Section[,]>("sections");

        private static AccessTools.FieldRef<DynamicDrawManager, List<Thing>> drawThingsRef;
        private static bool drawThingsRefFailed;

        private static readonly HashSet<Type> ContentLayerTypes = BuildContentLayerTypes();
        private static readonly Dictionary<Type, int> BelowLayerOffsets = BuildBelowLayerOffsets();
        private static readonly Dictionary<(Material, int), Material> belowMats =
            new Dictionary<(Material, int), Material>();

        private static int belowQueueCeiling = -1;
        private static int transformFrame = -1;

        private static Mesh maskMesh;
        private static Material maskMat;
        private static Material forbidOverlayMat;
        private static int maskLastFrame = -999;
        private static CellRect maskLastRect;
        private static int maskLastLowerId = -1;
        private static readonly List<Vector3> maskVerts = new List<Vector3>();
        private static readonly List<int> maskTris = new List<int>();
        private static readonly List<Color32> maskColors = new List<Color32>();

        public static bool Enabled
        {
            get
            {
                if (sessionDisabled) return false;
                StrataSettings s = StrataMod.Settings;
                return s == null || s.seeBelowEnabled;
            }
        }

        public static bool LiveEnabled
        {
            get
            {
                StrataSettings s = StrataMod.Settings;
                return s == null || s.seeBelowLive;
            }
        }

        public static void DisableSession(Exception e, string context)
        {
            sessionDisabled = true;
            if (!warnedDisable)
            {
                warnedDisable = true;
                Log.Error("[Strata] See-below rendering disabled for this session (" + context + "): " + e);
            }
        }

        public static void ApplyDrawShift(ref Vector3 v) => v.y += BelowOffset;

        public static void EnsureBelowTransform() => RefreshBelowTransform();

        internal static Matrix4x4 BelowMatrix
        {
            get
            {
                RefreshBelowTransform();
                return BelowMatrixConst;
            }
        }

        private static void RefreshBelowTransform()
        {
            int frame = Time.frameCount;
            if (frame == transformFrame) return;
            transformFrame = frame;
            StrataSettings s = StrataMod.Settings;
            BelowThingScale = Mathf.Clamp(s?.belowThingScale ?? 0.85f, 0.5f, 1f);
        }

        public static bool TryGetBelowContext(Map sky, out Map lower)
        {
            lower = null;
            if (!Enabled || sky == null || sky != Find.CurrentMap) return false;
            if (!StrataMapUtility.IsUpperLevel(sky)) return false;
            lower = UpperDeckUtility.SourceMapFor(sky);
            return lower != null && !lower.Disposed;
        }

        public static IntVec3 SkyToLowerCell(Map sky, Map lower, IntVec3 skyCell)
        {
            if (sky == null || lower == null) return skyCell;
            // Gravship A+/B+ decks paint 1:1 with the host even when sizes differ.
            if (sky.Size == lower.Size || UsesRawOneToOne(sky, lower)) return skyCell;
            return StrataMapUtility.ProportionalCell(skyCell, sky, lower);
        }

        public static IntVec3 LowerToSkyCell(Map lower, Map sky, IntVec3 lowerCell)
        {
            if (sky == null || lower == null) return lowerCell;
            if (sky.Size == lower.Size || UsesRawOneToOne(sky, lower)) return lowerCell;
            return StrataMapUtility.ProportionalCell(lowerCell, lower, sky);
        }

        private static bool UsesRawOneToOne(Map sky, Map lower)
        {
            if (!StrataGravshipUtility.OdysseyActive) return false;
            return StrataGravshipUtility.IsInGravshipStack(sky)
                || StrataGravshipUtility.IsInGravshipStack(lower);
        }

        public static void DrawBelowStatic(Map map)
        {
            if (!TryGetBelowContext(map, out Map lower)) return;
            try
            {
                Camera cam = Find.Camera;
                if (cam != null && cam.farClipPlane < 70f)
                {
                    cam.farClipPlane = 70f;
                }

                lower.waterInfo?.SetTextures();
                lower.mapDrawer.MapMeshDrawerUpdate_First();

                CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(1).ClipInsideMap(lower);
                DrawSections(lower, view);
                DrawBelowMask(map, lower, view);
                DrawForbiddenOverlays(map, lower, view);
            }
            catch (Exception e)
            {
                DisableSession(e, "see-below static");
            }
        }

        public static void DrawBelowDynamic(Map map)
        {
            if (!LiveEnabled || !TryGetBelowContext(map, out Map lower)) return;
            try
            {
                RefreshBelowTransform();
                OffsetActive = true;
                // Lower map is not CurrentMap, so its DrawDynamicThings never runs.
                // Call the full draw phases ourselves or pawns stay invisible.
                if (!TryDrawFilteredDynamic(map, lower))
                {
                    DrawAllDynamicInView(map, lower);
                }
                // Forbidden icons for realtime-drawn items (static pass already
                // covered map-mesh stacks; redraw is cheap and keeps icons on top).
                CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(1).ClipInsideMap(lower);
                DrawForbiddenOverlays(map, lower, view);
            }
            catch (Exception e)
            {
                DisableSession(e, "see-below dynamic");
            }
            finally
            {
                OffsetActive = false;
            }
        }

        internal static void DrawBelowSubMesh(LayerSubMesh sub)
        {
            int queueBase = Mathf.Max(BelowQueueCeiling - BelowThingsOffset, 1);
            float y = sub.mesh.bounds.center.y;
            int queue = queueBase + Mathf.Clamp((int)(y * 14f), 0, 99);
            Material mat = BelowMaterialFor(sub.material, queue);
            if (mat != null)
            {
                Graphics.DrawMesh(sub.mesh, BelowMatrix, mat, 0);
            }
        }

        private static void DrawSections(Map lower, CellRect view)
        {
            Section[,] sections = SectionsRef(lower.mapDrawer);
            if (sections == null) return;

            int maxX = sections.GetUpperBound(0);
            int maxZ = sections.GetUpperBound(1);
            int minSx = Mathf.Max(0, view.minX / 17);
            int minSz = Mathf.Max(0, view.minZ / 17);
            int maxSx = Mathf.Min(maxX, view.maxX / 17);
            int maxSz = Mathf.Min(maxZ, view.maxZ / 17);

            for (int i = minSx; i <= maxSx; i++)
            {
                for (int j = minSz; j <= maxSz; j++)
                {
                    List<SectionLayer> layers = LayersRef(sections[i, j]);
                    for (int k = 0; k < layers.Count; k++)
                    {
                        SectionLayer layer = layers[k];
                        Type type = layer.GetType();
                        if (!ContentLayerTypes.Contains(type) || !layer.Visible) continue;

                        if (!BelowLayerOffsets.TryGetValue(type, out int offset))
                        {
                            offset = BelowDefaultOffset;
                        }

                        int queueBase = Mathf.Max(BelowQueueCeiling - offset, 1);
                        bool isTerrain = type == typeof(SectionLayer_Terrain);
                        List<LayerSubMesh> subMeshes = layer.subMeshes;
                        for (int s = 0; s < subMeshes.Count; s++)
                        {
                            LayerSubMesh sub = subMeshes[s];
                            float y = sub.mesh.bounds.center.y;
                            if (!sub.finalized || sub.disabled || y > MaxSubMeshAltitude) continue;

                            int queue = queueBase;
                            if (isTerrain && sub.material != null)
                            {
                                queue += Mathf.Clamp((sub.material.renderQueue - 2000) / 25, 0, 14);
                            }

                            Material mat = BelowMaterialFor(sub.material, queue);
                            if (mat != null)
                            {
                                Graphics.DrawMesh(sub.mesh, BelowMatrix, mat, 0);
                            }
                        }
                    }
                }
            }
        }

        private static void DrawBelowMask(Map sky, Map lower, CellRect view)
        {
            if (maskMat == null)
            {
                // Darken open-sky holes only — black fog, not white (white looked like deck slabs).
                maskMat = new Material(MatBases.FogOfWar)
                {
                    mainTexture = BaseContent.BlackTex,
                    color = Color.black
                };
            }

            int frame = Time.frameCount;
            bool needRebuild = maskMesh == null
                || !maskLastRect.Contains(new IntVec3(view.minX, 0, view.minZ))
                || !maskLastRect.Contains(new IntVec3(view.maxX, 0, view.maxZ))
                || frame - maskLastFrame >= MaskRebuildIntervalFrames
                || lower.uniqueID != maskLastLowerId;

            if (needRebuild)
            {
                CellRect rect = view.ExpandedBy(MaskPadCells).ClipInsideMap(sky);
                RebuildMask(sky, lower, rect);
                maskLastFrame = frame;
                maskLastRect = rect;
                maskLastLowerId = lower.uniqueID;
            }

            if (maskMesh != null && maskMesh.vertexCount > 0)
            {
                Graphics.DrawMesh(maskMesh, Matrix4x4.identity, maskMat, 0);
            }
        }

        private static void RebuildMask(Map sky, Map lower, CellRect rect)
        {
            EnsureMaskMesh();
            float dim = Mathf.Clamp(StrataMod.Settings?.belowDim ?? 0.12f, 0f, 0.6f);
            BuildMaskBuffers(sky, lower, rect, (byte)(255f * dim));
            maskMesh.Clear();
            if (maskVerts.Count > 0)
            {
                maskMesh.SetVertices(maskVerts);
                maskMesh.SetColors(maskColors);
                maskMesh.SetTriangles(maskTris, 0);
                maskMesh.RecalculateBounds();
            }
        }

        private static void DrawForbiddenOverlays(Map sky, Map lower, CellRect view)
        {
            Material mat = ForbidOverlayMat;
            if (mat == null) return;

            float scale = Mathf.Clamp(BelowThingScale, 0.5f, 1f) * 0.55f;
            foreach (IntVec3 cell in view)
            {
                if (!cell.InBounds(lower) || lower.fogGrid.IsFogged(cell) || lower.roofGrid.Roofed(cell))
                {
                    continue;
                }

                IntVec3 skyCell = LowerToSkyCell(lower, sky, cell);
                if (!skyCell.InBounds(sky)) continue;
                if (!UpperDeckUtility.IsSeeThroughGap(sky.terrainGrid.TerrainAt(skyCell))) continue;

                List<Thing> list = lower.thingGrid.ThingsListAtFast(cell);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing t = list[i];
                    if (t == null || !t.Spawned) continue;
                    if (t.def.category != ThingCategory.Item && t.def.category != ThingCategory.Building)
                    {
                        continue;
                    }
                    if (!t.IsForbidden(Faction.OfPlayer)) continue;

                    Vector3 pos = t.DrawPos;
                    pos.y += BelowOffset + 0.2f;
                    Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(scale, 1f, scale));
                    Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
                }
            }
        }

        private static Material ForbidOverlayMat
        {
            get
            {
                if (forbidOverlayMat != null) return forbidOverlayMat;
                Texture2D tex = ContentFinder<Texture2D>.Get("UI/Overlays/ForbiddenOutOfBounds", reportFailure: false)
                    ?? ContentFinder<Texture2D>.Get("UI/Overlays/Forbidden", reportFailure: false)
                    ?? ContentFinder<Texture2D>.Get("UI/Designators/ForbidOff", reportFailure: false)
                    ?? BaseContent.BadTex;
                forbidOverlayMat = MaterialPool.MatFrom(new MaterialRequest(tex, ShaderDatabase.MetaOverlay));
                return forbidOverlayMat;
            }
        }

        private static void EnsureMaskMesh()
        {
            if (maskMesh == null)
            {
                maskMesh = new Mesh { name = "StrataBelowMask" };
            }
        }

        private static void BuildMaskBuffers(Map sky, Map lower, CellRect rect, byte dimAlpha)
        {
            maskVerts.Clear();
            maskTris.Clear();
            maskColors.Clear();

            TerrainGrid skyTerrain = sky.terrainGrid;
            FogGrid lowerFog = lower.fogGrid;

            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(sky)) continue;

                TerrainDef t = skyTerrain.TerrainAt(cell);
                if (t == null || !UpperDeckUtility.IsSeeThroughGap(t)) continue;

                IntVec3 below = SkyToLowerCell(sky, lower, cell);
                byte alpha = (!below.InBounds(lower) || lowerFog.IsFogged(below))
                    ? byte.MaxValue
                    : dimAlpha;
                if (alpha > 0)
                {
                    AddMaskQuad(cell.x, cell.z, cell.x + 1, cell.z + 1, alpha);
                }
            }
        }

        private static void AddMaskQuad(float x0, float z0, float x1, float z1, byte alpha)
        {
            int i = maskVerts.Count;
            maskVerts.Add(new Vector3(x0, MaskAltitude, z0));
            maskVerts.Add(new Vector3(x0, MaskAltitude, z1));
            maskVerts.Add(new Vector3(x1, MaskAltitude, z1));
            maskVerts.Add(new Vector3(x1, MaskAltitude, z0));
            var c = new Color32(0, 0, 0, alpha);
            maskColors.Add(c);
            maskColors.Add(c);
            maskColors.Add(c);
            maskColors.Add(c);
            maskTris.Add(i);
            maskTris.Add(i + 1);
            maskTris.Add(i + 2);
            maskTris.Add(i);
            maskTris.Add(i + 2);
            maskTris.Add(i + 3);
        }

        private static bool TryDrawFilteredDynamic(Map sky, Map lower)
        {
            if (drawThingsRefFailed) return false;
            if (drawThingsRef == null)
            {
                try
                {
                    drawThingsRef = AccessTools.FieldRefAccess<DynamicDrawManager, List<Thing>>("drawThings");
                }
                catch
                {
                    drawThingsRef = null;
                }
                if (drawThingsRef == null)
                {
                    drawThingsRefFailed = true;
                    Log.Warning("[Strata] DynamicDrawManager.drawThings missing; see-below live uses pawn fallback.");
                    return false;
                }
            }

            List<Thing> list = drawThingsRef(lower.dynamicDrawManager);
            if (list == null)
            {
                drawThingsRefFailed = true;
                return false;
            }

            CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(1).ClipInsideMap(lower);
            for (int i = 0; i < list.Count; i++)
            {
                DrawBelowThingIfVisible(sky, lower, list[i], view);
            }
            return true;
        }

        private static void DrawAllDynamicInView(Map sky, Map lower)
        {
            CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(1).ClipInsideMap(lower);
            IReadOnlyList<Pawn> pawns = lower.mapPawns?.AllPawnsSpawned;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    DrawBelowThingIfVisible(sky, lower, pawns[i], view);
                }
            }

            List<Thing> projectiles = lower.listerThings?.ThingsInGroup(ThingRequestGroup.Projectile);
            if (projectiles == null) return;
            for (int i = 0; i < projectiles.Count; i++)
            {
                DrawBelowThingIfVisible(sky, lower, projectiles[i], view);
            }
        }

        private static void DrawBelowThingIfVisible(Map sky, Map lower, Thing thing, CellRect view)
        {
            if (thing == null || !thing.Spawned) return;

            IntVec3 pos = thing.Position;
            if (!pos.InBounds(lower) || lower.fogGrid.IsFogged(pos)) return;
            // Match AASB: only outdoor cells (roofed ground stays under solid roof-deck above).
            if (lower.roofGrid.Roofed(pos)) return;
            if (!view.Contains(pos) && !thing.def.drawOffscreen) return;

            IntVec3 skyCell = LowerToSkyCell(lower, sky, pos);
            if (!skyCell.InBounds(sky)) return;
            TerrainDef skyT = sky.terrainGrid.TerrainAt(skyCell);
            if (skyT == null || !UpperDeckUtility.IsSeeThroughGap(skyT)) return;

            try
            {
                // Full phase chain — Draw alone skips ParallelPreDraw when the
                // lower map is not the current map, so pawns never appear.
                thing.DynamicDrawPhase(DrawPhase.EnsureInitialized);
                thing.DynamicDrawPhase(DrawPhase.ParallelPreDraw);
                thing.DynamicDrawPhase(DrawPhase.Draw);
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] See-below draw failed for " + thing.LabelCap + ": " + e.Message,
                    thing.thingIDNumber ^ 0x5B71001);
            }
        }

        private static Material BelowMaterialFor(Material source, int queue)
        {
            if (source == null) return null;
            var key = (source, queue);
            if (belowMats.TryGetValue(key, out Material cached)) return cached;
            if (belowMats.Count > 1024) belowMats.Clear();
            cached = new Material(source) { renderQueue = queue };
            belowMats[key] = cached;
            return cached;
        }

        private static int BelowQueueCeiling
        {
            get
            {
                if (belowQueueCeiling < 0)
                {
                    int min = int.MaxValue;
                    min = MinQueue(min, TerrainDefOf.Soil?.graphic?.MatSingle);
                    min = MinQueue(min, UpperDeckUtility.RoofDeck?.graphic?.MatSingle);
                    min = MinQueue(min, TerrainDefOf.MetalTile?.graphic?.MatSingle);
                    if (ShaderDatabase.TerrainHard != null)
                    {
                        int rq = ShaderDatabase.TerrainHard.renderQueue;
                        if (rq >= 500) min = Mathf.Min(min, rq);
                    }
                    belowQueueCeiling = (min >= 500 && min != int.MaxValue) ? min : 2000;
                }
                return belowQueueCeiling;
            }
        }

        private static int MinQueue(int current, Material m)
        {
            if (m == null) return current;
            int rq = m.renderQueue;
            if (rq <= 0 && m.shader != null) rq = m.shader.renderQueue;
            return rq >= 500 ? Mathf.Min(current, rq) : current;
        }

        private static HashSet<Type> BuildContentLayerTypes()
        {
            var set = new HashSet<Type>
            {
                typeof(SectionLayer_Terrain),
                typeof(SectionLayer_Snow),
                typeof(SectionLayer_Gas),
                typeof(SectionLayer_PollutionCloud),
                typeof(SectionLayer_EdgeShadows),
            };
            AddByName(set, "Verse.SectionLayer_SunShadows");
            AddByName(set, "Verse.SectionLayer_Sand");
            AddByName(set, "RimWorld.SectionLayer_TerrainEdges");
            AddByName(set, "Verse.SectionLayer_TerrainScatter");
            AddByName(set, "RimWorld.SectionLayer_BridgeProps");
            return set;
        }

        private static Dictionary<Type, int> BuildBelowLayerOffsets()
        {
            var map = new Dictionary<Type, int>
            {
                { typeof(SectionLayer_Terrain), 300 },
                { typeof(SectionLayer_EdgeShadows), 260 },
                { typeof(SectionLayer_Snow), 90 },
                { typeof(SectionLayer_Gas), 85 },
                { typeof(SectionLayer_PollutionCloud), 80 },
            };
            AddOffsetByName(map, "Verse.SectionLayer_Sand", 285);
            AddOffsetByName(map, "RimWorld.SectionLayer_TerrainEdges", 280);
            AddOffsetByName(map, "Verse.SectionLayer_TerrainScatter", 275);
            AddOffsetByName(map, "Verse.SectionLayer_SunShadows", 255);
            AddOffsetByName(map, "RimWorld.SectionLayer_BridgeProps", 250);
            return map;
        }

        private static void AddByName(HashSet<Type> set, string typeName)
        {
            Type t = AccessTools.TypeByName(typeName);
            if (t != null) set.Add(t);
        }

        private static void AddOffsetByName(Dictionary<Type, int> map, string typeName, int offset)
        {
            Type t = AccessTools.TypeByName(typeName);
            if (t != null) map[t] = offset;
        }
    }

    [HarmonyPatch(typeof(MapDrawer), "DrawMapMesh")]
    public static class Patch_MapDrawer_DrawMapMesh_SeeBelow
    {
        public static void Prefix(Map ___map) => StrataBelowRenderer.DrawBelowStatic(___map);
    }

    [HarmonyPatch(typeof(DynamicDrawManager), "DrawDynamicThings")]
    public static class Patch_DynamicDrawManager_DrawDynamicThings_SeeBelow
    {
        public static void Postfix(Map ___map) => StrataBelowRenderer.DrawBelowDynamic(___map);
    }

    [HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
    public static class Patch_PawnRenderer_GetDrawParms_SeeBelow
    {
        public static void Postfix(ref PawnDrawParms __result)
        {
            if (!StrataBelowRenderer.OffsetActive) return;
            float scale = StrataBelowRenderer.BelowThingScale;
            if (scale < 0.999f)
            {
                __result.matrix *= Matrix4x4.Scale(new Vector3(scale, 1f, scale));
            }
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
    public static class Patch_PawnRenderer_ParallelGetPreRenderResults_SeeBelow
    {
        public static void Prefix(ref bool disableCache)
        {
            if (StrataBelowRenderer.OffsetActive && StrataBelowRenderer.BelowThingScale < 0.999f)
            {
                disableCache = true;
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class StrataBelowDrawPosPatcher
    {
        static StrataBelowDrawPosPatcher()
        {
            var postfix = new HarmonyMethod(typeof(StrataBelowDrawPosPatcher), nameof(OffsetPostfix));
            var harmony = new Harmony("azraelgodking.strata.seebelow.drawpos");
            int patched = 0;
            var types = new List<Type> { typeof(Thing) };
            types.AddRange(GenTypes.AllSubclasses(typeof(Thing)));
            foreach (Type type in types)
            {
                System.Reflection.MethodInfo getter = null;
                try
                {
                    getter = type.GetProperty("DrawPos",
                            System.Reflection.BindingFlags.DeclaredOnly
                            | System.Reflection.BindingFlags.Instance
                            | System.Reflection.BindingFlags.Public
                            | System.Reflection.BindingFlags.NonPublic)
                        ?.GetGetMethod(nonPublic: true);
                }
                catch
                {
                    continue;
                }
                if (getter == null || getter.IsAbstract) continue;
                try
                {
                    harmony.Patch(getter, postfix: postfix);
                    patched++;
                }
                catch
                {
                    // Some exotic overrides refuse patching; skip.
                }
            }
            Log.Message("[Strata] See-below DrawPos patched on " + patched + " type(s).");
        }

        private static void OffsetPostfix(ref Vector3 __result)
        {
            if (StrataBelowRenderer.OffsetActive)
            {
                StrataBelowRenderer.ApplyDrawShift(ref __result);
            }
        }
    }
}

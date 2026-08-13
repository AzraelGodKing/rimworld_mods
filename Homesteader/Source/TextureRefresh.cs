using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Optional art pack under Textures/HomesteaderRefresh/. Default off; original
    /// sprites stay on disk and in use until the player enables the setting.
    /// </summary>
    public static class TextureRefresh
    {
        public const string RefreshRoot = "HomesteaderRefresh";

        private static readonly FieldInfo GraphicDataCachedGraphic =
            AccessTools.Field(typeof(GraphicData), "cachedGraphic");

        private static readonly FieldInfo ThingGraphicInt =
            AccessTools.Field(typeof(Thing), "graphicInt");

        private static readonly Dictionary<string, ThingGraphicSnapshot> thingSnapshots =
            new Dictionary<string, ThingGraphicSnapshot>();

        private static readonly Dictionary<string, TerrainGraphicSnapshot> terrainSnapshots =
            new Dictionary<string, TerrainGraphicSnapshot>();

        private static bool initialized;

        private struct ThingGraphicSnapshot
        {
            public string texPath;
            public Type graphicClass;
        }

        private struct TerrainGraphicSnapshot
        {
            public string texturePath;
        }

        [StaticConstructorOnStartup]
        private static class Bootstrap
        {
            static Bootstrap()
            {
                Apply(HomesteaderMod.Settings != null && HomesteaderMod.Settings.useRefreshedTextures);
            }
        }

        public static void Apply(bool useRefresh)
        {
            EnsureInitialized();
            foreach (KeyValuePair<string, ThingGraphicSnapshot> kv in thingSnapshots)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(kv.Key);
                if (def?.graphicData == null)
                {
                    continue;
                }

                ThingGraphicSnapshot original = kv.Value;
                if (useRefresh)
                {
                    string refreshPath = RefreshPathFor(def, original.texPath);
                    Type graphicClass = RefreshGraphicClass(def, original.graphicClass);
                    if (!RefreshTexturePresent(refreshPath, graphicClass))
                    {
                        Log.Warning("[Homesteader] Refresh texture missing for " + def.defName + " at " + refreshPath + "; keeping original art.");
                        RestoreThing(def, original);
                        continue;
                    }

                    def.graphicData.texPath = refreshPath;
                    def.graphicData.graphicClass = graphicClass;
                }
                else
                {
                    RestoreThing(def, original);
                }

                RecacheThingGraphic(def);
            }

            foreach (KeyValuePair<string, TerrainGraphicSnapshot> kv in terrainSnapshots)
            {
                TerrainDef def = DefDatabase<TerrainDef>.GetNamedSilentFail(kv.Key);
                if (def == null)
                {
                    continue;
                }

                TerrainGraphicSnapshot original = kv.Value;
                if (useRefresh)
                {
                    string refreshPath = RefreshPathFor(def, original.texturePath);
                    if (ContentFinder<Texture2D>.Get(refreshPath, reportFailure: false) == null)
                    {
                        Log.Warning("[Homesteader] Refresh terrain texture missing for " + def.defName + " at " + refreshPath + "; keeping original art.");
                        def.texturePath = original.texturePath;
                    }
                    else
                    {
                        def.texturePath = refreshPath;
                    }
                }
                else
                {
                    def.texturePath = original.texturePath;
                }

                RecacheTerrainGraphic(def);
            }

            RefreshSpawnedThingGraphics();
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            ModContentPack pack = HomesteaderMod.ContentPack;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def?.graphicData == null || def.graphicData.texPath.NullOrEmpty())
                {
                    continue;
                }

                if (!OwnsDef(pack, def))
                {
                    continue;
                }

                thingSnapshots[def.defName] = new ThingGraphicSnapshot
                {
                    texPath = def.graphicData.texPath,
                    graphicClass = def.graphicData.graphicClass
                };
            }

            foreach (TerrainDef def in DefDatabase<TerrainDef>.AllDefsListForReading)
            {
                if (def == null || def.texturePath.NullOrEmpty())
                {
                    continue;
                }

                if (!OwnsDef(pack, def))
                {
                    continue;
                }

                terrainSnapshots[def.defName] = new TerrainGraphicSnapshot
                {
                    texturePath = def.texturePath
                };
            }
        }

        private static bool OwnsDef(ModContentPack pack, Def def)
        {
            if (def?.modContentPack == null)
            {
                return false;
            }

            if (pack != null)
            {
                return def.modContentPack == pack;
            }

            return def.modContentPack.PackageId != null
                && def.modContentPack.PackageId.IndexOf("homesteader", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static string RefreshPathFor(Def def, string originalPath)
        {
            if (def != null && def.defName == "Homesteader_CompostedSoil")
            {
                return RefreshRoot + "/Terrain/CompostedSoil";
            }

            if (def != null && def.defName == "Homesteader_Plant_AppleTree")
            {
                return RefreshRoot + "/Plants/AppleTree";
            }

            if (def != null && def.defName == "Homesteader_Plant_CherryTree")
            {
                return RefreshRoot + "/Plants/CherryTree";
            }

            if (def != null && def.defName == "Homesteader_Plant_MapleTree")
            {
                return RefreshRoot + "/Plants/MapleTree";
            }

            if (originalPath.StartsWith("Homesteader/", StringComparison.Ordinal))
            {
                return RefreshRoot + originalPath.Substring("Homesteader".Length);
            }

            if (originalPath.StartsWith("Wellspring/", StringComparison.Ordinal))
            {
                return RefreshRoot + "/Wellspring" + originalPath.Substring("Wellspring".Length);
            }

            return originalPath;
        }

        private static Type RefreshGraphicClass(ThingDef def, Type original)
        {
            if (def != null &&
                (def.defName == "Homesteader_Plant_AppleTree"
                 || def.defName == "Homesteader_Plant_CherryTree"
                 || def.defName == "Homesteader_Plant_MapleTree"))
            {
                return typeof(Graphic_Single);
            }

            return original ?? typeof(Graphic_Single);
        }

        private static bool RefreshTexturePresent(string texPath, Type graphicClass)
        {
            if (graphicClass == typeof(Graphic_Multi))
            {
                return ContentFinder<Texture2D>.Get(texPath + "_south", reportFailure: false) != null
                    || ContentFinder<Texture2D>.Get(texPath + "_north", reportFailure: false) != null;
            }

            return ContentFinder<Texture2D>.Get(texPath, reportFailure: false) != null;
        }

        private static void RestoreThing(ThingDef def, ThingGraphicSnapshot original)
        {
            def.graphicData.texPath = original.texPath;
            def.graphicData.graphicClass = original.graphicClass ?? typeof(Graphic_Single);
        }

        private static void RecacheThingGraphic(ThingDef def)
        {
            if (GraphicDataCachedGraphic == null)
            {
                Log.Warning("[Homesteader] GraphicData.cachedGraphic field missing; texture refresh may need a restart.");
            }
            else
            {
                GraphicDataCachedGraphic.SetValue(def.graphicData, null);
            }

            try
            {
                def.graphic = def.graphicData.Graphic;
            }
            catch (Exception e)
            {
                Log.Warning("[Homesteader] Could not recache graphic for " + def.defName + ": " + e.Message);
            }
        }

        private static void RecacheTerrainGraphic(TerrainDef def)
        {
            try
            {
                def.ResolveReferences();
            }
            catch (Exception e)
            {
                Log.Warning("[Homesteader] Could not recache terrain graphic for " + def.defName + ": " + e.Message);
            }
        }

        private static void RefreshSpawnedThingGraphics()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            foreach (Map map in Find.Maps)
            {
                if (map?.listerThings?.AllThings == null)
                {
                    continue;
                }

                List<Thing> things = map.listerThings.AllThings;
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing?.def == null || !thingSnapshots.ContainsKey(thing.def.defName))
                    {
                        continue;
                    }

                    ThingGraphicInt?.SetValue(thing, null);
                    thing.DirtyMapMesh(map);
                }
            }
        }
    }
}

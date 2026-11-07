#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Optional art pack under Textures/HomesteaderRefresh/. Not in the Workshop zip;
    /// detect presence so the setting can stay off when the folder is missing.
    /// </summary>
    public static class TextureRefresh
    {
        public const string RefreshRoot = "HomesteaderRefresh";

        /// <summary>One file that always ships with the pack; used to detect install.</summary>
        public const string PackSentinel = RefreshRoot + "/Apparel/Overalls";

        /// <summary>
        /// True when HomesteaderRefresh textures are on disk (repo clone, dropped folder,
        /// or a future optional Workshop item). False for the Workshop-only pack.
        /// </summary>
        public static bool PackPresent()
        {
            return ContentFinder<Texture2D>.Get(PackSentinel, reportFailure: false) != null;
        }

        [StaticConstructorOnStartup]
        private static class Bootstrap
        {
            static Bootstrap()
            {
                if (HomesteaderMod.Settings.useRefreshedTextures)
                {
                    Apply();
                }
            }
        }

        /// <summary>
        /// Startup only (AZR-541, AZR-577): defs are swapped before any map exists, so
        /// nothing spawned needs a graphic rebuild. Toggling the setting takes effect
        /// on the next start.
        /// </summary>
        private static void Apply()
        {
            if (!PackPresent())
            {
                Log.Warning("[Homesteader] Refresh texture pack not installed; using original art.");
                return;
            }

            if (!PrivateAccess.CachedGraphicAvailable)
            {
                Log.Error("[Homesteader] The refresh texture pack cannot be applied on this RimWorld build "
                    + "(GraphicData.cachedGraphic is missing). Using original art.");
                return;
            }

            ModContentPack? pack = HomesteaderMod.ContentPack;
            int swapped = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                string? original = def?.graphicData?.texPath;
                if (def?.graphicData == null || original == null || original.Length == 0 || !OwnsDef(pack, def)
                    || KeepOriginalArt(def, original))
                {
                    continue;
                }

                string refreshPath = RefreshPathFor(def, original);
                Type graphicClass = RefreshGraphicClass(def, def.graphicData.graphicClass);
                if (!RefreshTexturePresent(refreshPath, graphicClass))
                {
                    Log.Warning("[Homesteader] Refresh texture missing for " + def.defName + " at " + refreshPath + "; keeping original art.");
                    continue;
                }

                def.graphicData.texPath = refreshPath;
                def.graphicData.graphicClass = graphicClass;
                def.rotatable = RefreshRotatable(def, def.rotatable);
                PrivateAccess.TryClearCachedGraphic(def.graphicData);
                try
                {
                    def.graphic = def.graphicData.Graphic;
                    swapped++;
                }
                catch (Exception e)
                {
                    Log.Warning("[Homesteader] Could not rebuild graphic for " + def.defName + ": " + e.Message);
                }
            }

            foreach (TerrainDef def in DefDatabase<TerrainDef>.AllDefsListForReading)
            {
                string? original = def?.texturePath;
                if (def == null || original == null || original.Length == 0 || !OwnsDef(pack, def))
                {
                    continue;
                }

                string refreshPath = RefreshPathFor(def, original);
                if (ContentFinder<Texture2D>.Get(refreshPath, reportFailure: false) == null)
                {
                    Log.Warning("[Homesteader] Refresh terrain texture missing for " + def.defName + " at " + refreshPath + "; keeping original art.");
                    continue;
                }

                def.texturePath = refreshPath;
                try
                {
                    def.ResolveReferences();
                    swapped++;
                }
                catch (Exception e)
                {
                    Log.Warning("[Homesteader] Could not rebuild terrain graphic for " + def.defName + ": " + e.Message);
                    def.texturePath = original;
                }
            }

            Log.Message("[Homesteader] Refresh texture pack applied to " + swapped + " defs.");
        }

        private static bool OwnsDef(ModContentPack? pack, Def def)
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

        /// <summary>
        /// Brought Diggo / 27 statue art stays on the original texPath even when the refresh pack is on.
        /// </summary>
        private static bool KeepOriginalArt(ThingDef def, string originalPath)
        {
            if (def != null &&
                (def.defName == "Homesteader_DiggoPlushie" || def.defName == "Homesteader_StatueTwentySeven"))
            {
                return true;
            }

            if (originalPath == null)
            {
                return false;
            }

            return originalPath.IndexOf("HippoDogPlushie", StringComparison.OrdinalIgnoreCase) >= 0
                || (originalPath.IndexOf("/Statue27", StringComparison.OrdinalIgnoreCase) >= 0
                    && originalPath.IndexOf("Statue27Grand", StringComparison.OrdinalIgnoreCase) < 0);
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

        private static Type RefreshGraphicClass(ThingDef def, Type? original)
        {
            if (def != null &&
                (def.defName == "Homesteader_Plant_AppleTree"
                 || def.defName == "Homesteader_Plant_CherryTree"
                 || def.defName == "Homesteader_Plant_MapleTree"))
            {
                return typeof(Graphic_Single);
            }

            // Original defs stay Graphic_Single so toggle-off keeps working.
            // Refresh pack ships CanningKitchen_* and Icehouse_* cardinals.
            if (def != null &&
                (def.defName == "Homesteader_CanningKitchen"
                 || def.defName == "Homesteader_Icehouse"))
            {
                return typeof(Graphic_Multi);
            }

            return original ?? typeof(Graphic_Single);
        }

        private static bool RefreshRotatable(ThingDef def, bool original)
        {
            if (def != null && def.defName == "Homesteader_Icehouse")
            {
                return true;
            }

            return original;
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

    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Default stair art: Textures/Strata/Buildings/HandrailStairs/ (Graphic_Multi, rotatable).
    // Optional Multifloor Stairs setting swaps to Textures/Strata/Buildings/MultiFloors/.
    public static class StrataMultiFloorStairsUtility
    {
        private const string MfRoot = "Strata/Buildings/MultiFloors";
        private const string MfHandrailStairs = MfRoot + "/HandrailStairsMedium";
        private const string MfStairsDown = MfHandrailStairs + "/HandrailStairsMDown";
        private const string MfStairsUp = MfHandrailStairs + "/HandrailStairsMUp";
        private const string MfElevator = MfRoot + "/ModernElevator/ModernElevatorA";
        private const string MfElevatorIcon = MfRoot + "/ModernElevator/ModernElevatorMovingAIcon";

        private static readonly HashSet<string> affectedDefNames = new HashSet<string>();
        private static readonly Dictionary<string, StairGraphicSnapshot> snapshots = new Dictionary<string, StairGraphicSnapshot>();
        private static readonly FieldInfo GraphicIntField = AccessTools.Field(typeof(Thing), "graphicInt");
        private static bool initialized;

        private struct StairGraphicSnapshot
        {
            public string texPath;
            public Vector2 drawSize;
            public Color color;
            public bool hasColor;
            public Type graphicClass;
            public bool hasDrawOffsetNorth;
            public Vector3 drawOffsetNorth;
            public bool hasDrawOffsetSouth;
            public Vector3 drawOffsetSouth;
            public bool hasDrawOffsetEast;
            public Vector3 drawOffsetEast;
            public bool hasDrawOffsetWest;
            public Vector3 drawOffsetWest;
            public string uiIconPath;
            public bool hasUiIconPath;
            public bool rotatable;
        }

        public static void ApplyFromSettings()
        {
            StrataSettings settings = StrataMod.Settings;
            Apply(settings != null && settings.multiFloorStairs);
        }

        public static void Apply(bool useMultiFloorArt)
        {
            EnsureInitialized();
            foreach (KeyValuePair<string, StairGraphicSnapshot> kv in snapshots)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(kv.Key);
                if (def?.graphicData == null)
                {
                    continue;
                }
                StairGraphicSnapshot original = kv.Value;
                GraphicData graphic = def.graphicData;
                if (useMultiFloorArt)
                {
                    ApplyMultiFloorGraphic(original, kv.Key, graphic);
                    ApplyMultiFloorUiIcon(def, original, IsElevatorDefName(kv.Key));
                }
                else
                {
                    RestoreStrataGraphic(original, graphic);
                    RestoreStrataUiIcon(def, original);
                    def.rotatable = original.rotatable;
                }
            }
            RefreshSpawnedGraphics();
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            foreach (string defName in CollectStairDefNames())
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def?.graphicData == null)
                {
                    continue;
                }
                GraphicData graphic = def.graphicData;
                snapshots[defName] = new StairGraphicSnapshot
                {
                    texPath = graphic.texPath,
                    drawSize = graphic.drawSize,
                    color = graphic.color,
                    hasColor = graphic.color != Color.white,
                    graphicClass = graphic.graphicClass,
                    hasDrawOffsetNorth = graphic.drawOffsetNorth.HasValue,
                    drawOffsetNorth = graphic.drawOffsetNorth.GetValueOrDefault(),
                    hasDrawOffsetSouth = graphic.drawOffsetSouth.HasValue,
                    drawOffsetSouth = graphic.drawOffsetSouth.GetValueOrDefault(),
                    hasDrawOffsetEast = graphic.drawOffsetEast.HasValue,
                    drawOffsetEast = graphic.drawOffsetEast.GetValueOrDefault(),
                    hasDrawOffsetWest = graphic.drawOffsetWest.HasValue,
                    drawOffsetWest = graphic.drawOffsetWest.GetValueOrDefault(),
                    uiIconPath = def.uiIconPath,
                    hasUiIconPath = !def.uiIconPath.NullOrEmpty(),
                    rotatable = def.rotatable,
                };
                affectedDefNames.Add(defName);
            }
        }

        private static IEnumerable<string> CollectStairDefNames()
        {
            yield return "Strata_StairsDown";
            yield return "Strata_StairsUp";
            yield return "Strata_StairsBuildUp";
            yield return "Strata_BuildUpLanding";
            yield return "Strata_DigDownShaft";
            yield return "Strata_ElevatorDown";
            yield return "Strata_ElevatorUp";
            yield return "Strata_ElevatorBuildUp";
            yield return "Strata_ElevatorBuildUpLanding";
            yield return "Strata_AncientColonyStairsDown";
            yield return "Strata_AncientColonyStairsUp";
            yield return "Strata_RuinStairsDown";
            yield return "Strata_GravshipStairsDown";
            yield return "Strata_GravshipStairsUp";
            yield return "Strata_GravshipStairsBuildUp";
            yield return "Strata_GravshipBuildUpLanding";
        }

        private static void ApplyMultiFloorGraphic(StairGraphicSnapshot original, string defName, GraphicData graphic)
        {
            if (IsElevatorDefName(defName))
            {
                graphic.texPath = MfElevator;
                graphic.graphicClass = typeof(Graphic_Single);
                graphic.drawSize = new Vector2(2.5f, 2.5f);
                graphic.color = new Color(0.59f, 0.59f, 0.59f);
                ClearDrawOffsets(graphic);
                return;
            }

            bool isUp = IsStairsUpGraphic(original.texPath);
            graphic.texPath = isUp ? MfStairsUp : MfStairsDown;
            graphic.graphicClass = typeof(Graphic_Multi);
            graphic.drawSize = isUp ? new Vector2(3.3f, 3.3f) : new Vector2(2.2f, 2.2f);
            if (original.hasColor)
            {
                graphic.color = original.color;
            }
            ApplyHandrailDrawOffsets(graphic, isUp);
        }

        private static void ApplyMultiFloorUiIcon(ThingDef def, StairGraphicSnapshot original, bool isElevator)
        {
            if (isElevator)
            {
                def.uiIconPath = MfElevatorIcon;
            }
        }

        private static void RestoreStrataGraphic(StairGraphicSnapshot original, GraphicData graphic)
        {
            graphic.texPath = original.texPath;
            graphic.drawSize = original.drawSize;
            graphic.graphicClass = original.graphicClass;
            if (original.hasColor)
            {
                graphic.color = original.color;
            }
            else
            {
                graphic.color = Color.white;
            }
            graphic.drawOffsetNorth = original.hasDrawOffsetNorth ? original.drawOffsetNorth : (Vector3?)null;
            graphic.drawOffsetSouth = original.hasDrawOffsetSouth ? original.drawOffsetSouth : (Vector3?)null;
            graphic.drawOffsetEast = original.hasDrawOffsetEast ? original.drawOffsetEast : (Vector3?)null;
            graphic.drawOffsetWest = original.hasDrawOffsetWest ? original.drawOffsetWest : (Vector3?)null;
        }

        private static void RestoreStrataUiIcon(ThingDef def, StairGraphicSnapshot original)
        {
            if (original.hasUiIconPath)
            {
                def.uiIconPath = original.uiIconPath;
            }
            else
            {
                def.uiIconPath = null;
            }
        }

        private static void ApplyHandrailDrawOffsets(GraphicData graphic, bool isUp)
        {
            if (isUp)
            {
                graphic.drawOffsetNorth = new Vector3(0f, 0f, 0.76f);
                graphic.drawOffsetSouth = new Vector3(0f, 0f, 0.4f);
                graphic.drawOffsetWest = new Vector3(-0.25f, 0f, 0.55f);
                graphic.drawOffsetEast = new Vector3(0.25f, 0f, 0.55f);
            }
            else
            {
                graphic.drawOffsetNorth = Vector3.zero;
                graphic.drawOffsetSouth = new Vector3(0f, 0f, 0.2f);
                graphic.drawOffsetWest = new Vector3(0.25f, 0f, 0f);
                graphic.drawOffsetEast = new Vector3(-0.3f, 0f, 0.06f);
            }
        }

        private static void ClearDrawOffsets(GraphicData graphic)
        {
            graphic.drawOffsetNorth = null;
            graphic.drawOffsetSouth = null;
            graphic.drawOffsetEast = null;
            graphic.drawOffsetWest = null;
        }

        private static bool IsElevatorDefName(string defName)
        {
            return defName != null && defName.Contains("Elevator");
        }

        private static bool IsStairsUpGraphic(string texPath)
        {
            return texPath != null && texPath.Contains("StairsUp");
        }

        private static void RefreshSpawnedGraphics()
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
                    if (thing?.def == null || !affectedDefNames.Contains(thing.def.defName))
                    {
                        continue;
                    }
                    GraphicIntField?.SetValue(thing, null);
                    thing.DirtyMapMesh(map);
                }
            }
        }
    }
}

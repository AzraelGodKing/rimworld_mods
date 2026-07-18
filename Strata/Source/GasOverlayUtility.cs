using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Shared gas-overlay rules: which channels tint the map, how cells pick a
    // color, and how the cursor readout is drawn.
    public static class GasOverlayUtility
    {
        private struct GasSlice
        {
            public StrataGasDef gas;
            public float density;
        }

        private static readonly Color LowOxygenAlarm = new Color(0.92f, 0.28f, 0.12f);
        private static readonly Color LabelTextFill = new Color(0.94f, 0.96f, 0.99f);

        private const float AmbientOverlayTolerance = 0.035f;
        private const float MaxOverlayAlpha = 0.42f;
        private const float MinOverlayAlpha = 0.04f;

        public static string OverlayLabel(StrataGasDef gas)
        {
            if (gas == null)
            {
                return "?";
            }
            return gas.overlayLabel.NullOrEmpty() ? gas.label : gas.overlayLabel;
        }

        // 0–1 visual weight: ambient N₂/Ar/CO₂ near the depth mix are skipped;
        // hypoxic O₂ and pollutants ramp up toward MaxOverlayAlpha.
        public static float OverlayContributionWeight(StrataGasDef gas, float density, Map map)
        {
            if (gas == null || density <= 0f)
            {
                return 0f;
            }

            StrataSettings settings = StrataMod.Settings;
            if (settings != null)
            {
                if (!settings.NaturalGasesActive && AtmosphericMix.IsAtmosphericComponent(gas))
                {
                    return 0f;
                }
                if (!settings.PollutantGasesActive && AtmosphericMix.IsPollutantGas(gas))
                {
                    return 0f;
                }
            }

            if (gas.harmWhenBelow)
            {
                if (density >= gas.harmThreshold)
                {
                    return 0f;
                }
                float t = 1f - Mathf.Clamp01(density / gas.harmThreshold);
                return Mathf.Sqrt(t);
            }

            if (AtmosphericMix.IsAtmosphericComponent(gas))
            {
                float target = TargetFraction(gas, map);
                if (gas == StrataGasDefOf.Strata_CarbonDioxide)
                {
                    float excess = density - target;
                    if (excess <= gas.overlayThreshold)
                    {
                        return 0f;
                    }
                    float span = Mathf.Max(0.08f, gas.harmThreshold - target);
                    return Mathf.Clamp01(excess / span);
                }

                float delta = Mathf.Abs(density - target);
                if (delta <= AmbientOverlayTolerance)
                {
                    return 0f;
                }
                return Mathf.Clamp01((delta - AmbientOverlayTolerance) / 0.2f);
            }

            if (density <= gas.overlayThreshold)
            {
                return 0f;
            }

            float reference = gas.harmHediff != null
                ? Mathf.Max(gas.harmThreshold, gas.overlayThreshold + 0.05f)
                : 0.55f;
            float pollutantSpan = reference - gas.overlayThreshold;
            if (pollutantSpan <= 0.001f)
            {
                return 1f;
            }
            return Mathf.Clamp01((density - gas.overlayThreshold) / pollutantSpan);
        }

        public static bool VisibleInOverlay(StrataGasDef gas, float density, Map map = null)
        {
            return OverlayContributionWeight(gas, density, map ?? Find.CurrentMap) > 0.001f;
        }

        private static float TargetFraction(StrataGasDef gas, Map map)
        {
            AtmosphericMix.TargetMix target = AtmosphericMix.TargetForMap(map);
            if (gas == StrataGasDefOf.Strata_Nitrogen)
            {
                return target.nitrogen;
            }
            if (gas == StrataGasDefOf.Strata_Oxygen)
            {
                return target.oxygen;
            }
            if (gas == StrataGasDefOf.Strata_Argon)
            {
                return target.argon;
            }
            if (gas == StrataGasDefOf.Strata_CarbonDioxide)
            {
                return target.carbonDioxide;
            }
            return 0f;
        }

        public static Color ResolveOverlayColor(StrataGasDef gas, float density)
        {
            Color color = gas.overlayColor;
            if (gas.harmWhenBelow && density < gas.harmThreshold)
            {
                float t = 1f - Mathf.Clamp01(density / gas.harmThreshold);
                color = Color.Lerp(color, LowOxygenAlarm, t * 0.85f);
            }
            return BoostForOverlay(color);
        }

        public static Color BoostForOverlay(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * 1.35f + 0.12f);
            v = Mathf.Clamp01(v * 1.15f + 0.08f);
            Color boosted = Color.HSVToRGB(h, s, v);
            boosted.a = color.a;
            return boosted;
        }

        public static bool CellHasOverlayGas(float[][] cellDensity, int cellIndex, Map map)
        {
            if (cellDensity == null)
            {
                return false;
            }
            List<StrataGasDef> gases = AtmosphereMapComponent.Gases;
            for (int i = 0; i < gases.Count; i++)
            {
                StrataGasDef gas = gases[i];
                float[] plane = cellDensity[gas.index];
                if (plane != null && VisibleInOverlay(gas, plane[cellIndex], map))
                {
                    return true;
                }
            }
            return false;
        }

        // Dominant visible gas tints the cell; a strong secondary gas blends in.
        public static Color GetCellOverlayColor(float[][] cellDensity, int cellIndex, Map map)
        {
            if (cellDensity == null)
            {
                return Color.clear;
            }
            StrataGasDef primary = null;
            StrataGasDef secondary = null;
            float primaryWeight = 0f;
            float secondaryWeight = 0f;
            List<StrataGasDef> gases = AtmosphereMapComponent.Gases;
            for (int i = 0; i < gases.Count; i++)
            {
                StrataGasDef gas = gases[i];
                float[] plane = cellDensity[gas.index];
                if (plane == null)
                {
                    continue;
                }
                float density = plane[cellIndex];
                float weight = OverlayContributionWeight(gas, density, map);
                if (weight <= 0.001f)
                {
                    continue;
                }
                if (weight > primaryWeight)
                {
                    secondary = primary;
                    secondaryWeight = primaryWeight;
                    primary = gas;
                    primaryWeight = weight;
                }
                else if (weight > secondaryWeight)
                {
                    secondary = gas;
                    secondaryWeight = weight;
                }
            }
            if (primary == null)
            {
                return Color.clear;
            }
            float primaryDensity = cellDensity[primary.index][cellIndex];
            Color color = ResolveOverlayColor(primary, primaryDensity);
            if (secondary != null && secondaryWeight >= primaryWeight * 0.2f)
            {
                float secondaryDensity = cellDensity[secondary.index][cellIndex];
                float mix = secondaryWeight / (primaryWeight + secondaryWeight);
                color = Color.Lerp(color, ResolveOverlayColor(secondary, secondaryDensity), mix * 0.45f);
            }
            float blendWeight = primaryWeight + secondaryWeight * 0.35f;
            float alpha = Mathf.Lerp(
                MinOverlayAlpha,
                MaxOverlayAlpha,
                Mathf.Clamp01(Mathf.Sqrt(blendWeight)));
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static bool TryFormatRoomMix(
            AtmosphereMapComponent atmosphere,
            Room room,
            out string line)
        {
            line = null;
            if (!TryCollectRoomSlices(atmosphere, room, out List<GasSlice> slices, out float pollutantLoad, out bool openAir))
            {
                return false;
            }
            if (openAir)
            {
                line = "Gas: open air";
                return true;
            }

            var sb = new StringBuilder("Gas: ");
            for (int i = 0; i < slices.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" · ");
                }
                sb.Append(FormatSliceLabel(slices[i]));
            }
            sb.Append("  (load ");
            sb.Append(FormatLoadPercent(pollutantLoad));
            sb.Append("%)");
            line = sb.ToString();
            return true;
        }

        // Gas mix panel that follows the mouse so percentages stay on screen.
        public static bool DrawCursorMixReadout(AtmosphereMapComponent atmosphere, Room room)
        {
            if (!TryCollectRoomSlices(atmosphere, room, out List<GasSlice> slices, out float pollutantLoad, out bool openAir))
            {
                return false;
            }

            GameFont savedFont = Text.Font;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Vector2 mouse = Event.current.mousePosition;
            const float cursorPad = 14f;
            const float padH = 8f;
            const float padV = 5f;

            if (openAir)
            {
                string line = "Gas: open air";
                Vector2 size = Text.CalcSize(line);
                Rect rect = ClampLabelRect(new Rect(
                    mouse.x + cursorPad,
                    mouse.y + cursorPad,
                    size.x + padH * 2f,
                    size.y + padV * 2f));
                rect = PreferCursorSide(rect, mouse, cursorPad);
                GUI.DrawTexture(rect, TexUI.GrayTextBG);
                Widgets.Label(new Rect(rect.x + padH, rect.y + padV, size.x, size.y), line);
                Text.Font = savedFont;
                Text.Anchor = TextAnchor.UpperLeft;
                return true;
            }

            if (!TryMeasureMixLabel(slices, pollutantLoad, ReadoutLineHeight, padH, padV,
                maxGasLines: 99, showLoadLine: true, out float width, out float height))
            {
                Text.Font = savedFont;
                Text.Anchor = TextAnchor.UpperLeft;
                return false;
            }

            Rect labelRect = ClampLabelRect(new Rect(
                mouse.x + cursorPad,
                mouse.y + cursorPad,
                width,
                height));
            labelRect = PreferCursorSide(labelRect, mouse, cursorPad);
            DrawMixLines(
                labelRect,
                slices,
                pollutantLoad,
                ReadoutLineHeight,
                maxGasLines: 99,
                showLoadLine: true,
                drawBackground: true,
                padH: padH,
                padV: padV);

            Text.Font = savedFont;
            Text.Anchor = TextAnchor.UpperLeft;
            return true;
        }

        // Flip the panel to the left or above the cursor when it would clip off-screen.
        private static Rect PreferCursorSide(Rect rect, Vector2 mouse, float pad)
        {
            float maxW = UI.screenWidth / Prefs.UIScale;
            float maxH = UI.screenHeight / Prefs.UIScale;
            if (rect.xMax > maxW - pad)
            {
                rect.x = mouse.x - rect.width - pad;
            }
            if (rect.yMax > maxH - pad)
            {
                rect.y = mouse.y - rect.height - pad;
            }
            return ClampLabelRect(rect);
        }

        private const float ReadoutLineHeight = 22f;
        private const float MapLabelLineHeight = 20f;
        private const float MapLabelPadH = 8f;
        private const float MapLabelPadV = 5f;

        // Color-coded percentage labels anchored on each room (mod option).
        public static void DrawRoomLabelsOnMap(Map map, AtmosphereMapComponent atmosphere)
        {
            if (map == null || atmosphere == null || Find.CameraDriver.CurrentZoom > CameraZoomRange.Middle)
            {
                return;
            }
            CellRect view = Find.CameraDriver.CurrentViewRect;
            GameFont savedFont = Text.Font;
            TextAnchor savedAnchor = Text.Anchor;
            Color savedColor = GUI.color;
            Text.Font = GameFont.Small;

            foreach (Room room in map.regionGrid.AllRooms)
            {
                if (room == null || room.Dereferenced || !room.ProperRoom || room.UsesOutdoorTemperature)
                {
                    continue;
                }
                if (!TryCollectRoomSlices(atmosphere, room, out List<GasSlice> slices, out float pollutantLoad, out bool openAir))
                {
                    continue;
                }
                if (openAir)
                {
                    continue;
                }
                if (!TryGetRoomLabelScreenPos(room, map, view, out Vector2 screenPos))
                {
                    continue;
                }
                if (!TryMeasureMixLabel(slices, pollutantLoad, MapLabelLineHeight, MapLabelPadH, MapLabelPadV,
                    maxGasLines: 3, showLoadLine: true, out float width, out float height))
                {
                    continue;
                }
                Rect labelRect = ClampLabelRect(new Rect(
                    screenPos.x - width / 2f,
                    screenPos.y - height / 2f,
                    width,
                    height));
                DrawMixLines(
                    labelRect,
                    slices,
                    pollutantLoad,
                    MapLabelLineHeight,
                    maxGasLines: 3,
                    showLoadLine: true,
                    drawBackground: true,
                    padH: MapLabelPadH,
                    padV: MapLabelPadV);
            }

            Text.Font = savedFont;
            Text.Anchor = savedAnchor;
            GUI.color = savedColor;
        }

        private static bool TryGetRoomLabelScreenPos(Room room, Map map, CellRect view, out Vector2 screenPos)
        {
            screenPos = default;
            CellRect visible = ClipRect(room.ExtentsClose, view);
            if (visible.IsEmpty)
            {
                return false;
            }
            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (IntVec3 cell in SampleCells(visible))
            {
                if (!cell.InBounds(map) || cell.Fogged(map))
                {
                    continue;
                }
                Vector2 projected = GenMapUI.LabelDrawPosFor(cell);
                if (!IsOnScreen(projected, margin: 16f))
                {
                    continue;
                }
                sum += projected;
                count++;
            }
            if (count == 0)
            {
                return false;
            }
            screenPos = sum / count;
            return IsOnScreen(screenPos, margin: 8f);
        }

        private static CellRect ClipRect(CellRect a, CellRect b)
        {
            int minX = Mathf.Max(a.minX, b.minX);
            int maxX = Mathf.Min(a.maxX, b.maxX);
            int minZ = Mathf.Max(a.minZ, b.minZ);
            int maxZ = Mathf.Min(a.maxZ, b.maxZ);
            if (minX > maxX || minZ > maxZ)
            {
                return CellRect.Empty;
            }
            return CellRect.FromLimits(minX, minZ, maxX, maxZ);
        }

        private static IEnumerable<IntVec3> SampleCells(CellRect rect, int grid = 4)
        {
            if (rect.IsEmpty)
            {
                yield break;
            }
            if (rect.Area <= 25)
            {
                foreach (IntVec3 cell in rect)
                {
                    yield return cell;
                }
                yield break;
            }
            for (int ix = 0; ix <= grid; ix++)
            {
                for (int iz = 0; iz <= grid; iz++)
                {
                    int x = rect.minX + ix * rect.Width / grid;
                    int z = rect.minZ + iz * rect.Height / grid;
                    yield return new IntVec3(x, 0, z);
                }
            }
        }

        private static bool IsOnScreen(Vector2 screenPos, float margin)
        {
            float w = UI.screenWidth / Prefs.UIScale;
            float h = UI.screenHeight / Prefs.UIScale;
            return screenPos.x >= margin
                && screenPos.x <= w - margin
                && screenPos.y >= margin
                && screenPos.y <= h - margin;
        }

        private static Rect ClampLabelRect(Rect rect)
        {
            const float pad = 10f;
            float maxW = UI.screenWidth / Prefs.UIScale;
            float maxH = UI.screenHeight / Prefs.UIScale;
            if (rect.width > maxW - pad * 2f)
            {
                rect.width = maxW - pad * 2f;
            }
            if (rect.height > maxH - pad * 2f)
            {
                rect.height = maxH - pad * 2f;
            }
            rect.x = Mathf.Clamp(rect.x, pad, maxW - pad - rect.width);
            rect.y = Mathf.Clamp(rect.y, pad, maxH - pad - rect.height);
            return rect;
        }

        private static bool TryMeasureMixLabel(
            List<GasSlice> slices,
            float pollutantLoad,
            float lineHeight,
            float padH,
            float padV,
            int maxGasLines,
            bool showLoadLine,
            out float width,
            out float height)
        {
            width = 0f;
            height = 0f;
            if (slices == null || slices.Count == 0)
            {
                return false;
            }
            int gasLines = Mathf.Min(maxGasLines, slices.Count);
            int lineCount = gasLines + (showLoadLine ? 1 : 0);
            for (int i = 0; i < gasLines; i++)
            {
                width = Mathf.Max(width, Text.CalcSize(FormatSliceLabel(slices[i])).x);
            }
            if (showLoadLine)
            {
                string load = FormatLoadLabel(pollutantLoad);
                width = Mathf.Max(width, Text.CalcSize(load).x);
            }
            width += padH * 2f;
            height = lineCount * lineHeight + padV * 2f;
            return width > 0f;
        }

        private static string FormatLoadLabel(float pollutantLoad)
        {
            return "load " + FormatLoadPercent(pollutantLoad) + "%";
        }

        private static string FormatLoadPercent(float pollutantLoad)
        {
            pollutantLoad = Mathf.Clamp01(pollutantLoad);
            return pollutantLoad >= 0.001f
                ? Mathf.RoundToInt(pollutantLoad * 100f).ToString()
                : pollutantLoad.ToStringPercent().TrimEnd('%');
        }

        private static void DrawMixLines(
            Rect area,
            List<GasSlice> slices,
            float pollutantLoad,
            float lineHeight,
            int maxGasLines = 99,
            bool showLoadLine = true,
            bool drawBackground = false,
            float padH = 0f,
            float padV = 0f,
            float wrapWidth = 0f)
        {
            if (slices == null || slices.Count == 0)
            {
                return;
            }
            int gasLines = Mathf.Min(maxGasLines, slices.Count);
            int lineCount = gasLines + (showLoadLine ? 1 : 0);
            var texts = new List<string>(lineCount);
            var colors = new List<Color>(lineCount);
            for (int i = 0; i < gasLines; i++)
            {
                texts.Add(FormatSliceLabel(slices[i]));
                colors.Add(LabelTextColor(slices[i].gas, slices[i].density));
            }
            if (showLoadLine)
            {
                texts.Add(FormatLoadLabel(pollutantLoad));
                colors.Add(LabelTextFill);
            }

            float maxWidth = 0f;
            for (int i = 0; i < texts.Count; i++)
            {
                maxWidth = Mathf.Max(maxWidth, Text.CalcSize(texts[i]).x);
            }
            float blockWidth = wrapWidth > 0f ? Mathf.Min(maxWidth, wrapWidth) : maxWidth;
            float blockHeight = lineCount * lineHeight;
            float startX = area.x + padH + Mathf.Max(0f, (area.width - padH * 2f - blockWidth) / 2f);
            float startY = area.y + padV + Mathf.Max(0f, (area.height - padV * 2f - blockHeight) / 2f);

            if (drawBackground)
            {
                GUI.DrawTexture(
                    new Rect(startX - padH, startY - padV, blockWidth + padH * 2f, blockHeight + padV * 2f),
                    TexUI.GrayTextBG);
            }

            TextAnchor savedAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Color saved = GUI.color;
            float x = startX;
            float y = startY;
            for (int i = 0; i < texts.Count; i++)
            {
                if (wrapWidth > 0f && i > 0 && x + blockWidth > area.x + wrapWidth)
                {
                    x = area.x + padH;
                    y += lineHeight;
                }
                DrawOutlinedLabel(new Rect(x, y + i * lineHeight, blockWidth + 2f, lineHeight), texts[i], colors[i]);
            }
            GUI.color = saved;
            Text.Anchor = savedAnchor;
        }

        private static void DrawOutlinedLabel(Rect rect, string text, Color fill)
        {
            Color saved = GUI.color;
            const float outline = 1f;
            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            Widgets.Label(new Rect(rect.x - outline, rect.y, rect.width, rect.height), text);
            Widgets.Label(new Rect(rect.x + outline, rect.y, rect.width, rect.height), text);
            Widgets.Label(new Rect(rect.x, rect.y - outline, rect.width, rect.height), text);
            Widgets.Label(new Rect(rect.x, rect.y + outline, rect.width, rect.height), text);
            GUI.color = fill;
            Widgets.Label(rect, text);
            GUI.color = saved;
        }

        private static string FormatSliceLabel(GasSlice slice)
        {
            return OverlayLabel(slice.gas) + " " + Mathf.RoundToInt(slice.density * 100f) + "%";
        }

        private static Color LabelTextColor(StrataGasDef gas, float density)
        {
            Color tint = ResolveOverlayColor(gas, density);
            Color.RGBToHSV(tint, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * 0.55f);
            v = Mathf.Clamp01(v * 0.35f + 0.72f);
            Color light = Color.HSVToRGB(h, s, v);
            light.a = 1f;
            return Color.Lerp(LabelTextFill, light, 0.35f);
        }

        // Room composition for labels and readouts — significant fractions only;
        // load = pollutant / non-breathable share (smoke, deep gas, excess CO₂, …).
        private static bool TryCollectRoomSlices(
            AtmosphereMapComponent atmosphere,
            Room room,
            out List<GasSlice> slices,
            out float pollutantLoad,
            out bool openAir)
        {
            slices = null;
            pollutantLoad = 0f;
            openAir = false;
            if (atmosphere == null || room == null)
            {
                return false;
            }
            if (room.UsesOutdoorTemperature)
            {
                openAir = true;
                slices = new List<GasSlice>();
                return true;
            }

            Map map = atmosphere.map;
            if (!atmosphere.TryGetRoomDensity(room, out float[] density)
                && !TrySynthesizeAmbientDensity(atmosphere, room, out density))
            {
                return false;
            }

            pollutantLoad = AtmosphericMix.PollutantFraction(density, map);
            float composableTotal = AtmosphericMix.ComposableTotal(density);
            if (composableTotal <= 0.001f && pollutantLoad <= 0.001f)
            {
                return false;
            }

            List<StrataGasDef> gases = AtmosphereMapComponent.Gases;
            slices = new List<GasSlice>();
            for (int i = 0; i < gases.Count; i++)
            {
                StrataGasDef gas = gases[i];
                float d = density[gas.index];
                if (StrataMod.Settings != null)
                {
                    if (!StrataMod.Settings.NaturalGasesActive && AtmosphericMix.IsAtmosphericComponent(gas))
                    {
                        continue;
                    }
                    if (!StrataMod.Settings.PollutantGasesActive && AtmosphericMix.IsPollutantGas(gas))
                    {
                        continue;
                    }
                }
                if (!AtmosphericMix.SignificantForReadout(gas, d, map))
                {
                    continue;
                }
                slices.Add(new GasSlice { gas = gas, density = d });
            }
            if (slices.Count == 0 && TryAppendAmbientBaselineSlices(map, density, slices))
            {
                // Healthy ambient mix: SignificantForReadout hides ~21% O₂ until
                // something deviates — still show the baseline for sealed rooms.
            }
            else if (slices.Count == 0)
            {
                return false;
            }
            slices.Sort((a, b) => b.density.CompareTo(a.density));
            return true;
        }

        private static bool TrySynthesizeAmbientDensity(
            AtmosphereMapComponent atmosphere,
            Room room,
            out float[] density)
        {
            density = null;
            Map map = atmosphere?.map;
            if (map == null || room == null || room.UsesOutdoorTemperature)
            {
                return false;
            }
            if (!AtmosphericMix.ForcesAmbientInEnclosedRooms(map)
                && (!StrataMapUtility.IsUnderground(map)
                    || AtmosphericMix.NaturalReplenishRate(map) <= 0f))
            {
                return false;
            }
            AtmosphericMix.TargetMix target = AtmosphericMix.TargetForMap(map);
            density = new float[DefDatabase<StrataGasDef>.DefCount];
            if (StrataGasDefOf.Strata_Oxygen != null)
            {
                density[StrataGasDefOf.Strata_Oxygen.index] = target.oxygen;
            }
            if (StrataGasDefOf.Strata_Nitrogen != null)
            {
                density[StrataGasDefOf.Strata_Nitrogen.index] = target.nitrogen;
            }
            if (StrataGasDefOf.Strata_Argon != null)
            {
                density[StrataGasDefOf.Strata_Argon.index] = target.argon;
            }
            if (StrataGasDefOf.Strata_CarbonDioxide != null)
            {
                density[StrataGasDefOf.Strata_CarbonDioxide.index] = target.carbonDioxide;
            }
            return true;
        }

        private static bool TryAppendAmbientBaselineSlices(
            Map map,
            float[] density,
            List<GasSlice> slices)
        {
            if (map == null || density == null || slices == null
                || AtmosphericMix.PollutantFraction(density, map) > 0.02f)
            {
                return false;
            }
            AtmosphericMix.TargetMix target = AtmosphericMix.TargetForMap(map);
            StrataGasDef o2 = StrataGasDefOf.Strata_Oxygen;
            StrataGasDef n2 = StrataGasDefOf.Strata_Nitrogen;
            if (o2 == null || n2 == null)
            {
                return false;
            }
            float o2Density = density[o2.index] > 0.001f ? density[o2.index] : target.oxygen;
            float n2Density = density[n2.index] > 0.001f ? density[n2.index] : target.nitrogen;
            if (o2Density <= 0.001f && n2Density <= 0.001f)
            {
                return false;
            }
            if (o2Density > 0.001f)
            {
                slices.Add(new GasSlice { gas = o2, density = o2Density });
            }
            if (n2Density > 0.001f)
            {
                slices.Add(new GasSlice { gas = n2, density = n2Density });
            }
            return slices.Count > 0;
        }
    }
}

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

        public static string OverlayLabel(StrataGasDef gas)
        {
            if (gas == null)
            {
                return "?";
            }
            return gas.overlayLabel.NullOrEmpty() ? gas.label : gas.overlayLabel;
        }

        // Normal breathable O₂ is hidden; depleted O₂ and toxic gases tint.
        public static bool VisibleInOverlay(StrataGasDef gas, float density)
        {
            if (gas == null || density <= gas.overlayThreshold)
            {
                return false;
            }
            if (gas.harmWhenBelow)
            {
                return density < gas.harmThreshold;
            }
            return true;
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

        public static bool CellHasOverlayGas(float[][] cellDensity, int cellIndex)
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
                if (plane != null && VisibleInOverlay(gas, plane[cellIndex]))
                {
                    return true;
                }
            }
            return false;
        }

        // Dominant visible gas tints the cell; a strong secondary gas blends in.
        public static Color GetCellOverlayColor(float[][] cellDensity, int cellIndex)
        {
            if (cellDensity == null)
            {
                return Color.clear;
            }
            StrataGasDef primary = null;
            StrataGasDef secondary = null;
            float primaryDensity = 0f;
            float secondaryDensity = 0f;
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
                if (!VisibleInOverlay(gas, density))
                {
                    continue;
                }
                if (density > primaryDensity)
                {
                    secondary = primary;
                    secondaryDensity = primaryDensity;
                    primary = gas;
                    primaryDensity = density;
                }
                else if (density > secondaryDensity)
                {
                    secondary = gas;
                    secondaryDensity = density;
                }
            }
            if (primary == null)
            {
                return Color.clear;
            }
            Color color = ResolveOverlayColor(primary, primaryDensity);
            if (secondary != null && secondaryDensity >= primaryDensity * 0.2f)
            {
                float mix = secondaryDensity / (primaryDensity + secondaryDensity);
                color = Color.Lerp(color, ResolveOverlayColor(secondary, secondaryDensity), mix * 0.45f);
            }
            float alpha = Mathf.Clamp01(Mathf.Sqrt(primaryDensity + secondaryDensity * 0.35f) * 1.35f);
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static bool TryFormatRoomMix(
            AtmosphereMapComponent atmosphere,
            Room room,
            out string line)
        {
            line = null;
            if (!TryCollectRoomSlices(atmosphere, room, out List<GasSlice> slices, out float total, out bool openAir))
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
                sb.Append(FormatSliceLabel(slices[i], total));
            }
            sb.Append("  (load ");
            sb.Append(total >= 0.001f ? Mathf.RoundToInt(total * 100f).ToString() : total.ToStringPercent());
            sb.Append(')');
            line = sb.ToString();
            return true;
        }

        // Gas mix panel that follows the mouse so percentages stay on screen.
        public static bool DrawCursorMixReadout(AtmosphereMapComponent atmosphere, Room room)
        {
            if (!TryCollectRoomSlices(atmosphere, room, out List<GasSlice> slices, out float total, out bool openAir))
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

            if (!TryMeasureMixLabel(slices, total, ReadoutLineHeight, padH, padV,
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
                total,
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
                if (!TryCollectRoomSlices(atmosphere, room, out List<GasSlice> slices, out float total, out bool openAir))
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
                if (!TryMeasureMixLabel(slices, total, MapLabelLineHeight, MapLabelPadH, MapLabelPadV,
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
                    total,
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
            float total,
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
                width = Mathf.Max(width, Text.CalcSize(FormatSliceLabel(slices[i], total)).x);
            }
            if (showLoadLine)
            {
                string load = FormatLoadLabel(total);
                width = Mathf.Max(width, Text.CalcSize(load).x);
            }
            width += padH * 2f;
            height = lineCount * lineHeight + padV * 2f;
            return width > 0f;
        }

        private static string FormatLoadLabel(float total)
        {
            return "load "
                + (total >= 0.001f ? Mathf.RoundToInt(total * 100f).ToString() : total.ToStringPercent())
                + "%";
        }

        private static void DrawMixLines(
            Rect area,
            List<GasSlice> slices,
            float total,
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
                texts.Add(FormatSliceLabel(slices[i], total));
                colors.Add(ReadoutColor(slices[i].gas, slices[i].density));
            }
            if (showLoadLine)
            {
                texts.Add(FormatLoadLabel(total));
                colors.Add(new Color(0.75f, 0.75f, 0.75f));
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
                GUI.color = colors[i];
                Widgets.Label(new Rect(x, y + i * lineHeight, blockWidth + 2f, lineHeight), texts[i]);
            }
            GUI.color = saved;
            Text.Anchor = savedAnchor;
        }

        private static string FormatSliceLabel(GasSlice slice, float total)
        {
            // Absolute density (0.21 → 21%) matches ambient-O₂ expectations on B1.
            return OverlayLabel(slice.gas) + " " + Mathf.RoundToInt(slice.density * 100f) + "%";
        }

        private static Color ReadoutColor(StrataGasDef gas, float density)
        {
            if (gas.harmWhenBelow && density >= gas.harmThreshold)
            {
                return new Color(0.72f, 0.82f, 0.95f);
            }
            return ResolveOverlayColor(gas, density);
        }

        // Full room composition for labels and readouts (includes normal O₂ and trace gases).
        private static bool TryCollectRoomSlices(
            AtmosphereMapComponent atmosphere,
            Room room,
            out List<GasSlice> slices,
            out float total,
            out bool openAir)
        {
            slices = null;
            total = 0f;
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

            List<StrataGasDef> gases = AtmosphereMapComponent.Gases;
            slices = new List<GasSlice>();
            for (int i = 0; i < gases.Count; i++)
            {
                StrataGasDef gas = gases[i];
                float density = atmosphere.DensityInRoom(room, gas);
                if (density <= 0.001f)
                {
                    continue;
                }
                slices.Add(new GasSlice { gas = gas, density = density });
                total += density;
            }
            if (slices.Count == 0 || total <= 0f)
            {
                return false;
            }
            slices.Sort((a, b) => b.density.CompareTo(a.density));
            return true;
        }
    }
}

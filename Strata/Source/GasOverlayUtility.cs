using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Formats the play-settings gas overlay readout: every Strata gas channel
    // present in a room, as mix ratios that sum to 100%.
    public static class GasOverlayUtility
    {
        private struct GasSlice
        {
            public StrataGasDef gas;
            public float density;
        }

        public static string OverlayLabel(StrataGasDef gas)
        {
            if (gas == null)
            {
                return "?";
            }
            return gas.overlayLabel.NullOrEmpty() ? gas.label : gas.overlayLabel;
        }

        public static bool TryFormatRoomMix(
            AtmosphereMapComponent atmosphere,
            Room room,
            out string line)
        {
            line = null;
            if (atmosphere == null || room == null)
            {
                return false;
            }
            if (room.UsesOutdoorTemperature)
            {
                line = "Gas: open air";
                return true;
            }

            List<StrataGasDef> gases = AtmosphereMapComponent.Gases;
            var slices = new List<GasSlice>();
            float total = 0f;
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

            var sb = new StringBuilder("Gas: ");
            for (int i = 0; i < slices.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" · ");
                }
                float ratio = slices[i].density / total;
                sb.Append(OverlayLabel(slices[i].gas));
                sb.Append(' ');
                sb.Append(Mathf.RoundToInt(ratio * 100f));
                sb.Append('%');
            }
            sb.Append("  (load ");
            sb.Append(total >= 0.01f ? Mathf.RoundToInt(total * 100f).ToString() : total.ToStringPercent());
            sb.Append(')');
            line = sb.ToString();
            return true;
        }
    }
}

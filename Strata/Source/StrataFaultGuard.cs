using System;
using RimWorld;
using Verse;

namespace Strata
{
    // A5: trip high-risk systems after repeated errors so a broken soft-compat
    // path cannot spam Log.Error forever. Manual settings stay authoritative —
    // trips flip the same flags the player can re-enable in options.
    public static class StrataFaultGuard
    {
        public enum System
        {
            WorkRelay,
            Atmosphere,
            ShaftFluid,
            RobotSoftCompat,
        }

        private const int TripThreshold = 8;
        private const int WindowTicks = 15000;

        private static readonly int[] counts = new int[4];
        private static readonly int[] windowStart = new int[4];
        private static readonly bool[] tripped = new bool[4];

        public static void ResetSession()
        {
            Array.Clear(counts, 0, counts.Length);
            Array.Clear(windowStart, 0, windowStart.Length);
            Array.Clear(tripped, 0, tripped.Length);
        }

        public static bool ShaftFluidActive
        {
            get
            {
                StrataSettings s = StrataMod.Settings;
                if (s == null)
                {
                    return true;
                }
                return s.shaftFluidEnabled && !tripped[(int)System.ShaftFluid];
            }
        }

        public static void Report(System system, string detail = null)
        {
            int i = (int)system;
            int now = Find.TickManager?.TicksGame ?? 0;
            if (windowStart[i] == 0 || now - windowStart[i] > WindowTicks)
            {
                windowStart[i] = now;
                counts[i] = 0;
            }
            counts[i]++;
            if (tripped[i] || counts[i] < TripThreshold)
            {
                return;
            }
            tripped[i] = true;
            ApplyTrip(system, detail);
        }

        private static void ApplyTrip(System system, string detail)
        {
            StrataSettings s = StrataMod.Settings;
            string label;
            switch (system)
            {
                case System.WorkRelay:
                    if (s != null)
                    {
                        s.workRelayEnabled = false;
                    }
                    label = "work / haul relay";
                    break;
                case System.Atmosphere:
                    if (s != null)
                    {
                        s.naturalGasesEnabled = false;
                        s.pollutantGasesEnabled = false;
                    }
                    label = "atmosphere / gases";
                    break;
                case System.ShaftFluid:
                    if (s != null)
                    {
                        s.shaftFluidEnabled = false;
                    }
                    label = "shaft fluid junctions";
                    break;
                case System.RobotSoftCompat:
                    if (s != null)
                    {
                        s.robotSoftCompatEnabled = false;
                    }
                    label = "robot soft-compat";
                    break;
                default:
                    label = system.ToString();
                    break;
            }

            string msg = "[Strata] Auto-disabled " + label
                + " after repeated errors"
                + (detail.NullOrEmpty() ? "." : ": " + detail)
                + " Re-enable in mod options if needed.";
            Log.Warning(msg);
            if (Current.ProgramState == ProgramState.Playing)
            {
                Messages.Message(
                    "Strata_FaultGuardTripped".Translate(label),
                    MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}

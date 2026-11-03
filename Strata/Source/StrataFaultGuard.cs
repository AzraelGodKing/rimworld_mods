using System;
using RimWorld;
using Verse;

namespace Strata
{
    // Kill switches per subsystem. Repeated errors trip one system: it is
    // logged once (naming the system and what it was doing), only that system
    // stops, and vanilla plus the rest of Strata keep running. The Diagnostics
    // tab in mod options lists health and last error and can re-arm a system.
    //
    // The first four predate this and also flip their mod option off so the
    // trip survives a restart; re-arming turns that option back on. The rest
    // are session-only: a restart re-arms them.
    public static class StrataFaultGuard
    {
        public enum System
        {
            WorkRelay,
            Atmosphere,
            ShaftFluid,
            RobotSoftCompat,
            SeeBelow,
            CrossLevelCombat,
            Flood,
            Incidents,
            GravshipStacks,
            LevelGeneration,
            PowerShafts,
            CrossFloorScan,
            CombatExtendedCompat,
            GuardsForMeCompat,
            OpenTheWindowsCompat,
            ReGrowthCompat,
            PuahCompat,
            StorageCompat,
            VehicleFrameworkCompat,
            SisterModBridges,
        }

        private const int TripThreshold = 8;
        private const int WindowTicks = 15000;

        private static readonly int Count = Enum.GetValues(typeof(System)).Length;
        private static readonly int[] counts = new int[Count];
        private static readonly int[] windowStart = new int[Count];
        private static readonly int[] totalErrors = new int[Count];
        private static readonly bool[] tripped = new bool[Count];
        private static readonly string[] lastError = new string[Count];
        private static readonly int[] lastErrorTick = new int[Count];
        private static bool hadNaturalGases;
        private static bool hadPollutantGases;

        public static void ResetSession()
        {
            Array.Clear(counts, 0, counts.Length);
            Array.Clear(windowStart, 0, windowStart.Length);
            Array.Clear(totalErrors, 0, totalErrors.Length);
            Array.Clear(tripped, 0, tripped.Length);
            Array.Clear(lastError, 0, lastError.Length);
            Array.Clear(lastErrorTick, 0, lastErrorTick.Length);
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

        public static bool IsTripped(System system) => tripped[(int)system];

        public static int TotalErrors(System system) => totalErrors[(int)system];

        public static string LastError(System system) => lastError[(int)system];

        public static int LastErrorTick(System system) => lastErrorTick[(int)system];

        // Runs `body`; an exception is reported against `system` instead of
        // escaping into vanilla. Returns false when skipped or failed.
        public static bool Run(System system, string doing, Action body)
        {
            if (tripped[(int)system])
            {
                return false;
            }
            try
            {
                body();
                return true;
            }
            catch (Exception e)
            {
                Report(system, doing + ": " + e.GetType().Name + ": " + e.Message, e);
                return false;
            }
        }

        public static void Report(System system, string detail = null, Exception e = null)
        {
            int i = (int)system;
            int now = Find.TickManager?.TicksGame ?? 0;
            totalErrors[i]++;
            lastError[i] = detail.NullOrEmpty() ? e?.Message : detail;
            lastErrorTick[i] = now;
            if (e != null && totalErrors[i] == 1)
            {
                // Full trace once per system per session, then counts only.
                Log.Warning("[Strata] " + Label(system) + " error (" + detail + "): " + e);
            }
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

        public static void Rearm(System system)
        {
            int i = (int)system;
            bool wasTripped = tripped[i];
            tripped[i] = false;
            counts[i] = 0;
            windowStart[i] = 0;
            if (!wasTripped)
            {
                return;
            }
            StrataSettings s = StrataMod.Settings;
            if (s != null)
            {
                switch (system)
                {
                    case System.WorkRelay:
                        s.workRelayEnabled = true;
                        break;
                    case System.Atmosphere:
                        s.naturalGasesEnabled |= hadNaturalGases;
                        s.pollutantGasesEnabled |= hadPollutantGases;
                        break;
                    case System.ShaftFluid:
                        s.shaftFluidEnabled = true;
                        break;
                    case System.RobotSoftCompat:
                        s.robotSoftCompatEnabled = true;
                        break;
                }
            }
            Log.Message("[Strata] Re-armed " + Label(system) + ".");
        }

        public static string Label(System system)
        {
            switch (system)
            {
                case System.WorkRelay: return "work / haul relay";
                case System.Atmosphere: return "atmosphere / gases / smoke";
                case System.ShaftFluid: return "shaft fluid junctions";
                case System.RobotSoftCompat: return "robot soft-compat";
                case System.SeeBelow: return "see-below rendering";
                case System.CrossLevelCombat: return "cross-level combat";
                case System.Flood: return "flooding";
                case System.Incidents: return "Strata incidents";
                case System.GravshipStacks: return "gravship stacks";
                case System.LevelGeneration: return "level generation";
                case System.PowerShafts: return "power shafts";
                case System.CrossFloorScan: return "generic cross-floor job scan";
                case System.CombatExtendedCompat: return "Combat Extended bridge";
                case System.GuardsForMeCompat: return "Guards For Me bridge";
                case System.OpenTheWindowsCompat: return "Open The Windows bridge";
                case System.ReGrowthCompat: return "ReGrowth bridge";
                case System.PuahCompat: return "Pick Up And Haul bridge";
                case System.StorageCompat: return "storage mod bridge";
                case System.VehicleFrameworkCompat: return "Vehicle Framework bridge";
                case System.SisterModBridges: return "Azrael sister-mod bridges";
                default: return system.ToString();
            }
        }

        private static void ApplyTrip(System system, string detail)
        {
            StrataSettings s = StrataMod.Settings;
            switch (system)
            {
                case System.WorkRelay:
                    if (s != null)
                    {
                        s.workRelayEnabled = false;
                    }
                    break;
                case System.Atmosphere:
                    if (s != null)
                    {
                        hadNaturalGases = s.naturalGasesEnabled;
                        hadPollutantGases = s.pollutantGasesEnabled;
                        s.naturalGasesEnabled = false;
                        s.pollutantGasesEnabled = false;
                    }
                    break;
                case System.ShaftFluid:
                    if (s != null)
                    {
                        s.shaftFluidEnabled = false;
                    }
                    break;
                case System.RobotSoftCompat:
                    if (s != null)
                    {
                        s.robotSoftCompatEnabled = false;
                    }
                    break;
            }

            string label = Label(system);
            Log.Warning("[Strata] Auto-disabled " + label
                + " after repeated errors"
                + (detail.NullOrEmpty() ? "." : " while " + detail + ".")
                + " Vanilla and the rest of Strata keep running. Mod options > Diagnostics can re-arm it.");
            if (Current.ProgramState == ProgramState.Playing)
            {
                Messages.Message(
                    "Strata_FaultGuardTripped".Translate(label),
                    MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}

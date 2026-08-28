using System.Text;
using Verse;

namespace Strata
{
    /// <summary>
    /// Session counters for Misc. Robots soft-compat hooks. Cheap int increments
    /// on paths we already patch; rates use a 60-second rolling snapshot.
    /// </summary>
    public static class StrataRobotDiagnostics
    {
        public enum Counter
        {
            RechargeRelayPrefixHit,
            RechargeRelayPostfixFix,
            ReturnBasePortalJob,
            WorkSeekingGiverCall,
            WorkRelayPostfixHit,
            WorkRelayJobIssued,
            EnterPortalRobotJob,
            ReachableLevelsCall,
            BestFirstStepCall,
        }

        private const int RateWindowTicks = 3600;
        private const int LogIntervalTicks = 600;

        private static readonly int[] totals = new int[CounterCount()];
        private static readonly int[] rateBaseline = new int[CounterCount()];
        private static int rateBaselineTick = -1;
        private static int lastLogTick = -1;

        private static int CounterCount() => System.Enum.GetValues(typeof(Counter)).Length;

        [StrataSessionReset]
        internal static void ResetSession()
        {
            for (int i = 0; i < totals.Length; i++)
            {
                totals[i] = 0;
                rateBaseline[i] = 0;
            }
            rateBaselineTick = Find.TickManager?.TicksGame ?? 0;
            lastLogTick = rateBaselineTick;
        }

        internal static void Increment(Counter counter)
        {
            totals[(int)counter]++;
        }

        internal static void Tick()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }
            int tick = Find.TickManager.TicksGame;
            if (rateBaselineTick < 0)
            {
                rateBaselineTick = tick;
            }
            else if (tick - rateBaselineTick >= RateWindowTicks)
            {
                for (int i = 0; i < totals.Length; i++)
                {
                    rateBaseline[i] = totals[i];
                }
                rateBaselineTick = tick;
            }

            if (StrataMod.Settings?.logRobotDiagnostics == true && tick - lastLogTick >= LogIntervalTicks)
            {
                lastLogTick = tick;
                StrataLog.Verbose(BuildLogLine());
            }
        }

        public static int Total(Counter counter) => totals[(int)counter];

        public static int RatePer60Seconds(Counter counter)
        {
            return totals[(int)counter] - rateBaseline[(int)counter];
        }

        public static void ResetCounters()
        {
            ResetSession();
        }

        internal static string Label(Counter counter)
        {
            switch (counter)
            {
                case Counter.RechargeRelayPrefixHit:
                    return "Strata_RobotDiag_RechargeRelayPrefix".Translate();
                case Counter.RechargeRelayPostfixFix:
                    return "Strata_RobotDiag_RechargeRelayPostfix".Translate();
                case Counter.ReturnBasePortalJob:
                    return "Strata_RobotDiag_ReturnBasePortal".Translate();
                case Counter.WorkSeekingGiverCall:
                    return "Strata_RobotDiag_WorkSeekingCall".Translate();
                case Counter.WorkRelayPostfixHit:
                    return "Strata_RobotDiag_WorkRelayHit".Translate();
                case Counter.WorkRelayJobIssued:
                    return "Strata_RobotDiag_WorkRelayJob".Translate();
                case Counter.EnterPortalRobotJob:
                    return "Strata_RobotDiag_EnterPortalJob".Translate();
                case Counter.ReachableLevelsCall:
                    return "Strata_RobotDiag_ReachableLevels".Translate();
                case Counter.BestFirstStepCall:
                    return "Strata_RobotDiag_BestFirstStep".Translate();
                default:
                    return counter.ToString();
            }
        }

        private static string BuildLogLine()
        {
            var sb = new StringBuilder("[Strata] Robot diagnostics (60s rate / total): ");
            for (int i = 0; i < totals.Length; i++)
            {
                var counter = (Counter)i;
                if (i > 0)
                {
                    sb.Append("; ");
                }
                sb.Append(Label(counter));
                sb.Append('=');
                sb.Append(RatePer60Seconds(counter));
                sb.Append('/');
                sb.Append(Total(counter));
            }
            return sb.ToString();
        }
    }
}

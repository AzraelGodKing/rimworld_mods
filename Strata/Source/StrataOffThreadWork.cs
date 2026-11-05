namespace Strata
{
    // Off-thread atmosphere flag. Density/overlay work lives
    // in StrataOffThreadDensity; the retired per-cell breath job was removed.
    internal static class StrataOffThreadWork
    {
        public static bool OffThreadAtmosphereEnabled =>
            StrataMod.Settings?.offThreadAtmosphere != false
            && StrataMod.Settings?.NaturalGasesActive != false;

        public static void LogStartup()
        {
            StrataLog.Verbose("[Strata] Off-thread work: atmosphere=" + OffThreadAtmosphereEnabled
                + " workRelayBoard=" + (StrataMod.Settings?.OffThreadWorkRelayActive == true)
                + " (A1 buoyancy, A2 density, A3 overlay; rock plans use Parallel, cavern plans run inline;"
                + " work board packs grow cells on main, aggregates on worker).");
        }
    }
}

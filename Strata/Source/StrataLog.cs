using Verse;

namespace Strata
{
    // Routine / non-fatal diagnostics. Log.Message never pops the in-game debug
    // log window (unlike Warning/Error); keep that contract for land/takeoff noise.
    public static class StrataLog
    {
        public static bool VerboseEnabled =>
            StrataMod.Settings != null && StrataMod.Settings.verboseLogging;

        public static void Verbose(string text)
        {
            if (!VerboseEnabled)
            {
                return;
            }
            Log.Message(text);
        }

        public static void Message(string text)
        {
            Log.Message(text);
        }

        public static void Warning(string text)
        {
            // Non-breaking paths: still write to Player.log, but do not open the
            // debug log window the way Log.Warning does.
            Log.Message(text);
        }
    }
}

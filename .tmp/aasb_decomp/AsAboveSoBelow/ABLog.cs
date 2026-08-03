using Verse;

namespace AsAboveSoBelow;

public static class ABLog
{
	public const string Tag = "[As above, So below]";

	public static void Dev(string msg)
	{
		if (ABMod.Settings != null && ABMod.Settings.verboseLogging)
		{
			Log.Message("[As above, So below] " + msg);
		}
	}
}

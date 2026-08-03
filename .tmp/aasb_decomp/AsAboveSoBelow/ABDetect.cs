using Verse;

namespace AsAboveSoBelow;

public static class ABDetect
{
	public static bool Active(string packageId)
	{
		return ModLister.GetActiveModWithIdentifier(packageId, true) != null;
	}
}

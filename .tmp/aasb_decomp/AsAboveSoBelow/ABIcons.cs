using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

internal static class ABIcons
{
	private static Texture2D up;

	private static Texture2D down;

	public static Texture2D UpStairs => up ?? (up = ((BuildableDef)(DefDatabase<ThingDef>.GetNamedSilentFail("AB_StairsUp")?)).uiIcon);

	public static Texture2D DownStairs => down ?? (down = ((BuildableDef)(DefDatabase<ThingDef>.GetNamedSilentFail("AB_StairsDown")?)).uiIcon);
}

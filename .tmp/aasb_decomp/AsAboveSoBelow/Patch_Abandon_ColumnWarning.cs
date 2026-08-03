using System;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(SettlementAbandonUtility), "TryAbandonViaInterface")]
internal static class Patch_Abandon_ColumnWarning
{
	private static bool bypass;

	private static bool Prefix(MapParent settlement)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		if (bypass || !ABGuard.On(ABGuard.World))
		{
			return true;
		}
		MapParent obj = settlement;
		LevelComp levelComp = ((obj != null) ? obj.Map : null)?.Levels();
		if (levelComp == null || levelComp.level != 0)
		{
			return true;
		}
		int num = CountColonists(levelComp.upperMap) + CountColonists(levelComp.lowerMap);
		if (num == 0)
		{
			return true;
		}
		Find.WindowStack.Add((Window)(object)Dialog_MessageBox.CreateConfirmation(TranslatorFormattedStringExtensions.Translate("AB_AbandonLevelsWarning", NamedArgument.op_Implicit(num)), (Action)delegate
		{
			bypass = true;
			try
			{
				SettlementAbandonUtility.TryAbandonViaInterface(settlement);
			}
			finally
			{
				bypass = false;
			}
		}, false, (string)null, (WindowLayer)1));
		return false;
	}

	private static int CountColonists(Map level)
	{
		if (level == null || level.Disposed)
		{
			return 0;
		}
		return level.mapPawns.FreeColonistsSpawnedCount;
	}
}

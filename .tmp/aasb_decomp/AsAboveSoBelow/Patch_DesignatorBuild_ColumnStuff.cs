using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Designator_Build), "ProcessInput")]
internal static class Patch_DesignatorBuild_ColumnStuff
{
	private static readonly MethodInfo ThingsOfDefMethod = AccessTools.Method(typeof(ListerThings), "ThingsOfDef", (Type[])null, (Type[])null);

	private static readonly MethodInfo ReplacementMethod = AccessTools.Method(typeof(Patch_DesignatorBuild_ColumnStuff), "ColumnThingsOfDef", (Type[])null, (Type[])null);

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> list = new List<CodeInstruction>(instructions);
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (CodeInstructionExtensions.Calls(list[i], ThingsOfDefMethod))
			{
				list[i].opcode = OpCodes.Call;
				list[i].operand = ReplacementMethod;
				num++;
			}
		}
		if (num == 0)
		{
			Log.Warning("[As above, So below] Stuff picker patch found no ThingsOfDef call; cross level materials will not show in the build menu.");
		}
		return list;
	}

	public static List<Thing> ColumnThingsOfDef(ListerThings lister, ThingDef def)
	{
		List<Thing> list = lister.ThingsOfDef(def);
		if (list.Count > 0 || !ABGuard.On(ABGuard.Logistics))
		{
			return list;
		}
		try
		{
			Map currentMap = Find.CurrentMap;
			if (currentMap == null || currentMap.listerThings != lister)
			{
				return list;
			}
			LevelComp levelComp = currentMap.Levels();
			if (levelComp == null)
			{
				return list;
			}
			Map upperMap = levelComp.upperMap;
			if (upperMap != null && !upperMap.Disposed)
			{
				List<Thing> list2 = upperMap.listerThings.ThingsOfDef(def);
				if (list2.Count > 0)
				{
					return list2;
				}
			}
			Map lowerMap = levelComp.lowerMap;
			if (lowerMap != null && !lowerMap.Disposed)
			{
				List<Thing> list3 = lowerMap.listerThings.ThingsOfDef(def);
				if (list3.Count > 0)
				{
					return list3;
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "column stuff lookup");
		}
		return list;
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
public static class ABReverseCompat
{
	public static readonly bool Active;

	private static readonly MethodInfo pathInfoAddInfo;

	private const int MaxRemotePawnsPerMap = 15;

	static ABReverseCompat()
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		try
		{
			Active = ABDetect.Active("brrainz.reversecommands");
			if (Active)
			{
				Type type = AccessTools.TypeByName("ReverseCommands.Tools");
				MethodInfo methodInfo = ((type != null) ? AccessTools.Method(type, "GetPawnActions", (Type[])null, (Type[])null) : null);
				Type type2 = AccessTools.TypeByName("ReverseCommands.PathInfo");
				pathInfoAddInfo = ((type2 != null) ? AccessTools.Method(type2, "AddInfo", (Type[])null, (Type[])null) : null);
				if (methodInfo == null)
				{
					Log.Warning("[As above, So below] Reverse Commands detected but its Tools.GetPawnActions was not found; cross level support disabled.");
					Active = false;
				}
				else
				{
					HarmonyBoot.Harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(ABReverseCompat), "GetPawnActionsPostfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null);
					ABLog.Dev("Reverse Commands detected, cross level pawn actions enabled.");
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] Reverse Commands compat setup failed: " + ex.Message);
			Active = false;
		}
	}

	private static void GetPawnActionsPostfix(Dictionary<string, Dictionary<Pawn, FloatMenuOption>> __result)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.Logistics) || __result == null)
		{
			return;
		}
		try
		{
			Map currentMap = Find.CurrentMap;
			LevelComp levelComp = currentMap?.Levels();
			if (levelComp != null && (levelComp.upperMap != null || levelComp.lowerMap != null))
			{
				ABSettings settings = ABMod.Settings;
				if (settings != null && settings.crossLevelOrders)
				{
					Vector3 clickPos = UI.MouseMapPosition();
					Map below;
					Map targetMap = CrossLevelOrders.ResolveTargetMap(currentMap, clickPos, out below);
					string goHere = TaggedString.op_Implicit(Translator.Translate("GoHere"));
					AddColumnColonists(__result, currentMap, levelComp.upperMap, clickPos, targetMap, goHere);
					AddColumnColonists(__result, currentMap, levelComp.lowerMap, clickPos, targetMap, goHere);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "reverse commands cross level");
		}
	}

	private static void AddColumnColonists(Dictionary<string, Dictionary<Pawn, FloatMenuOption>> result, Map cur, Map other, Vector3 clickPos, Map targetMap, string goHere)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		if (other == null || other.Disposed)
		{
			return;
		}
		List<Pawn> freeColonists = other.mapPawns.FreeColonists;
		int num = 0;
		for (int i = 0; i < freeColonists.Count; i++)
		{
			Pawn val = freeColonists[i];
			if (!val.IsColonistPlayerControlled || val.Dead || !((Thing)val).Spawned || val.Downed || val.Drafted || LordUtility.GetLord(val) != null)
			{
				continue;
			}
			if (++num > 15)
			{
				break;
			}
			FloatMenuContext context;
			List<FloatMenuOption> list = CrossLevelOrders.BuildOptions(val, clickPos, cur, targetMap, out context);
			if (list == null || list.Count == 0)
			{
				continue;
			}
			pathInfoAddInfo?.Invoke(null, new object[2]
			{
				val,
				IntVec3Utility.ToIntVec3(clickPos)
			});
			for (int j = 0; j < list.Count; j++)
			{
				FloatMenuOption val2 = list[j];
				if (val2 != null && !val2.Disabled && !(val2.Label == goHere))
				{
					if (!result.TryGetValue(val2.Label, out var value))
					{
						value = new Dictionary<Pawn, FloatMenuOption>();
						result[val2.Label] = value;
					}
					value[val] = val2;
				}
			}
		}
	}
}

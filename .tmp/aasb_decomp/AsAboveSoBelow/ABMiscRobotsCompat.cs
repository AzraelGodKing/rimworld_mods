using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
internal static class ABMiscRobotsCompat
{
	private const int RouteCooldownTicks = 450;

	private static readonly ABPawnCooldown routeCooldown;

	private static FieldInfo rechargeStationField;

	private static bool active;

	static ABMiscRobotsCompat()
	{
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected O, but got Unknown
		routeCooldown = new ABPawnCooldown();
		try
		{
			if (!ABDetect.Active("Haplo.Miscellaneous.Robots"))
			{
				return;
			}
			Type type = AccessTools.TypeByName("AIRobot.X2_AIRobot");
			rechargeStationField = ((type != null) ? AccessTools.Field(type, "rechargeStation") : null);
			if (rechargeStationField == null)
			{
				Log.Warning("[As above, So below] Misc. Robots detected but its recharge station internals were not found; cross-level robot return routing is off.");
				return;
			}
			string[] array = new string[5] { "AIRobot.X2_JobGiver_RechargeEnergy", "AIRobot.X2_JobGiver_RechargeEnergyIdle", "AIRobot.X2_JobGiver_Return2BaseAndWait", "AIRobot.X2_JobGiver_Return2BaseDespawn", "AIRobot.X2_JobGiver_Return2BaseRoom" };
			HarmonyMethod val = new HarmonyMethod(typeof(ABMiscRobotsCompat), "ReturnJobPostfix", (Type[])null);
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				Type type2 = AccessTools.TypeByName(array[i]);
				MethodInfo methodInfo = ((type2 != null) ? AccessTools.DeclaredMethod(type2, "TryGiveJob", (Type[])null, (Type[])null) : null);
				if (methodInfo != null)
				{
					HarmonyBoot.Harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, val, (HarmonyMethod)null, (HarmonyMethod)null);
					num++;
				}
			}
			active = num > 0;
			if (active)
			{
				ABLog.Dev("Misc. Robots detected, cross-level return routing active (" + num + " givers patched).");
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] Misc. Robots compat setup failed: " + ex.Message);
		}
	}

	private static void ReturnJobPostfix(Pawn pawn, ref Job __result)
	{
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		if (!active || __result != null || !ABGuard.On(ABGuard.Movement))
		{
			return;
		}
		try
		{
			if (pawn == null || !((Thing)pawn).Spawned || pawn.Dead || pawn.Downed || ((Thing)pawn).Map == null || LordUtility.GetLord(pawn) != null)
			{
				return;
			}
			object value = rechargeStationField.GetValue(pawn);
			Thing val = (Thing)((value is Thing) ? value : null);
			if (val == null || val.Destroyed || !val.Spawned)
			{
				return;
			}
			Map map = val.Map;
			if (map == null || map == ((Thing)pawn).Map || !((Thing)pawn).Map.TryLinkedLevels(out var comp))
			{
				return;
			}
			Map val2;
			if (comp.upperMap == map || comp.lowerMap == map)
			{
				val2 = map;
			}
			else if (comp.upperMap != null && comp.upperMap.Levels()?.upperMap == map)
			{
				val2 = comp.upperMap;
			}
			else
			{
				if (comp.lowerMap == null || comp.lowerMap.Levels()?.lowerMap != map)
				{
					return;
				}
				val2 = comp.lowerMap;
			}
			int ticksGame = Find.TickManager.TicksGame;
			if (routeCooldown.Ready(pawn, ticksGame))
			{
				routeCooldown.ChargeUntil(pawn, ticksGame + 450);
				IntVec3 dest = ((val2 == map) ? val.Position : IntVec3.Invalid);
				if (CrossLevelWork.TryStairsJobToward(pawn, val2, dest, out var job))
				{
					ABLog.Dev("Routing robot " + ((Entity)pawn).LabelShort + " home toward its recharge station on level " + map.Level() + ".");
					__result = job;
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "robot return routing");
		}
	}
}

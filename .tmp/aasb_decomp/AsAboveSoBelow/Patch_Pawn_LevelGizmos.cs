using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Pawn), "GetGizmos")]
internal static class Patch_Pawn_LevelGizmos
{
	private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
	{
		foreach (Gizmo item in __result)
		{
			yield return item;
		}
		List<Gizmo> extras = null;
		try
		{
			extras = Build(__instance);
		}
		catch (Exception ex)
		{
			Log.WarningOnce("[As above, So below] Pawn level gizmos failed: " + ex, 762195845);
		}
		if (extras != null)
		{
			for (int i = 0; i < extras.Count; i++)
			{
				yield return extras[i];
			}
		}
	}

	private static List<Gizmo> Build(Pawn pawn)
	{
		if (!ABGuard.On(ABGuard.Movement) || !((Thing)pawn).Spawned)
		{
			return null;
		}
		bool flag = pawn.Drafted && pawn.IsColonistPlayerControlled;
		bool flag2 = !flag && pawn.RaceProps.Animal && ((Thing)pawn).Faction == Faction.OfPlayer && !pawn.Downed && pawn.training != null && pawn.training.HasLearned(TrainableDefOf.Obedience) && !AnimalPenUtility.NeedsToBeManagedByRope(pawn);
		if (!flag && !flag2)
		{
			return null;
		}
		LevelComp levelComp = ((Thing)pawn).Map.Levels();
		if (levelComp == null || (levelComp.upperMap == null && levelComp.lowerMap == null))
		{
			return null;
		}
		List<Gizmo> list = new List<Gizmo>();
		AddOption(list, pawn, levelComp.upperMap, up: true);
		AddOption(list, pawn, levelComp.lowerMap, up: false);
		return list;
	}

	private static void AddOption(List<Gizmo> list, Pawn pawn, Map target, bool up)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		if (target == null || target.Disposed)
		{
			return;
		}
		Building_ABStairs building_ABStairs = CrossLevelWork.NearestUsableStairs(pawn, target, checkReachability: false);
		Command_Action val = new Command_Action
		{
			defaultLabel = TaggedString.op_Implicit(Translator.Translate(up ? "AB_GoUp" : "AB_GoDown")),
			defaultDesc = TaggedString.op_Implicit(Translator.Translate(up ? "AB_GoUpDesc" : "AB_GoDownDesc")),
			icon = (Texture)(object)(up ? ABIcons.UpStairs : ABIcons.DownStairs)
		};
		if (building_ABStairs == null)
		{
			((Gizmo)val).Disable(TaggedString.op_Implicit(Translator.Translate("AB_NoStairs")));
		}
		else
		{
			Building_ABStairs chosen = building_ABStairs;
			Map chosenTarget = target;
			val.action = delegate
			{
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0029: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Unknown result type (might be due to invalid IL or missing references)
				Job val2 = JobMaker.MakeJob(ABDefOf.AB_UseStairs, LocalTargetInfo.op_Implicit((Thing)(object)chosen));
				val2.targetC = LocalTargetInfo.op_Implicit((Thing)(object)chosen.CounterpartTowards(chosenTarget));
				if (pawn.Drafted)
				{
					pawn.jobs.TryTakeOrderedJob(val2, (JobTag?)(JobTag)6, false);
				}
				else
				{
					pawn.jobs.StartJob(val2, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
				}
			};
		}
		list.Add((Gizmo)(object)val);
	}
}

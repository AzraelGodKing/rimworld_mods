using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Building_TurretGun), "GetGizmos")]
internal static class Patch_TurretGun_CrossLevelGizmos
{
	private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_TurretGun __instance)
	{
		foreach (Gizmo item in __result)
		{
			yield return item;
		}
		if (ABGuard.On(ABGuard.Combat) && ((Thing)__instance).Faction == Faction.OfPlayer && CrossLevelTurret.HasOrder((Building_Turret)(object)__instance, out var _, out var _))
		{
			yield return (Gizmo)new Command_Action
			{
				defaultLabel = TaggedString.op_Implicit(Translator.Translate("AB_StopCrossBombard")),
				defaultDesc = TaggedString.op_Implicit(Translator.Translate("AB_StopCrossBombardTip")),
				icon = (Texture)(object)TexCommand.ClearPrioritizedWork,
				action = delegate
				{
					CrossLevelTurret.Cancel((Building_Turret)(object)__instance);
				}
			};
		}
	}
}

using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AsAboveSoBelow;

internal static class CaravanAcrossLevels
{
	internal static bool Active(Map map, out LevelComp comp)
	{
		comp = null;
		if (!ABGuard.On(ABGuard.World))
		{
			return false;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.worldIntegration)
		{
			return false;
		}
		comp = map?.Levels();
		return comp != null && comp.level == 0 && (comp.upperMap != null || comp.lowerMap != null);
	}

	internal static bool ListableForCaravan(Pawn p)
	{
		return p != null && ((Thing)p).Spawned && !p.Dead && !p.Downed && !p.InMentalState && p.IsFreeColonist && p.RaceProps.Humanlike && p.RaceProps.allowedOnCaravan && !QuestUtility.IsQuestLodger(p) && !QuestUtility.IsQuestHelper(p) && LordUtility.GetLord(p) == null;
	}
}

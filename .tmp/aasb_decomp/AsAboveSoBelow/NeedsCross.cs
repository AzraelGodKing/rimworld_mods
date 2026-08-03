using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AsAboveSoBelow;

internal static class NeedsCross
{
	private const int FailCooldownTicks = 300;

	internal static readonly Dictionary<int, int> RestNext = new Dictionary<int, int>();

	internal static readonly Dictionary<int, int> FoodNext = new Dictionary<int, int>();

	internal static bool EligibleColonist(Pawn pawn)
	{
		return pawn != null && ((Thing)pawn).Spawned && pawn.IsColonistPlayerControlled && !pawn.Drafted && !pawn.Downed && LordUtility.GetLord(pawn) == null;
	}

	internal static bool EligiblePetForFood(Pawn pawn)
	{
		int result;
		if (pawn != null && ((Thing)pawn).Spawned && pawn.RaceProps != null && pawn.RaceProps.Animal && ((Thing)pawn).Faction == Faction.OfPlayer && !pawn.Downed && LordUtility.GetLord(pawn) == null && !AnimalPenUtility.NeedsToBeManagedByRope(pawn))
		{
			Pawn_PlayerSettings playerSettings = pawn.playerSettings;
			result = ((((playerSettings != null) ? playerSettings.AreaRestrictionInPawnCurrentMap : null) == null) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}

	internal static bool OnCooldown(Dictionary<int, int> table, Pawn pawn)
	{
		int value;
		return table.TryGetValue(((Thing)pawn).thingIDNumber, out value) && Find.TickManager.TicksGame < value;
	}

	internal static void Charge(Dictionary<int, int> table, Pawn pawn)
	{
		if (table.Count > 512)
		{
			table.Clear();
		}
		table[((Thing)pawn).thingIDNumber] = Find.TickManager.TicksGame + 300;
	}
}

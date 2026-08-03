using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class WorkGiver_ABWardenTakeToBedAcrossLevels : WorkGiver_Warden
{
	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.Logistics))
		{
			return null;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelPrisoners)
		{
			return null;
		}
		Map map = ((Thing)pawn).Map;
		if (map == null || !map.ConnectedToOtherLevel())
		{
			return null;
		}
		Pawn val = (Pawn)(object)((t is Pawn) ? t : null);
		if (val == null || !val.IsPrisonerOfColony || !((WorkGiver_Warden)this).ShouldTakeCareOfPrisoner(pawn, (Thing)(object)val, forced))
		{
			return null;
		}
		try
		{
			if (val.Downed && (!HealthAIUtility.ShouldSeekMedicalRest(val) || RestUtility.InBed(val)))
			{
				return null;
			}
			LevelComp levelComp = map.Levels();
			Pawn_Ownership ownership = val.ownership;
			Building_Bed val2 = ((ownership != null) ? ownership.OwnedBed : null);
			if (val2 == null || !((Thing)val2).Spawned || levelComp == null || (((Thing)val2).Map != levelComp.upperMap && ((Thing)val2).Map != levelComp.lowerMap))
			{
				if (RestUtility.FindBedFor(val, pawn, true, false, (GuestStatus?)(GuestStatus)1) != null)
				{
					return null;
				}
				if (!val.Downed && RestUtility.FindBedFor(val, val, true, false, (GuestStatus?)(GuestStatus)1) != null)
				{
					return null;
				}
			}
			Building_ABStairs exit;
			Building_ABStairs building_ABStairs = TakePawnAcrossLevels.FindStairsTowardBed(pawn, val, (GuestStatus)1, out exit);
			if (building_ABStairs == null || exit == null)
			{
				return null;
			}
			Job val3 = JobMaker.MakeJob(ABDefOf.AB_TakePrisonerAcrossLevels, LocalTargetInfo.op_Implicit((Thing)(object)val), LocalTargetInfo.op_Implicit((Thing)(object)building_ABStairs));
			val3.targetC = LocalTargetInfo.op_Implicit((Thing)(object)exit);
			val3.count = 1;
			return val3;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "cross level warden take to bed");
			return null;
		}
	}
}

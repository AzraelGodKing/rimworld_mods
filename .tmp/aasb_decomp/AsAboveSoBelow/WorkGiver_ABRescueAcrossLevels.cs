using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class WorkGiver_ABRescueAcrossLevels : WorkGiver_Scanner
{
	public override PathEndMode PathEndMode => (PathEndMode)1;

	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup((ThingRequestGroup)12);

	public override Danger MaxPathDanger(Pawn pawn)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return (Danger)3;
	}

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return (IEnumerable<Thing>)((Thing)pawn).Map.mapPawns.SpawnedDownedPawns;
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		if (!ABGuard.On(ABGuard.Logistics) || ABMod.Settings == null || !ABMod.Settings.crossLevelNeeds || !((Thing)pawn).Map.ConnectedToOtherLevel())
		{
			return true;
		}
		List<Pawn> list = ((Thing)pawn).Map.mapPawns.SpawnedPawnsInFaction(((Thing)pawn).Faction);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Downed && !RestUtility.InBed(list[i]))
			{
				return false;
			}
		}
		return true;
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		Pawn val = (Pawn)(object)((t is Pawn) ? t : null);
		if (val == null || RestUtility.InBed(val) || !HealthAIUtility.CanRescueNow(pawn, val, forced))
		{
			return false;
		}
		BreastfeedFailReason? val2 = default(BreastfeedFailReason?);
		if (ChildcareUtility.CanSuckle(val, ref val2))
		{
			return false;
		}
		if (RestUtility.FindBedFor(val, pawn, false, false, (GuestStatus?)null) != null)
		{
			return false;
		}
		Building_ABStairs exit;
		return TakePawnAcrossLevels.FindStairsTowardBed(pawn, val, null, out exit) != null;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		Pawn val = (Pawn)(object)((t is Pawn) ? t : null);
		if (val == null)
		{
			return null;
		}
		Building_ABStairs exit;
		Building_ABStairs building_ABStairs = TakePawnAcrossLevels.FindStairsTowardBed(pawn, val, null, out exit);
		if (building_ABStairs == null)
		{
			return null;
		}
		Job val2 = JobMaker.MakeJob(ABDefOf.AB_RescueAcrossLevels, LocalTargetInfo.op_Implicit((Thing)(object)val), LocalTargetInfo.op_Implicit((Thing)(object)building_ABStairs));
		val2.targetC = LocalTargetInfo.op_Implicit((Thing)(object)exit);
		val2.count = 1;
		return val2;
	}
}

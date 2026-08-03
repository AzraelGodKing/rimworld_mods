using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class WorkGiver_ABHaulAcrossLevels : WorkGiver_Scanner
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup((ThingRequestGroup)3);

	public override PathEndMode PathEndMode => (PathEndMode)3;

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		return !ABGuard.On(ABGuard.Logistics) || ABMod.Settings == null || !ABMod.Settings.crossLevelHauling || !((Thing)pawn).Map.ConnectedToOtherLevel() || CrossLevelWork.LowPowerWorker(pawn);
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		Building_ABStairs stairs;
		return CrossLevelHaul.TargetLevelFor(pawn, t, out stairs) != null;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Building_ABStairs stairs;
		Map val = CrossLevelHaul.TargetLevelFor(pawn, t, out stairs);
		if (val == null || stairs == null)
		{
			return null;
		}
		Job val2 = JobMaker.MakeJob(ABDefOf.AB_HaulAcrossLevels, LocalTargetInfo.op_Implicit(t), LocalTargetInfo.op_Implicit((Thing)(object)stairs));
		val2.targetC = LocalTargetInfo.op_Implicit((Thing)(object)stairs.CounterpartTowards(val));
		val2.count = Mathf.Min(t.stackCount, pawn.carryTracker.MaxStackSpaceEver(t.def));
		return val2;
	}
}

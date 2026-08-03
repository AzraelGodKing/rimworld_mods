using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class JobDriver_UseStairs : JobDriver
{
	private const int ClimbTicks = 90;

	private Building_ABStairs Stairs
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			LocalTargetInfo target = base.job.GetTarget((TargetIndex)1);
			return ((LocalTargetInfo)(ref target)).Thing as Building_ABStairs;
		}
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (ABGiddyUpCompat.BlockForMount(base.pawn))
		{
			return false;
		}
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		ToilFailConditions.FailOnDespawnedOrNull<JobDriver_UseStairs>(this, (TargetIndex)1);
		ToilFailConditions.FailOn<JobDriver_UseStairs>(this, (Func<bool>)(() => Stairs == null || !Stairs.HasAnyLink));
		yield return Toils_Goto.GotoThing((TargetIndex)1, (PathEndMode)2, false);
		Toil climb = Toils_General.Wait(Stairs?.ClimbTicksFor(base.pawn) ?? 90, (TargetIndex)1);
		ToilEffects.WithProgressBarToilDelay(climb, (TargetIndex)1, false, -0.5f);
		yield return climb;
		Toil transfer = ToilMaker.MakeToil("AB_Transfer");
		transfer.initAction = DoTransfer;
		transfer.defaultCompleteMode = (ToilCompleteMode)1;
		yield return transfer;
	}

	private void DoTransfer()
	{
		Building_ABStairs stairs = Stairs;
		Building_ABStairs building_ABStairs = ((LocalTargetInfo)(ref base.job.targetC)).Thing as Building_ABStairs;
		Building_ABStairs building_ABStairs2 = StairTransfer.ResolveNextDest(stairs, building_ABStairs);
		if (building_ABStairs2 != null)
		{
			StairTransfer.Transfer(base.pawn, stairs, CarriedIntent.Auto, building_ABStairs2, building_ABStairs);
		}
	}
}

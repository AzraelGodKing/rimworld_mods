using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class JobDriver_HaulAcrossLevels : JobDriver
{
	private const int ClimbTicks = 90;

	private Building_ABStairs Stairs
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			LocalTargetInfo target = base.job.GetTarget((TargetIndex)2);
			return ((LocalTargetInfo)(ref target)).Thing as Building_ABStairs;
		}
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if (ABGiddyUpCompat.BlockForMount(base.pawn))
		{
			return false;
		}
		return ReservationUtility.Reserve(base.pawn, base.job.GetTarget((TargetIndex)1), base.job, 1, -1, (ReservationLayerDef)null, errorOnFailed, false);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		ToilFailConditions.FailOnDestroyedOrNull<JobDriver_HaulAcrossLevels>(this, (TargetIndex)1);
		ToilFailConditions.FailOnDespawnedOrNull<JobDriver_HaulAcrossLevels>(this, (TargetIndex)2);
		ToilFailConditions.FailOn<JobDriver_HaulAcrossLevels>(this, (Func<bool>)(() => Stairs == null || !Stairs.HasAnyLink));
		ToilFailConditions.FailOnForbidden<JobDriver_HaulAcrossLevels>(this, (TargetIndex)1);
		yield return ToilFailConditions.FailOnSomeonePhysicallyInteracting<Toil>(Toils_Goto.GotoThing((TargetIndex)1, (PathEndMode)3, false), (TargetIndex)1);
		yield return Toils_Haul.StartCarryThing((TargetIndex)1, false, false, false, true, false);
		yield return Toils_Goto.GotoThing((TargetIndex)2, (PathEndMode)2, false);
		Toil climb = Toils_General.Wait(Stairs?.ClimbTicksFor(base.pawn) ?? 90, (TargetIndex)2);
		ToilEffects.WithProgressBarToilDelay(climb, (TargetIndex)2, false, -0.5f);
		yield return climb;
		Toil transfer = ToilMaker.MakeToil("AB_HaulTransfer");
		transfer.initAction = delegate
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			LocalTargetInfo target = base.job.GetTarget((TargetIndex)3);
			Building_ABStairs explicitDest = ((LocalTargetInfo)(ref target)).Thing as Building_ABStairs;
			StairTransfer.Transfer(base.pawn, Stairs, CarriedIntent.Auto, explicitDest);
		};
		transfer.defaultCompleteMode = (ToilCompleteMode)1;
		yield return transfer;
	}
}

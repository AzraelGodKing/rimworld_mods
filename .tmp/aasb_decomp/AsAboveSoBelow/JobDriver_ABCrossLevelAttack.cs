using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class JobDriver_ABCrossLevelAttack : JobDriver
{
	private Thing target;

	private bool warmedUp;

	private int warmupTicksLeft = -1;

	private int burstShotsLeft;

	private int ticksToNextShot;

	private int cooldownTicksLeft;

	private Verb_LaunchProjectile cachedVerb;

	private int verbStaleAt;

	private const int LofCheckInterval = 15;

	private int lofCheckIn;

	internal Thing Target => target;

	internal bool Warming => !warmedUp && warmupTicksLeft > 0;

	internal int WarmupTicksLeft => warmupTicksLeft;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	public override string GetReport()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (target != null && !target.Destroyed)
		{
			return TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_ReportAttackingAcross", NamedArgument.op_Implicit(((Entity)target).LabelShort)));
		}
		return ((JobDriver)this).GetReport();
	}

	public override void Notify_Starting()
	{
		((JobDriver)this).Notify_Starting();
		if (target == null && CrossLevelCombat.PendingTargets.TryGetValue(((Thing)base.pawn).thingIDNumber, out var value))
		{
			target = value;
		}
		CrossLevelCombat.PendingTargets.Remove(((Thing)base.pawn).thingIDNumber);
	}

	public override void ExposeData()
	{
		((JobDriver)this).ExposeData();
		Scribe_References.Look<Thing>(ref target, "AB_target", false);
		Scribe_Values.Look<bool>(ref warmedUp, "AB_warmedUp", false, false);
		Scribe_Values.Look<int>(ref warmupTicksLeft, "AB_warmupLeft", -1, false);
		Scribe_Values.Look<int>(ref burstShotsLeft, "AB_burstLeft", 0, false);
		Scribe_Values.Look<int>(ref ticksToNextShot, "AB_toNextShot", 0, false);
		Scribe_Values.Look<int>(ref cooldownTicksLeft, "AB_cooldownLeft", 0, false);
	}

	private Verb_LaunchProjectile ResolvedVerb()
	{
		int ticksGame = Find.TickManager.TicksGame;
		if (cachedVerb != null && ticksGame < verbStaleAt)
		{
			Thing equipmentSource = (Thing)(object)((Verb)cachedVerb).EquipmentSource;
			if (equipmentSource == null || !equipmentSource.Destroyed)
			{
				return cachedVerb;
			}
		}
		cachedVerb = CrossLevelCombat.GetRangedVerb(base.pawn);
		verbStaleAt = ticksGame + 15;
		return cachedVerb;
	}

	private bool Valid()
	{
		if (!CrossLevelCombat.Enabled)
		{
			return false;
		}
		if (target == null || target.Destroyed || !target.Spawned)
		{
			return false;
		}
		if (base.pawn.Downed || base.pawn.Dead)
		{
			return false;
		}
		if (base.pawn.IsColonistPlayerControlled && !base.pawn.Drafted)
		{
			return false;
		}
		return ResolvedVerb() != null;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		CrossLevelCombatUI.ActiveShooters.Add(base.pawn);
		((JobDriver)this).AddFinishAction((Action<JobCondition>)delegate
		{
			CrossLevelCombatUI.ActiveShooters.Remove(base.pawn);
		});
		ToilFailConditions.FailOn<JobDriver_ABCrossLevelAttack>(this, (Func<bool>)(() => !Valid()));
		yield return Toils_Goto.GotoCell((TargetIndex)2, (PathEndMode)1);
		Toil fire = ToilMaker.MakeToil("AB_CrossFire");
		fire.defaultCompleteMode = (ToilCompleteMode)5;
		fire.handlingFacing = true;
		fire.tickAction = FireTick;
		yield return fire;
	}

	private void FireTick()
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		Verb_LaunchProjectile val = ResolvedVerb();
		if (val == null)
		{
			((JobDriver)this).EndJobWith((JobCondition)4);
			return;
		}
		if (--lofCheckIn <= 0)
		{
			if (!CrossLevelCombat.CanFireFrom(((Thing)base.pawn).Map, ((Thing)base.pawn).Position, target, val, out var _))
			{
				((JobDriver)this).EndJobWith((JobCondition)4);
				return;
			}
			lofCheckIn = 15;
		}
		base.pawn.rotationTracker.FaceCell(target.Position);
		if (cooldownTicksLeft > 0)
		{
			cooldownTicksLeft--;
			return;
		}
		if (!warmedUp)
		{
			if (warmupTicksLeft < 0)
			{
				float num = ((Verb)val).verbProps.warmupTime;
				try
				{
					num *= StatExtension.GetStatValue((Thing)(object)base.pawn, StatDefOf.AimingDelayFactor, true, -1);
				}
				catch
				{
				}
				warmupTicksLeft = SecondsToTicks(num);
			}
			if (warmupTicksLeft > 0)
			{
				warmupTicksLeft--;
				return;
			}
			warmedUp = true;
		}
		if (burstShotsLeft <= 0)
		{
			burstShotsLeft = Mathf.Max(1, ((Verb)val).verbProps.burstShotCount);
			ticksToNextShot = 0;
		}
		if (ticksToNextShot > 0)
		{
			ticksToNextShot--;
			return;
		}
		if (!CrossLevelCombat.Fire((Thing)(object)base.pawn, val, target))
		{
			((JobDriver)this).EndJobWith((JobCondition)4);
			return;
		}
		burstShotsLeft--;
		if (burstShotsLeft > 0)
		{
			ticksToNextShot = Mathf.Max(0, ((Verb)val).verbProps.ticksBetweenBurstShots);
			return;
		}
		cooldownTicksLeft = CooldownTicks((Verb)(object)val, base.pawn);
		warmedUp = false;
		warmupTicksLeft = -1;
	}

	private static int CooldownTicks(Verb verb, Pawn pawn)
	{
		try
		{
			return Mathf.Max(1, SecondsToTicks(verb.verbProps.AdjustedCooldown(verb, pawn)));
		}
		catch
		{
			return Mathf.Max(1, SecondsToTicks(verb.verbProps.defaultCooldownTime));
		}
	}

	private static int SecondsToTicks(float seconds)
	{
		return Mathf.Max(0, Mathf.RoundToInt(seconds * 60f));
	}
}

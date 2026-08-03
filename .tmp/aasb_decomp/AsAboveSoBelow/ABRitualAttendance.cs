using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

internal static class ABRitualAttendance
{
	private sealed class Pending
	{
		public RitualBehaviorWorker behavior;

		public TargetInfo target;

		public Pawn organizer;

		public Precept_Ritual ritual;

		public RitualObligation obligation;

		public RitualRoleAssignments assignments;

		public bool playerForced;

		public int timeoutAt;
	}

	[ThreadStatic]
	private static bool candidateScope;

	internal static bool Executing;

	[ThreadStatic]
	private static bool merging;

	private static readonly List<Pending> pending = new List<Pending>();

	private const int GatherTimeoutTicks = 18000;

	internal static bool Enabled
	{
		get
		{
			ABSettings settings = ABMod.Settings;
			return ABGuard.On(ABGuard.Movement) && settings != null && settings.crossLevelRituals;
		}
	}

	internal static bool AnyPending => pending.Count > 0;

	internal static void EnterScope()
	{
		candidateScope = true;
	}

	internal static void ExitScope()
	{
		candidateScope = false;
	}

	internal static List<Pawn> TryMergeCandidates(Map map, List<Pawn> vanilla)
	{
		if (merging || !candidateScope || !Enabled || map == null || vanilla == null)
		{
			return null;
		}
		LevelComp levelComp = map.Levels();
		if (levelComp == null)
		{
			return null;
		}
		merging = true;
		try
		{
			List<Pawn> merged = null;
			AppendFrom(levelComp.upperMap, map, vanilla, ref merged);
			AppendFrom(levelComp.lowerMap, map, vanilla, ref merged);
			return merged;
		}
		finally
		{
			merging = false;
		}
	}

	private static void AppendFrom(Map other, Map ritualMap, List<Pawn> vanilla, ref List<Pawn> merged)
	{
		if (other == null || other.Disposed || other == ritualMap)
		{
			return;
		}
		List<Pawn> freeColonistsAndPrisonersSpawned = other.mapPawns.FreeColonistsAndPrisonersSpawned;
		for (int i = 0; i < freeColonistsAndPrisonersSpawned.Count; i++)
		{
			Pawn val = freeColonistsAndPrisonersSpawned[i];
			if (val != null && !val.Dead && !val.Downed && ((Thing)val).Spawned && !val.IsPrisoner && CrossLevelWork.NearestUsableStairsCached(val, ritualMap)?.CounterpartTowards(ritualMap) != null)
			{
				if (merged == null)
				{
					merged = new List<Pawn>(vanilla);
				}
				if (!merged.Contains(val))
				{
					merged.Add(val);
				}
			}
		}
	}

	internal static void ClearAll()
	{
		pending.Clear();
	}

	internal static bool BeginGather(RitualBehaviorWorker behavior, TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments, bool playerForced)
	{
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Map map = ((TargetInfo)(ref target)).Map;
			if (map == null || assignments == null)
			{
				return false;
			}
			int num = 0;
			List<Pawn> participants = assignments.Participants;
			for (int i = 0; i < participants.Count; i++)
			{
				Pawn val = participants[i];
				if (val != null && !val.Dead && ((Thing)val).Spawned && ((Thing)val).MapHeld != map)
				{
					if (!StairRouter.TryBestToward(val, map, ((TargetInfo)(ref target)).Cell, out var stairs, out var exit))
					{
						stairs = CrossLevelWork.NearestUsableStairsCached(val, map);
						exit = stairs?.CounterpartTowards(map);
					}
					if (exit == null)
					{
						Messages.Message(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_RitualNoRoute", NamedArgument.op_Implicit(((Entity)val).LabelShort))), LookTargets.op_Implicit((Thing)(object)val), MessageTypeDefOf.RejectInput, false);
						return false;
					}
					Job val2 = CrossLevelWork.MakeStairsJob(stairs, exit);
					Pawn_JobTracker jobs = val.jobs;
					if (jobs != null)
					{
						jobs.StartJob(val2, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
					}
					num++;
				}
			}
			if (num == 0)
			{
				return false;
			}
			if (pending.Count > 8)
			{
				pending.RemoveAt(0);
			}
			pending.Add(new Pending
			{
				behavior = behavior,
				target = target,
				organizer = organizer,
				ritual = ritual,
				obligation = obligation,
				assignments = assignments,
				playerForced = playerForced,
				timeoutAt = Find.TickManager.TicksGame + 18000
			});
			Messages.Message(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_RitualGathering", NamedArgument.op_Implicit(num))), LookTargets.op_Implicit(target), MessageTypeDefOf.NeutralEvent, false);
			return true;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "ritual gather begin");
			return false;
		}
	}

	internal static void Tick()
	{
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		if (ABRitualAttendance.pending.Count == 0)
		{
			return;
		}
		int ticksGame = Find.TickManager.TicksGame;
		for (int num = ABRitualAttendance.pending.Count - 1; num >= 0; num--)
		{
			Pending pending = ABRitualAttendance.pending[num];
			Map map = ((TargetInfo)(ref pending.target)).Map;
			if (map == null || map.Disposed || pending.behavior == null)
			{
				ABRitualAttendance.pending.RemoveAt(num);
			}
			else if (ticksGame > pending.timeoutAt)
			{
				ABRitualAttendance.pending.RemoveAt(num);
				Precept_Ritual ritual = pending.ritual;
				Messages.Message(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_RitualGatherTimeout", NamedArgument.op_Implicit(((ritual != null) ? ((Precept)ritual).LabelCap : null) ?? Translator.TranslateSimple("ritual")))), LookTargets.op_Implicit(pending.target), MessageTypeDefOf.NegativeEvent, false);
			}
			else
			{
				bool flag = true;
				List<Pawn> participants = pending.assignments.Participants;
				for (int i = 0; i < participants.Count; i++)
				{
					Pawn val = participants[i];
					if (val != null && !val.Dead && ((Thing)val).MapHeld != map)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					ABRitualAttendance.pending.RemoveAt(num);
					try
					{
						Executing = true;
						pending.behavior.TryExecuteOn(pending.target, pending.organizer, pending.ritual, pending.obligation, pending.assignments, pending.playerForced);
					}
					catch (Exception e)
					{
						ABGuard.Disable(ABGuard.Movement, e, "ritual gather start");
					}
					finally
					{
						Executing = false;
					}
				}
			}
		}
	}
}

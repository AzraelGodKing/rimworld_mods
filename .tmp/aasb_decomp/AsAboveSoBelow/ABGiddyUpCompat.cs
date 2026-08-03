using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

internal static class ABGiddyUpCompat
{
	private static bool resolved;

	private static JobDef mountedDef;

	private static JobDef MountedDef
	{
		get
		{
			if (!resolved)
			{
				resolved = true;
				if (ABDetect.Active("MemeGoddess.GiddyUp") || ABDetect.Active("Owlchemist.GiddyUp") || ABDetect.Active("Roolo.GiddyUpCore"))
				{
					mountedDef = DefDatabase<JobDef>.GetNamedSilentFail("Mounted");
				}
			}
			return mountedDef;
		}
	}

	public static bool IsMounted(Pawn rider)
	{
		JobDef val = MountedDef;
		if (val == null || rider == null || !((Thing)rider).Spawned || ((Thing)rider).Map == null)
		{
			return false;
		}
		IReadOnlyList<Pawn> allPawnsSpawned = ((Thing)rider).Map.mapPawns.AllPawnsSpawned;
		for (int i = 0; i < allPawnsSpawned.Count; i++)
		{
			Pawn val2 = allPawnsSpawned[i];
			if (val2.CurJobDef == val)
			{
				Job curJob = val2.CurJob;
				if ((object)((curJob != null) ? ((LocalTargetInfo)(ref curJob.targetA)).Thing : null) == rider)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsCarryingRider(Pawn animal)
	{
		JobDef val = MountedDef;
		return val != null && ((animal != null) ? animal.CurJobDef : null) == val;
	}

	public static bool BlockForMount(Pawn rider)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (!IsMounted(rider))
		{
			return false;
		}
		Messages.Message(TaggedString.op_Implicit(Translator.Translate("AB_DismountFirst")), LookTargets.op_Implicit((Thing)(object)rider), MessageTypeDefOf.RejectInput, false);
		return true;
	}
}

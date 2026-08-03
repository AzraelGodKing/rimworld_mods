using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

public class PodTransitComp : MapComponent
{
	private List<Skyfaller> lift;

	private List<Skyfaller> descending;

	private List<int> transferAt;

	public PodTransitComp(Map map)
		: base(map)
	{
	}

	internal void RegisterLift(Skyfaller sf)
	{
		if (lift == null)
		{
			lift = new List<Skyfaller>();
		}
		if (!lift.Contains(sf))
		{
			lift.Add(sf);
		}
	}

	internal void RegisterDescent(Skyfaller sf, int at)
	{
		if (descending == null)
		{
			descending = new List<Skyfaller>();
			transferAt = new List<int>();
		}
		if (!descending.Contains(sf))
		{
			descending.Add(sf);
			transferAt.Add(at);
		}
	}

	internal bool DevQueuedForLift(Skyfaller sf)
	{
		return lift != null && lift.Contains(sf);
	}

	internal int DevTransferAt(Skyfaller sf)
	{
		int num = descending?.IndexOf(sf) ?? (-1);
		return (num < 0) ? (-1) : transferAt[num];
	}

	public override void MapComponentTick()
	{
		bool flag = lift != null && lift.Count > 0;
		bool flag2 = descending != null && descending.Count > 0;
		if (!flag && !flag2)
		{
			return;
		}
		if (!ABGuard.On(ABGuard.Transit))
		{
			lift?.Clear();
			descending?.Clear();
			transferAt?.Clear();
			return;
		}
		try
		{
			if (flag)
			{
				ProcessLift();
			}
			if (flag2)
			{
				ProcessDescent();
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Transit, e, "pod transit tick");
		}
	}

	private void ProcessLift()
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		for (int num = lift.Count - 1; num >= 0; num--)
		{
			Skyfaller val = lift[num];
			lift.RemoveAt(num);
			if (val != null && !((Thing)val).Destroyed && ((Thing)val).Spawned && ((Thing)val).Map == base.map)
			{
				Map val2 = base.map.Levels()?.upperMap;
				if (val2 != null && !val2.Disposed && PodTransit.GapOpen(val2, GenAdj.OccupiedRect((Thing)(object)val)))
				{
					int num2 = Math.Min(22, val.ticksToImpact / 2);
					if (num2 >= 16 && PodTransit.Transfer(val, val2))
					{
						val2.GetComponent<PodTransitComp>()?.RegisterDescent(val, num2);
					}
				}
			}
		}
	}

	private void ProcessDescent()
	{
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		for (int num = descending.Count - 1; num >= 0; num--)
		{
			Skyfaller val = descending[num];
			if (val == null || ((Thing)val).Destroyed || !((Thing)val).Spawned || ((Thing)val).Map != base.map)
			{
				if (val != null && ((Thing)val).Destroyed)
				{
					IntVec3 position = ((Thing)val).Position;
					if (((IntVec3)(ref position)).IsValid && GenGrid.InBounds(((Thing)val).Position, base.map))
					{
						FleckMaker.Static(((Thing)val).Position, base.map, FleckDefOf.ExplosionFlash, 3f);
						position = ((Thing)val).Position;
						FleckMaker.ThrowSmoke(((IntVec3)(ref position)).ToVector3Shifted(), base.map, 2.5f);
						position = ((Thing)val).Position;
						FleckMaker.ThrowMicroSparks(((IntVec3)(ref position)).ToVector3Shifted(), base.map);
					}
				}
				descending.RemoveAt(num);
				transferAt.RemoveAt(num);
			}
			else if (val.ticksToImpact <= transferAt[num])
			{
				descending.RemoveAt(num);
				transferAt.RemoveAt(num);
				if (PodTransit.GapOpen(base.map, GenAdj.OccupiedRect((Thing)(object)val)))
				{
					Map val2 = base.map.Levels()?.lowerMap;
					if (val2 != null && !val2.Disposed && GenGrid.InBounds(((Thing)val).Position, val2))
					{
						PodTransit.Transfer(val, val2);
					}
				}
			}
		}
	}

	public override void ExposeData()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Invalid comparison between Unknown and I4
		((MapComponent)this).ExposeData();
		Scribe_Collections.Look<Skyfaller>(ref lift, "abTransitLift", (LookMode)3, Array.Empty<object>());
		Scribe_Collections.Look<Skyfaller>(ref descending, "abTransitDescending", (LookMode)3, Array.Empty<object>());
		Scribe_Collections.Look<int>(ref transferAt, "abTransitTransferAt", (LookMode)1, Array.Empty<object>());
		if ((int)Scribe.mode != 4)
		{
			return;
		}
		lift?.RemoveAll((Skyfaller x) => x == null || ((Thing)x).Destroyed);
		if (descending == null)
		{
			return;
		}
		if (transferAt == null || transferAt.Count != descending.Count)
		{
			descending.Clear();
			transferAt = new List<int>();
			return;
		}
		for (int num = descending.Count - 1; num >= 0; num--)
		{
			if (descending[num] == null || ((Thing)descending[num]).Destroyed)
			{
				descending.RemoveAt(num);
				transferAt.RemoveAt(num);
			}
		}
	}
}

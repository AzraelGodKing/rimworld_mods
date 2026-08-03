using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

internal static class PodTransit
{
	internal const int MinTransitTicks = 40;

	internal const int HandoffTicksToImpact = 22;

	private static readonly HashSet<string> transitDefNames = new HashSet<string> { "CrashedShipPartIncoming", "ShipChunkIncoming", "ShipChunkIncoming_SmallExplosion", "MeteoriteIncoming", "MeteoriteCraterIncoming", "VFEI_InsectMeteoriteIncoming" };

	private static readonly FieldRef<Skyfaller, int> ticksToImpactMaxRef = AccessTools.FieldRefAccess<Skyfaller, int>("ticksToImpactMax");

	internal static bool InTransfer { get; private set; }

	internal static event Action<Skyfaller, Map, Map> DevTransferred;

	internal static bool IsTransitDef(ThingDef def)
	{
		if (def?.skyfaller == null || def.skyfaller.reversed)
		{
			return false;
		}
		ABSkyfallerTransit modExtension = ((Def)def).GetModExtension<ABSkyfallerTransit>();
		if (modExtension != null)
		{
			return modExtension.transit;
		}
		if (def.thingClass != null && typeof(DropPodIncoming).IsAssignableFrom(def.thingClass))
		{
			return true;
		}
		return transitDefNames.Contains(((Def)def).defName);
	}

	internal unsafe static bool GapOpen(Map sky, CellRect rect)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
		Enumerator enumerator = ((CellRect)(ref rect)).GetEnumerator();
		try
		{
			while (((Enumerator)(ref enumerator)).MoveNext())
			{
				IntVec3 current = ((Enumerator)(ref enumerator)).Current;
				if (!GenGrid.InBounds(current, sky) || sky.terrainGrid.TerrainAt(current) != aB_OpenAir || sky.roofGrid.Roofed(current))
				{
					return false;
				}
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
		return true;
	}

	internal static bool Transfer(Skyfaller sf, Map to)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		Map map = ((Thing)sf).Map;
		IntVec3 position = ((Thing)sf).Position;
		Rot4 rotation = ((Thing)sf).Rotation;
		int ticksToImpact = sf.ticksToImpact;
		int num = ticksToImpactMaxRef.Invoke(sf);
		int ticksToDiscard = sf.ticksToDiscard;
		float angle = sf.angle;
		int ageTicks = sf.ageTicks;
		InTransfer = true;
		try
		{
			((Entity)sf).DeSpawn((DestroyMode)0);
			GenSpawn.Spawn((Thing)(object)sf, position, to, rotation, (WipeMode)0, false, false);
			sf.ticksToImpact = ticksToImpact;
			ticksToImpactMaxRef.Invoke(sf) = num;
			sf.ticksToDiscard = ticksToDiscard;
			sf.angle = angle;
			sf.ageTicks = ageTicks;
			PodTransit.DevTransferred?.Invoke(sf, map, to);
			ABLog.Dev("Pod transit: " + ((Def)((Thing)sf).def).defName + " moved from map " + map.uniqueID + " to map " + to.uniqueID + " with " + ticksToImpact + " ticks to impact.");
			return true;
		}
		catch (Exception e)
		{
			if (!((Thing)sf).Destroyed && !((Thing)sf).Spawned)
			{
				try
				{
					GenSpawn.Spawn((Thing)(object)sf, position, map, rotation, (WipeMode)0, false, false);
					sf.ticksToImpact = ticksToImpact;
					ticksToImpactMaxRef.Invoke(sf) = num;
					sf.ticksToDiscard = ticksToDiscard;
					sf.angle = angle;
					sf.ageTicks = ageTicks;
				}
				catch (Exception)
				{
				}
			}
			ABGuard.Disable(ABGuard.Transit, e, "skyfaller level transfer");
			return false;
		}
		finally
		{
			InTransfer = false;
		}
	}
}

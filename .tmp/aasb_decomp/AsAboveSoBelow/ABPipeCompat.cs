using System.Collections.Generic;
using Verse;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
public static class ABPipeCompat
{
	public static readonly bool DbhActive;

	public static readonly bool VefActive;

	public static readonly bool RimefellerActive;

	static ABPipeCompat()
	{
		DbhActive = ABDetect.Active("Dubwise.DubsBadHygiene");
		VefActive = ABDetect.Active("OskarPotocki.VanillaFactionsExpanded.Core");
		RimefellerActive = ABDetect.Active("Dubwise.Rimefeller");
		if (DbhActive)
		{
			ABLog.Dev("Dubs Bad Hygiene detected, direct water bridging enabled.");
		}
		if (VefActive)
		{
			ABLog.Dev("VEF pipe system detected, direct resource bridging enabled (VPE, Helixien, VTE).");
		}
		if (RimefellerActive)
		{
			ABLog.Dev("Rimefeller detected, oil and fuel bridging enabled.");
		}
	}

	private static void BridgeLink(Building_ABStairs a, Building_ABStairs b)
	{
		if (b == null || !((Thing)b).Spawned || ((Thing)b).Map == null || ((Thing)b).Map.Disposed)
		{
			return;
		}
		ABStairsExtension ext = a.Ext;
		if (ext != null)
		{
			if (DbhActive && ext.bridgeWater)
			{
				DBHWaterBridge.BridgePair(a, b);
			}
			if (VefActive && ext.bridgeVef)
			{
				VEFPipeBridge.BridgePair(a, b);
			}
			if (RimefellerActive && ext.bridgeChem)
			{
				RimefellerBridge.BridgePair(a, b);
			}
		}
	}

	public static void TickGroundPairs(LevelComp groundComp)
	{
		if (!DbhActive && !VefActive && !RimefellerActive)
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelPipes)
		{
			return;
		}
		List<Building_ABStairs> stairs = groundComp.Stairs;
		for (int i = 0; i < stairs.Count; i++)
		{
			Building_ABStairs building_ABStairs = stairs[i];
			if (building_ABStairs != null && ((Thing)building_ABStairs).Spawned)
			{
				BridgeLink(building_ABStairs, building_ABStairs.Counterpart);
				BridgeLink(building_ABStairs, building_ABStairs.SecondCounterpart);
			}
		}
	}
}

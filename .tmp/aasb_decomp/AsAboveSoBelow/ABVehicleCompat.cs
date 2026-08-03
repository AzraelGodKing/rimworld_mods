using System;
using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
public static class ABVehicleCompat
{
	private static readonly Type vehicleType;

	static ABVehicleCompat()
	{
		if (ABDetect.Active("SmashPhil.VehicleFramework"))
		{
			vehicleType = AccessTools.TypeByName("Vehicles.VehiclePawn");
			if (vehicleType != null)
			{
				ABLog.Dev("Vehicle Framework detected, vehicles excluded from stair routing.");
			}
		}
	}

	public static bool IsVehicle(Pawn p)
	{
		return vehicleType != null && p != null && vehicleType.IsInstanceOfType(p);
	}
}

using System;
using HarmonyLib;
using RimWorld;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(AlertsReadout), "AlertsReadoutUpdate")]
internal static class Patch_AlertsScope_VanillaHome
{
	internal static int Depth;

	private static void Prefix()
	{
		Depth++;
	}

	private static void Finalizer()
	{
		Depth = Math.Max(0, Depth - 1);
	}
}

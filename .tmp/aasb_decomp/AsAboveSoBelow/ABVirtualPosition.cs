using System;
using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

internal static class ABVirtualPosition
{
	public struct Token
	{
		internal sbyte mapIndex;

		internal IntVec3 pos;
	}

	private static readonly FieldRef<Thing, sbyte> MapIndexRef = AccessTools.FieldRefAccess<Thing, sbyte>("mapIndexOrState");

	private static readonly FieldRef<Thing, IntVec3> PositionRef = AccessTools.FieldRefAccess<Thing, IntVec3>("positionInt");

	public static bool TrySwap(Pawn pawn, Map target, IntVec3 cell, out Token token)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		token = default(Token);
		sbyte b = (sbyte)Find.Maps.IndexOf(target);
		if (b < 0)
		{
			return false;
		}
		token.mapIndex = MapIndexRef.Invoke((Thing)(object)pawn);
		token.pos = PositionRef.Invoke((Thing)(object)pawn);
		MapIndexRef.Invoke((Thing)(object)pawn) = b;
		PositionRef.Invoke((Thing)(object)pawn) = cell;
		return true;
	}

	public static void Restore(Pawn pawn, Token token)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		MapIndexRef.Invoke((Thing)(object)pawn) = token.mapIndex;
		PositionRef.Invoke((Thing)(object)pawn) = token.pos;
	}

	public static bool WithPawnAt(Pawn pawn, Map target, IntVec3 cell, Func<bool> scan)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		if (!TrySwap(pawn, target, cell, out var token))
		{
			return false;
		}
		try
		{
			return scan();
		}
		finally
		{
			Restore(pawn, token);
		}
	}

	public static IntVec3 SwapPositionOnly(Thing thing, IntVec3 cell)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		IntVec3 result = PositionRef.Invoke(thing);
		PositionRef.Invoke(thing) = cell;
		return result;
	}

	public static void RestorePositionOnly(Thing thing, IntVec3 oldPos)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		PositionRef.Invoke(thing) = oldPos;
	}
}

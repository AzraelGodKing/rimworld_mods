using System.Collections.Generic;
using Verse;

namespace AsAboveSoBelow;

public sealed class ABPawnCooldown
{
	private const int MaxEntries = 512;

	private readonly Dictionary<int, int> nextAllowedTick = new Dictionary<int, int>();

	public bool Ready(Pawn pawn, int now)
	{
		int value;
		return !nextAllowedTick.TryGetValue(((Thing)pawn).thingIDNumber, out value) || now >= value;
	}

	public void ChargeUntil(Pawn pawn, int untilTick)
	{
		if (nextAllowedTick.Count > 512)
		{
			nextAllowedTick.Clear();
		}
		nextAllowedTick[((Thing)pawn).thingIDNumber] = untilTick;
	}
}

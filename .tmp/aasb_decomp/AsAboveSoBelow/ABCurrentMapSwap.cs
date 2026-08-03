using Verse;

namespace AsAboveSoBelow;

internal static class ABCurrentMapSwap
{
	public struct Token
	{
		internal sbyte index;
	}

	public static bool Swap(Map target, out Token token)
	{
		token = default(Token);
		Game game = Current.Game;
		if (game == null || target == null)
		{
			return false;
		}
		int num = Find.Maps.IndexOf(target);
		if (num < 0)
		{
			return false;
		}
		token.index = game.currentMapIndex;
		game.currentMapIndex = (sbyte)num;
		return true;
	}

	public static void Restore(Token token)
	{
		Game game = Current.Game;
		if (game != null)
		{
			game.currentMapIndex = token.index;
		}
	}
}

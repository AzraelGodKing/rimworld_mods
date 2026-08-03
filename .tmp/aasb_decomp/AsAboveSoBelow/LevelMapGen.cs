using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AsAboveSoBelow;

public static class LevelMapGen
{
	public class Context
	{
		public Map groundMap;

		public int levelToGenerate;
	}

	public static Context CurrentContext;

	public static Map GetOrGenerate(Map currentMap, int destLevel, MapGeneratorDef generatorDef, out bool generated)
	{
		generated = false;
		if (!ABGuard.On(ABGuard.LevelGen) || currentMap == null || generatorDef == null)
		{
			return null;
		}
		if (destLevel < -1 || destLevel > 1 || destLevel == 0)
		{
			ABLog.Dev("Rejected level generation request for level " + destLevel + " (cap is one up, one down).");
			return null;
		}
		try
		{
			Map val = ((currentMap.Level() == 0) ? currentMap : currentMap.GroundMap());
			LevelComp levelComp = val?.Levels();
			if (levelComp == null)
			{
				return null;
			}
			if (levelComp.MapByLevel.TryGetValue(destLevel, out var value) && value != null && !value.Disposed)
			{
				return value;
			}
			Map val2 = Generate(currentMap, val, destLevel, generatorDef);
			if (val2 == null)
			{
				return null;
			}
			if (!levelComp.MapByLevel.ContainsKey(0))
			{
				levelComp.AddLevel(0, val);
			}
			levelComp.AddLevel(destLevel, val2);
			LevelComp levelComp2 = val2.Levels();
			LevelComp levelComp3 = currentMap.Levels();
			if (destLevel > currentMap.Level())
			{
				levelComp2.lowerMap = currentMap;
				levelComp3.upperMap = val2;
			}
			else
			{
				levelComp2.upperMap = currentMap;
				levelComp3.lowerMap = val2;
			}
			generated = true;
			ABLog.Dev("Generated level " + destLevel + " map " + val2.uniqueID + " for ground map " + val.uniqueID + ".");
			return val2;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.LevelGen, e, "level map generation");
			return null;
		}
	}

	private static Map Generate(Map sourceMap, Map groundMap, int destLevel, MapGeneratorDef generatorDef)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		PocketMapParent val = (PocketMapParent)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.PocketMap);
		val.sourceMap = sourceMap;
		val.mapGenerator = generatorDef;
		((WorldObject)val).Tile = sourceMap.Tile;
		CurrentContext = new Context
		{
			groundMap = groundMap,
			levelToGenerate = destLevel
		};
		try
		{
			Map val2 = MapGenerator.GenerateMap(sourceMap.Size, (MapParent)(object)val, generatorDef, (IEnumerable<GenStepWithParams>)null, (Action<Map>)null, true, false);
			Find.World.pocketMaps.Add(val);
			ABApi.NotifyLevelCreated(val2);
			return val2;
		}
		finally
		{
			CurrentContext = null;
		}
	}
}

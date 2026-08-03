using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MultiFloors.Gravships;
using MultiFloors.Maps;
using RimWorld.Planet;
using Verse;

namespace MultiFloors.HarmonyPatches;

[HotSwappable]
[HarmonyPatch]
[HarmonyPatchCategory("MultiFloors.Gravship")]
public static class HarmonyPatch_OnGravshipLanding
{
	[HarmonyPrepare]
	public static bool Prepare()
	{
		return ModsConfig.OdysseyActive;
	}

	[HarmonyPatch(typeof(WorldComponent_GravshipController), "PlaceGravship")]
	[HarmonyPostfix]
	public static void SpawnUpperDecks(Gravship gravship, IntVec3 root, Map map)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		MF_GravshipManager component = Current.Game.GetComponent<MF_GravshipManager>();
		List<SubGravship> list = (from deck in component.SubGravships
			where deck.Engine.ParentEngine == gravship.Engine
			orderby deck.Level
			select deck).ToList();
		Map currentMap = map;
		MF_LevelMapComp component2 = map.GetComponent<MF_LevelMapComp>();
		UpperLevelSettingsDef mF_UpperLevelSettings = MiscDefOfs.MF_UpperLevelSettings;
		PlanetTile tile = map.Tile;
		MapGeneratorDef mapGeneratorFor = mF_UpperLevelSettings.GetMapGeneratorFor(((PlanetTile)(ref tile)).LayerDef);
		for (int num = 0; num < list.Count; num++)
		{
			SubGravship subGravship = list[num];
			bool generated;
			Map orGenerateLevelMap = LevelMapGenerator.GetOrGenerateLevelMap(mapGeneratorFor, map.Size, null, currentMap, subGravship.Level, out generated);
			subGravship.Rotation = gravship.Rotation;
			bool isTopDeck = num == list.Count - 1;
			MF_GravshipUtility.PlaceSubGravshipInMap(subGravship, root, orGenerateLevelMap, isTopDeck, out var spawned);
			MF_GravshipUtility.PostPlacementSetup(subGravship, orGenerateLevelMap, root, spawned);
			currentMap = orGenerateLevelMap;
		}
		Current.Game.GetComponent<MultiFloorManager>().RestoreTransportedElevatorNet();
		component2.SetupLevelCachesAndPathFinder();
		map.wealthWatcher.ForceRecount(false);
		component.RestoreTileWorkSettings(gravship.Engine, component2);
	}
}

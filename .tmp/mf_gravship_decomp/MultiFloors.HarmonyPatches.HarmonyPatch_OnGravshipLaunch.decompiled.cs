using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MultiFloors.Gravships;
using RimWorld;
using RimWorld.Planet;
using Unity.Collections;
using Verse;

namespace MultiFloors.HarmonyPatches;

[HotSwappable]
[HarmonyPatch]
[HarmonyPatchCategory("MultiFloors.Gravship")]
public static class HarmonyPatch_OnGravshipLaunch
{
	[HarmonyPrepare]
	public static bool Prepare()
	{
		return ModsConfig.OdysseyActive;
	}

	[HarmonyPatch(typeof(WorldComponent_GravshipController), "RemoveGravshipFromMap")]
	[HarmonyPrefix]
	public static void RemoveOtherDecks(Building_GravEngine engine)
	{
		Map map = ((Thing)engine).Map;
		if (map.UpperMap() != null)
		{
			GenerateSubGravships(map, engine);
			PreCopyMapState(map, engine);
		}
	}

	[HarmonyPatch(typeof(WorldComponent_GravshipController), "RemoveGravshipFromMap")]
	[HarmonyPostfix]
	public static void PostRemoveAllDecksFromMap()
	{
		PostCopyMapState();
	}

	private static void GenerateSubGravships(Map map, Building_GravEngine baseEngine)
	{
		Map val = map.UpperMap();
		Map curMap = map;
		List<Building_SubGravEngine> list = new List<Building_SubGravEngine>();
		while (val != null && val.listerThings.ThingsOfDef(MFGravshipDefOfs.MF_SubGravEngine).FirstOrDefault() is Building_SubGravEngine building_SubGravEngine && MF_GravshipUtility.HasEntranceInGroundValidSubstructure(baseEngine, curMap))
		{
			building_SubGravEngine.Parent = baseEngine;
			list.Add(building_SubGravEngine);
			curMap = val;
			val = val.UpperMap();
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Building_SubGravEngine building_SubGravEngine2 = list[num];
			val = ((Thing)building_SubGravEngine2).MapHeld;
			IntVec3[] substructures = building_SubGravEngine2.ValidSubstructure.ToArray();
			SubGravship subGravship = new SubGravship(building_SubGravEngine2);
			subGravship.TransferThingsToSubGravship();
			DirtyDeckCells(val, substructures);
			Current.Game.GetComponent<MF_GravshipManager>().RegisterSubGravship(subGravship);
		}
	}

	private static void DirtyDeckCells(Map curMap, IntVec3[] substructures)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		ulong num = MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Terrain) | MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Roofs) | MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Snow) | MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Sand) | MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Gas);
		for (int i = 0; i < substructures.Length; i++)
		{
			IntVec3 val = substructures[i];
			int num2 = ((CellIndices)(ref curMap.cellIndices)).CellToIndex(val);
			curMap.terrainGrid.RemoveGravshipTerrainUnsafe(val, num2);
			curMap.roofGrid.RemoveRoofUnsafe(num2);
			((Area)curMap.areaManager.BuildRoof)[num2] = false;
			((Area)curMap.areaManager.NoRoof)[num2] = false;
			curMap.glowGrid.DirtyCell(val);
			NativeArray<float> depthGrid_Unsafe = curMap.snowGrid.DepthGrid_Unsafe;
			depthGrid_Unsafe[num2] = 0f;
			NativeArray<float> depthGrid_Unsafe2 = curMap.sandGrid.DepthGrid_Unsafe;
			depthGrid_Unsafe2[num2] = 0f;
			curMap.gasGrid.ClearCellUnsafe(val);
			foreach (Designation item in curMap.designationManager.AllDesignationsAt(val))
			{
				curMap.designationManager.RemoveDesignation(item);
			}
			IntVec2 cell2D = ((IntVec3)(ref val)).ToIntVec2;
			foreach (FleckSystem system in curMap.flecks.Systems)
			{
				system.RemoveAllFlecks((Predicate<IFleck>)((IFleck fleck) => IntVec2Utility.ToIntVec2(fleck.GetPosition()) == cell2D));
			}
			curMap.mapDrawer.MapMeshDirty(val, num, true, false);
			curMap.pathing.RecalculatePerceivedPathCostAt(val);
		}
		curMap.roofGrid.Drawer.SetDirty();
	}

	private static void PreCopyMapState(Map map, Building_GravEngine baseEngine)
	{
		MF_LevelMapComp mF_LevelMapComp = map.LevelMapComp();
		if (mF_LevelMapComp?.TileWorkSettings != null)
		{
			Current.Game.GetComponent<MF_GravshipManager>()?.StoreTileWorkSettings(baseEngine, mF_LevelMapComp.TileWorkSettings);
		}
	}

	private static void PostCopyMapState()
	{
		Current.Game.GetComponent<MultiFloorManager>()?.TransportElevatorNet();
	}

	[HarmonyPatch(typeof(WorldComponent_GravshipController), "TakeoffEnded")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> CheckSubGravEngine(IEnumerable<CodeInstruction> instructions)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		MethodInfo travelTo = typeof(GravshipUtility).GetMethod("TravelTo");
		MethodInfo method = typeof(HarmonyPatch_OnGravshipLaunch).GetMethod("RevalidateLaunchSiteState");
		try
		{
			CodeMatcher val = new CodeMatcher(instructions, (ILGenerator)null);
			val.Start().MatchEndForward((CodeMatch[])(object)new CodeMatch[1]
			{
				new CodeMatch((Func<CodeInstruction, bool>)((CodeInstruction i) => CodeInstructionExtensions.Calls(i, travelTo)), (string)null)
			}).ThrowIfInvalid("[MultiFloors] Transpiler on WorldComponent_GravshipController.TakeoffEnded() no match.")
				.Advance(1)
				.Insert((CodeInstruction[])(object)new CodeInstruction[3]
				{
					new CodeInstruction(OpCodes.Ldarg_0, (object)null),
					new CodeInstruction(OpCodes.Ldfld, (object)AccessTools.Field(typeof(WorldComponent_GravshipController), "map")),
					new CodeInstruction(OpCodes.Call, (object)method)
				});
			return val.InstructionEnumeration();
		}
		catch (Exception arg)
		{
			Log.Error("[MultiFloors] Transpiler on " + $"WorldComponent_GravshipController.TakeoffEnded() failed. {arg}");
			return instructions;
		}
	}

	public static void RevalidateLaunchSiteState(Map map)
	{
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		MapParent parent = map.Parent;
		if (parent == null || ((WorldObject)parent).Destroyed)
		{
			Current.Game.GetComponent<MultiFloorManager>()?.OnMapRemoved();
			return;
		}
		MF_LevelMapComp controller = map.LevelMapComp();
		if (map.UpperMap() != null)
		{
			bool flag = false;
			foreach (StairEntrance item in map.listerBuildings.AllBuildingsColonistOfClass<StairEntrance>())
			{
				if (item.ConnectedMap == map.UpperMap())
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (StairEntrance item2 in map.listerBuildings.AllBuildingsNonColonistOfClass<StairEntrance>())
				{
					if (item2.ConnectedMap == map.UpperMap())
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				Find.WindowStack.Add((Window)(object)new Dialog_LargeConfirmBox(TaggedString.op_Implicit(Translator.Translate("MF_DeleteDanglingUpperLevelsOnGravshipLaunch")), DeleteDanglingUpperLevels));
			}
		}
		controller.OnMapStateChanged();
		void DeleteDanglingUpperLevels()
		{
			PocketMapUtility.DestroyPocketMap(map.UpperMap());
			map.UpperMap() = null;
			controller.OnMapRemoved();
		}
	}
}

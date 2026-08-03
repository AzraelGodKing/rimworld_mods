using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

internal static class BelowSelection
{
	private const float ClickRadius = 0.75f;

	private static readonly FieldRef<Selector, List<object>> SelectedRef = AccessTools.FieldRefAccess<Selector, List<object>>("selected");

	private static readonly MethodInfo PlaySelectionSound = AccessTools.Method(typeof(Selector), "PlaySelectionSoundFor", (Type[])null, (Type[])null);

	private static readonly List<Thing> hitBuffer = new List<Thing>();

	private static readonly Dictionary<Thing, float> hitDist = new Dictionary<Thing, float>();

	internal static bool TryGetBelowView(out Map sky, out Map lower)
	{
		sky = null;
		lower = null;
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.selectBelowInPlace || !settings.showLiveBelow)
		{
			return false;
		}
		Map currentMap = Find.CurrentMap;
		if (currentMap == null)
		{
			return false;
		}
		LevelComp levelComp = currentMap.Levels();
		if (levelComp == null || levelComp.level <= 0)
		{
			return false;
		}
		Map lowerMap = levelComp.lowerMap;
		if (lowerMap == null || lowerMap.Disposed)
		{
			return false;
		}
		sky = currentMap;
		lower = lowerMap;
		return true;
	}

	internal static bool VisibleFromAbove(Thing t, Map sky, Map lower)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (t == null || !t.Spawned || t.MapHeld != lower)
		{
			return false;
		}
		return CellVisibleFromAbove(t.Position, sky, lower);
	}

	internal static bool CellVisibleFromAbove(IntVec3 pos, Map sky, Map lower)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (!GenGrid.InBounds(pos, lower) || lower.roofGrid.Roofed(pos))
		{
			return false;
		}
		if (!GenGrid.InBounds(pos, sky) || sky.terrainGrid.TerrainAt(pos) != ABDefOf.AB_OpenAir)
		{
			return false;
		}
		return !lower.fogGrid.IsFogged(pos);
	}

	internal static bool IsBelowSelected(Thing t, out Map sky, out Map lower)
	{
		sky = Find.CurrentMap;
		lower = null;
		if (sky == null || t == null)
		{
			return false;
		}
		LevelComp levelComp = sky.Levels();
		if (levelComp == null || levelComp.level <= 0)
		{
			return false;
		}
		lower = levelComp.lowerMap;
		return lower != null && !lower.Disposed && t.MapHeld == lower;
	}

	internal static List<Thing> SelectablesUnderMouse(Map sky, Map lower, Vector3 clickPos)
	{
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		hitBuffer.Clear();
		hitDist.Clear();
		IReadOnlyList<Pawn> allPawnsSpawned = lower.mapPawns.AllPawnsSpawned;
		for (int i = 0; i < allPawnsSpawned.Count; i++)
		{
			Pawn val = allPawnsSpawned[i];
			if (val != null && ((Thing)val).def.selectable && !InvisibilityUtility.IsHiddenFromPlayer(val) && VisibleFromAbove((Thing)(object)val, sky, lower))
			{
				Vector3 val2 = LevelRenderer.ShiftedBelowDrawPos(((Thing)val).DrawPos);
				float num = GenGeo.MagnitudeHorizontal(val2 - clickPos);
				if (num < 0.75f)
				{
					hitBuffer.Add((Thing)(object)val);
					hitDist[(Thing)(object)val] = num;
				}
			}
		}
		hitBuffer.Sort((Thing a, Thing b) => hitDist[a].CompareTo(hitDist[b]));
		AddBelowThingsAtCursor(sky, lower, clickPos);
		return hitBuffer;
	}

	private static void AddBelowThingsAtCursor(Map sky, Map lower, Vector3 clickPos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		IntVec3 val = IntVec3Utility.ToIntVec3(LevelRenderer.ScreenToBelowPos(clickPos));
		if (!GenGrid.InBounds(val, lower) || !CellVisibleFromAbove(val, sky, lower))
		{
			return;
		}
		List<Thing> list = lower.thingGrid.ThingsListAtFast(val);
		for (int i = 0; i < list.Count; i++)
		{
			Thing val2 = list[i];
			if (IsBelowSelectableThing(val2) && !hitBuffer.Contains(val2))
			{
				hitBuffer.Add(val2);
			}
		}
	}

	private static bool IsBelowSelectableThing(Thing t)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Invalid comparison between Unknown and I4
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		if (t == null || !t.Spawned || !t.def.selectable)
		{
			return false;
		}
		ThingCategory category = t.def.category;
		return (int)category == 2 || (int)category == 3;
	}

	internal static bool SkyBlocksSelection(Map sky, Vector3 clickPos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		TargetingParameters val = new TargetingParameters
		{
			mustBeSelectable = true,
			canTargetPawns = true,
			canTargetBuildings = true,
			canTargetItems = true,
			mapObjectTargetsMustBeAutoAttackable = false
		};
		if (GenUI.ThingsUnderMouse(clickPos, 1f, val, (ITargetingSource)null).Count > 0)
		{
			return true;
		}
		IntVec3 val2 = UI.MouseCell();
		if (sky.zoneManager.ZoneAt(val2) != null)
		{
			return true;
		}
		return sky.planManager.PlanAt(val2) != null;
	}

	private static void AddInPlace(Selector selector, Thing thing)
	{
		DesignatorManager designatorManager = Find.DesignatorManager;
		if (designatorManager != null)
		{
			designatorManager.Deselect();
		}
		List<object> list = SelectedRef.Invoke(selector);
		if (list.Count < 200 && !list.Contains(thing))
		{
			try
			{
				PlaySelectionSound?.Invoke(selector, new object[1] { thing });
			}
			catch
			{
			}
			list.Add(thing);
			thing.Notify_ThingSelected();
			SelectionDrawer.Notify_Selected((object)thing);
		}
	}

	private static void DropOtherMapSelection(Selector selector, Map map)
	{
		List<object> list = SelectedRef.Invoke(selector);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Map val = null;
			object obj = list[num];
			Thing val2 = (Thing)((obj is Thing) ? obj : null);
			if (val2 != null)
			{
				val = val2.MapHeld;
			}
			else
			{
				object obj2 = list[num];
				Zone val3 = (Zone)((obj2 is Zone) ? obj2 : null);
				if (val3 != null)
				{
					val = val3.Map;
				}
				else
				{
					object obj3 = list[num];
					Plan val4 = (Plan)((obj3 is Plan) ? obj3 : null);
					if (val4 != null)
					{
						val = val4.Map;
					}
				}
			}
			if (val != map)
			{
				selector.Deselect(list[num]);
			}
		}
	}

	internal static void HandleBelowClick(Selector selector, List<Thing> hits)
	{
		bool shiftIsHeld = Selector.ShiftIsHeld;
		Thing val = hits[0];
		if (!shiftIsHeld)
		{
			for (int i = 0; i < hits.Count; i++)
			{
				if (selector.IsSelected((object)hits[i]))
				{
					val = hits[(i + 1) % hits.Count];
					break;
				}
			}
		}
		if (shiftIsHeld)
		{
			if (selector.IsSelected((object)val))
			{
				selector.Deselect((object)val);
				return;
			}
			DropOtherMapSelection(selector, val.MapHeld);
			AddInPlace(selector, val);
		}
		else
		{
			selector.ClearSelection();
			AddInPlace(selector, val);
		}
	}
}

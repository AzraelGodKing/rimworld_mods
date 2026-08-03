using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public static class LevelCamera
{
	public static bool LevelLocked;

	private static readonly FieldRef<CameraDriver, Vector3> RootPosRef = AccessTools.FieldRefAccess<CameraDriver, Vector3>("rootPos");

	private static readonly FieldRef<Selector, List<object>> SelectedRef = AccessTools.FieldRefAccess<Selector, List<object>>("selected");

	public static void ToggleLevelLock()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		LevelLocked = !LevelLocked;
		Messages.Message(TaggedString.op_Implicit(Translator.Translate(LevelLocked ? "AB_CameraLockOn" : "AB_CameraLockOff")), MessageTypeDefOf.SilentInput, false);
	}

	public static void JumpPreservingView(Map target)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		if (target != null && !target.Disposed)
		{
			CameraDriver driver = Find.CameraDriver;
			Vector3 root = RootPosRef.Invoke(driver);
			float zoom = driver.ZoomRootSize;
			if (target.rememberedCameraPos != null)
			{
				target.rememberedCameraPos.rootPos = root;
				target.rememberedCameraPos.rootSize = zoom;
			}
			IntVec3 cell = IntVec3Utility.ToIntVec3(root);
			cell.x = Mathf.Clamp(cell.x, 0, target.Size.x - 1);
			cell.z = Mathf.Clamp(cell.z, 0, target.Size.z - 1);
			List<object> saved = new List<object>(Find.Selector.SelectedObjects);
			ABArchitectPreserve.Around(delegate
			{
				//IL_0002: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Unknown result type (might be due to invalid IL or missing references)
				CameraJumper.TryJump(cell, target, (MovementMode)0);
				driver.SetRootPosAndSize(root, zoom);
			});
			RestoreSelection(saved);
		}
	}

	private static void RestoreSelection(List<object> saved)
	{
		if (saved == null || saved.Count == 0 || !ABGuard.On(ABGuard.Ui))
		{
			return;
		}
		try
		{
			Selector selector = Find.Selector;
			List<object> list = SelectedRef.Invoke(selector);
			if (list.Count > 0)
			{
				return;
			}
			for (int i = 0; i < saved.Count; i++)
			{
				object obj = saved[i];
				Thing val = (Thing)((obj is Thing) ? obj : null);
				if (val != null && !val.Destroyed && val.Spawned && !list.Contains(val))
				{
					list.Add(val);
					val.Notify_ThingSelected();
					SelectionDrawer.Notify_Selected((object)val);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "level switch selection preserve");
		}
	}

	public static void FollowPawn(Pawn pawn)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		if (!LevelLocked && pawn != null && ((Thing)pawn).Spawned && ((Thing)pawn).Map != null && !((Thing)pawn).Map.Disposed)
		{
			CameraDriver driver = Find.CameraDriver;
			float zoom = driver.ZoomRootSize;
			Map target = ((Thing)pawn).Map;
			Vector3 root = new Vector3(((Thing)pawn).DrawPos.x, 0f, ((Thing)pawn).DrawPos.z);
			if (target.rememberedCameraPos != null)
			{
				target.rememberedCameraPos.rootPos = root;
				target.rememberedCameraPos.rootSize = zoom;
			}
			ABArchitectPreserve.Around(delegate
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_0020: Unknown result type (might be due to invalid IL or missing references)
				CameraJumper.TryJump(((Thing)pawn).Position, target, (MovementMode)0);
				driver.SetRootPosAndSize(root, zoom);
				Find.Selector.ClearSelection();
				Find.Selector.Select((object)pawn, true, true);
			});
		}
	}
}

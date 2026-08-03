using System;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public class ABGameComp : GameComponent
{
	public ABGameComp(Game game)
	{
	}

	public override void FinalizeInit()
	{
		((GameComponent)this).FinalizeInit();
		ABGuard.Reset();
		CrossLevelCombat.PendingTargets.Clear();
		CrossLevelTurret.ClearAll();
		CrossLevelCombatUI.ActiveShooters.Clear();
		CrossLevelAnimals.ClearAll();
		ABRitualAttendance.ClearAll();
	}

	public override void GameComponentTick()
	{
		((GameComponent)this).GameComponentTick();
		ABRitualAttendance.Tick();
	}

	public override void GameComponentOnGUI()
	{
		if (!ABGuard.On(ABGuard.Ui) || Find.CurrentMap == null || !WorldRendererUtility.DrawingMap)
		{
			return;
		}
		try
		{
			ABSettings settings = ABMod.Settings;
			if (settings != null && settings.cameraLockKeybind && ABDefOf.AB_CameraLevelLock != null && ABDefOf.AB_CameraLevelLock.KeyDownEvent)
			{
				LevelCamera.ToggleLevelLock();
				Event.current.Use();
			}
			else if (ABDefOf.AB_ViewLevelUp.KeyDownEvent)
			{
				Map val = Find.CurrentMap.UpperMap();
				if (val != null)
				{
					LevelCamera.JumpPreservingView(val);
				}
			}
			else if (ABDefOf.AB_ViewLevelDown.KeyDownEvent)
			{
				Map val2 = Find.CurrentMap.LowerMap();
				if (val2 != null)
				{
					LevelCamera.JumpPreservingView(val2);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "level view hotkeys");
		}
	}
}

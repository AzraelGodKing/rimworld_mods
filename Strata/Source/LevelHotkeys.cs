using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Page Up / Page Down (configurable in mod settings) flip the camera one
    // level up or down, keeping the same relative position so the view stays
    // over the same part of the base.
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    public static class Patch_LevelHotkeys
    {
        public static void Postfix()
        {
            if (Event.current.type != EventType.KeyDown
                || Find.CurrentMap == null
                || !StrataMapViewUtility.IsColonyMapView()
                || Find.WindowStack.WindowsPreventCameraMotion)
            {
                return;
            }
            KeyCode pressed = Event.current.keyCode;
            KeyCode down = StrataMod.Settings?.viewLevelDownKey ?? KeyCode.PageDown;
            KeyCode up = StrataMod.Settings?.viewLevelUpKey ?? KeyCode.PageUp;
            if (pressed == down && pressed != KeyCode.None)
            {
                JumpOneLevel(goDown: true);
                Event.current.Use();
            }
            else if (pressed == up && pressed != KeyCode.None)
            {
                JumpOneLevel(goDown: false);
                Event.current.Use();
            }
        }

        private static void JumpOneLevel(bool goDown)
        {
            Map current = Find.CurrentMap;
            Map target = null;
            if (goDown)
            {
                // Underground shaft first; from an upper floor, step toward surface.
                foreach (Thing thing in current.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (thing is Building_StairsBuildUp)
                    {
                        continue;
                    }
                    if (thing is Building_StairsDown stairs && stairs.Spawned && stairs.PocketMapExists)
                    {
                        target = stairs.PocketMap;
                        break;
                    }
                }
                if (target == null && StrataMapUtility.IsUpperLevel(current))
                {
                    target = (current.Parent as PocketMapParent)?.sourceMap;
                }
            }
            else
            {
                // Tower stairwell to A1+; otherwise climb the pocket parent (B→surface).
                foreach (Thing thing in current.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (thing is Building_StairsBuildUp up && up.Spawned && up.PocketMapExists)
                    {
                        target = up.PocketMap;
                        break;
                    }
                }
                if (target == null)
                {
                    target = (current.Parent as PocketMapParent)?.sourceMap;
                }
            }
            if (target == null || !Find.Maps.Contains(target))
            {
                return;
            }
            IntVec3 look = StrataMapUtility.ProportionalCell(Find.CameraDriver.MapPosition, current, target);
            CameraJumper.TryJump(new GlobalTargetInfo(look, target), CameraJumper.MovementMode.Cut);
        }
    }
}

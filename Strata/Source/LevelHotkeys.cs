using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    [DefOf]
    public static class StrataKeyDefOf
    {
        public static KeyBindingDef Strata_ViewLevelUp;

        public static KeyBindingDef Strata_ViewLevelDown;

        static StrataKeyDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataKeyDefOf));
        }
    }

    // Page Up / Page Down flip the camera one level up or down, keeping the
    // same relative position so the view stays over the same part of the base.
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    public static class Patch_LevelHotkeys
    {
        public static void Postfix()
        {
            if (Find.CurrentMap == null
                || Find.World?.renderer == null
                || Find.World.renderer.wantedMode != WorldRenderMode.None
                || Find.WindowStack.WindowsPreventCameraMotion)
            {
                return;
            }
            if (StrataKeyDefOf.Strata_ViewLevelDown.KeyDownEvent)
            {
                JumpOneLevel(down: true);
                Event.current.Use();
            }
            else if (StrataKeyDefOf.Strata_ViewLevelUp.KeyDownEvent)
            {
                JumpOneLevel(down: false);
                Event.current.Use();
            }
        }

        private static void JumpOneLevel(bool down)
        {
            Map current = Find.CurrentMap;
            Map target = null;
            if (down)
            {
                foreach (Thing thing in current.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (thing is Building_StairsDown stairs && stairs.Spawned && stairs.PocketMapExists)
                    {
                        target = stairs.PocketMap;
                        break;
                    }
                }
            }
            else
            {
                target = (current.Parent as PocketMapParent)?.sourceMap;
            }
            if (target == null || !Find.Maps.Contains(target))
            {
                return;
            }
            IntVec3 look = ProportionalCell(Find.CameraDriver.MapPosition, current, target);
            CameraJumper.TryJump(new GlobalTargetInfo(look, target), CameraJumper.MovementMode.Cut);
        }

        private static IntVec3 ProportionalCell(IntVec3 pos, Map from, Map to)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt((float)pos.x / from.Size.x * to.Size.x), 0, to.Size.x - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt((float)pos.z / from.Size.z * to.Size.z), 0, to.Size.z - 1);
            return new IntVec3(x, 0, z);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Colonists on a linked floor stay selected while the player looks at
    // another floor, so they can be ordered from there (the float menu and
    // CrossFloorDraftedMove handle the trip). Vanilla clears the selection on
    // every map switch and jumps the camera when a portrait is clicked.
    public static class CrossFloorSelection
    {
        private static readonly AccessTools.FieldRef<Selector, List<object>> SelectedRef =
            AccessTools.FieldRefAccess<Selector, List<object>>("selected");

        private static readonly MethodInfo PlaySelectionSound =
            AccessTools.Method(typeof(Selector), "PlaySelectionSoundFor");

        public static bool Enabled =>
            StrataMod.Settings == null || StrataMod.Settings.crossLevelSelectionEnabled;

        // A colony pawn the player can order, on a floor linked to 'viewed'.
        public static bool CanStaySelected(Pawn pawn, Map viewed)
        {
            if (pawn == null || viewed == null || !pawn.Spawned || pawn.Map == null)
            {
                return false;
            }
            if (pawn.Map == viewed)
            {
                return true;
            }
            if (!pawn.CanTakeOrder || !StrataPawnUtility.CanUseLevelPortals(pawn))
            {
                return false;
            }
            return ColonyBedUtility.MapsLinked(pawn.Map, viewed);
        }

        internal static List<Pawn> LinkedPawnsSelected(Map viewed, Map excludeMap)
        {
            List<Pawn> keep = null;
            List<object> selected = SelectedRef(Find.Selector);
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] is Pawn pawn
                    && pawn.Map != excludeMap
                    && CanStaySelected(pawn, viewed))
                {
                    (keep ??= new List<Pawn>()).Add(pawn);
                }
            }
            return keep;
        }

        internal static void AddBack(List<Pawn> pawns, bool notifyDrawer)
        {
            if (pawns == null)
            {
                return;
            }
            List<object> selected = SelectedRef(Find.Selector);
            for (int i = 0; i < pawns.Count && selected.Count < 200; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Destroyed || !pawn.Spawned || selected.Contains(pawn))
                {
                    continue;
                }
                selected.Add(pawn);
                if (notifyDrawer)
                {
                    SelectionDrawer.Notify_Selected(pawn);
                }
            }
        }

        // Select a pawn on another linked floor without moving the camera.
        // Keeps other orderable colonists on linked floors (shift-select builds
        // a group across floors); drops anything else from other maps.
        internal static bool TrySelectInPlace(Selector selector, Pawn pawn, bool playSound, bool forceDesignatorDeselect)
        {
            Map viewed = Find.CurrentMap;
            if (!Enabled || viewed == null || WorldRendererUtility.WorldSelected
                || !pawn.Spawned || pawn.Map == viewed || !CanStaySelected(pawn, viewed))
            {
                return false;
            }

            if (forceDesignatorDeselect)
            {
                Find.DesignatorManager.Deselect();
            }

            List<object> selected = SelectedRef(selector);
            if (selected.Contains(pawn))
            {
                return true;
            }
            for (int i = selected.Count - 1; i >= 0; i--)
            {
                if (selected[i] is Pawn other && CanStaySelected(other, viewed))
                {
                    continue;
                }
                // Buildings, items or zones mixed with colonists on other floors
                // make no sense as one selection; vanilla drops them too.
                selector.Deselect(selected[i]);
            }
            if (selected.Count >= 200)
            {
                return true;
            }

            if (playSound)
            {
                try
                {
                    PlaySelectionSound?.Invoke(selector, new object[] { pawn });
                }
                catch
                {
                    // Sound is cosmetic.
                }
            }
            selected.Add(pawn);
            pawn.Notify_ThingSelected();
            SelectionDrawer.Notify_Selected(pawn);
            return true;
        }

        // Selected things the camera can't see: on another floor and not shown
        // through a see-below gap. Pulled out while overlays draw so brackets and
        // paths don't appear at their coordinates on this floor.
        internal static List<KeyValuePair<int, object>> HideOffView()
        {
            Map viewed = Find.CurrentMap;
            if (viewed == null)
            {
                return null;
            }
            List<object> selected = SelectedRef(Find.Selector);
            List<KeyValuePair<int, object>> hidden = null;
            for (int i = selected.Count - 1; i >= 0; i--)
            {
                if (!(selected[i] is Thing thing) || thing.MapHeld == viewed || thing.MapHeld == null)
                {
                    continue;
                }
                if (StrataBelowSelection.IsBelowSelected(thing, out Map sky, out Map lower)
                    && StrataBelowSelection.VisibleFromAbove(thing, sky, lower))
                {
                    continue;
                }
                (hidden ??= new List<KeyValuePair<int, object>>()).Add(new KeyValuePair<int, object>(i, thing));
                selected.RemoveAt(i);
            }
            return hidden;
        }

        internal static void Unhide(List<KeyValuePair<int, object>> hidden)
        {
            if (hidden == null)
            {
                return;
            }
            List<object> selected = SelectedRef(Find.Selector);
            // Removed back to front, so re-insert front to back.
            for (int i = hidden.Count - 1; i >= 0; i--)
            {
                int at = Math.Min(hidden[i].Key, selected.Count);
                if (!selected.Contains(hidden[i].Value))
                {
                    selected.Insert(at, hidden[i].Value);
                }
            }
        }
    }

    [HarmonyPatch(typeof(MapInterface), nameof(MapInterface.Notify_SwitchedMap))]
    public static class Patch_SwitchedMap_KeepLinkedSelection
    {
        // Runs after the view has changed: Find.CurrentMap is the new floor.
        [HarmonyPriority(Priority.High)]
        public static void Prefix(out List<Pawn> __state)
        {
            __state = null;
            try
            {
                if (CrossFloorSelection.Enabled && Find.Selector.NumSelected > 0
                    && !WorldRendererUtility.WorldSelected)
                {
                    __state = CrossFloorSelection.LinkedPawnsSelected(Find.CurrentMap, excludeMap: null);
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Keeping selection across floors failed: " + e.Message, 0x5B71040);
            }
        }

        public static void Postfix(List<Pawn> __state)
        {
            try
            {
                CrossFloorSelection.AddBack(__state, notifyDrawer: true);
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Restoring selection across floors failed: " + e.Message, 0x5B71041);
            }
        }
    }

    // Single-click on a portrait (or any plain Select) of a colonist on a linked
    // floor selects them where the camera is. Double-click still jumps.
    [HarmonyPatch(typeof(Selector), "SelectInternal")]
    public static class Patch_Selector_SelectLinkedInPlace
    {
        public static bool Prefix(Selector __instance, object obj, bool playSound, bool forceDesignatorDeselect,
            out List<Pawn> __state)
        {
            __state = null;
            if (!(obj is Pawn pawn) || !CrossFloorSelection.Enabled)
            {
                return true;
            }
            try
            {
                if (CrossFloorSelection.TrySelectInPlace(__instance, pawn, playSound, forceDesignatorDeselect))
                {
                    return false;
                }
                // Shift-adding a colonist on this floor: vanilla drops everyone on
                // other maps. Remember the linked ones to put back.
                if (pawn.Map != null && pawn.Map == Find.CurrentMap)
                {
                    __state = CrossFloorSelection.LinkedPawnsSelected(pawn.Map, excludeMap: pawn.Map);
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Selecting across floors failed: " + e.Message, 0x5B71042);
            }
            return true;
        }

        public static void Postfix(object obj, List<Pawn> __state)
        {
            if (__state == null || !(obj is Pawn pawn) || !Find.Selector.IsSelected(pawn)
                || pawn.Map != Find.CurrentMap)
            {
                return;
            }
            CrossFloorSelection.AddBack(__state, notifyDrawer: false);
        }
    }

    [HarmonyPatch(typeof(SelectionDrawer), nameof(SelectionDrawer.DrawSelectionOverlays))]
    public static class Patch_SelectionOverlays_HideOtherFloors
    {
        [HarmonyPriority(Priority.High)]
        public static void Prefix(out List<KeyValuePair<int, object>> __state)
        {
            __state = null;
            try
            {
                __state = CrossFloorSelection.HideOffView();
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Hiding off-floor selection overlays failed: " + e.Message, 0x5B71043);
            }
        }

        public static void Finalizer(List<KeyValuePair<int, object>> __state)
        {
            CrossFloorSelection.Unhide(__state);
        }
    }
}

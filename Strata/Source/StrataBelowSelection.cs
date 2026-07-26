using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Click-select things on the map below through Strata open-sky / roof-deck gaps.
    public static class StrataBelowSelection
    {
        private const float ClickRadius = 0.75f;
        private const float DoubleClickSeconds = 0.45f;

        private static readonly AccessTools.FieldRef<Selector, List<object>> SelectedRef =
            AccessTools.FieldRefAccess<Selector, List<object>>("selected");

        private static readonly MethodInfo PlaySelectionSound =
            AccessTools.Method(typeof(Selector), "PlaySelectionSoundFor");

        private static readonly List<Thing> hitBuffer = new List<Thing>();
        private static readonly Dictionary<Thing, float> hitDist = new Dictionary<Thing, float>();

        private static float lastClickRealTime = -999f;
        private static ThingDef lastClickDef;
        private static ThingDef lastClickStuff;
        private static int lastClickThingId = -1;

        public static bool TryGetBelowView(out Map sky, out Map lower)
        {
            sky = null;
            lower = null;
            if (!StrataBelowRenderer.Enabled) return false;

            Map current = Find.CurrentMap;
            if (current == null || !StrataMapUtility.IsUpperLevel(current)) return false;

            lower = UpperDeckUtility.SourceMapFor(current);
            if (lower == null || lower.Disposed) return false;

            sky = current;
            return true;
        }

        public static bool VisibleFromAbove(Thing t, Map sky, Map lower)
        {
            if (t == null || !t.Spawned || t.MapHeld != lower) return false;
            return CellVisibleFromAbove(t.Position, sky, lower);
        }

        public static bool CellVisibleFromAbove(IntVec3 pos, Map sky, Map lower)
        {
            if (!pos.InBounds(lower) || lower.roofGrid.Roofed(pos) || lower.fogGrid.IsFogged(pos))
            {
                return false;
            }

            IntVec3 skyCell = StrataBelowRenderer.LowerToSkyCell(lower, sky, pos);
            if (!skyCell.InBounds(sky)) return false;
            return UpperDeckUtility.IsSeeThroughGap(sky.terrainGrid.TerrainAt(skyCell));
        }

        public static bool IsBelowSelected(Thing t, out Map sky, out Map lower)
        {
            sky = Find.CurrentMap;
            lower = null;
            if (sky == null || t == null) return false;
            if (!StrataMapUtility.IsUpperLevel(sky)) return false;
            lower = UpperDeckUtility.SourceMapFor(sky);
            return lower != null && !lower.Disposed && t.MapHeld == lower;
        }

        public static List<Thing> SelectablesUnderMouse(Map sky, Map lower, Vector3 clickPos)
        {
            hitBuffer.Clear();
            hitDist.Clear();

            IReadOnlyList<Pawn> pawns = lower.mapPawns?.AllPawnsSpawned;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn == null || !pawn.def.selectable) continue;
                    if (pawn.IsHiddenFromPlayer()) continue;
                    if (!VisibleFromAbove(pawn, sky, lower)) continue;

                    float dist = (pawn.DrawPos - clickPos).MagnitudeHorizontal();
                    if (dist < ClickRadius)
                    {
                        hitBuffer.Add(pawn);
                        hitDist[pawn] = dist;
                    }
                }
            }

            hitBuffer.Sort((a, b) => hitDist[a].CompareTo(hitDist[b]));
            AddBelowThingsAtCursor(sky, lower, clickPos);
            return hitBuffer;
        }

        private static void AddBelowThingsAtCursor(Map sky, Map lower, Vector3 clickPos)
        {
            IntVec3 skyCell = clickPos.ToIntVec3();
            if (!skyCell.InBounds(sky)) return;

            // Exact cell + neighbors so floor items are easy to click.
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    IntVec3 sc = skyCell + new IntVec3(dx, 0, dz);
                    if (!sc.InBounds(sky)) continue;
                    if (!UpperDeckUtility.IsSeeThroughGap(sky.terrainGrid.TerrainAt(sc))) continue;

                    IntVec3 lowerCell = StrataBelowRenderer.SkyToLowerCell(sky, lower, sc);
                    if (!CellVisibleFromAbove(lowerCell, sky, lower)) continue;

                    List<Thing> list = lower.thingGrid.ThingsListAtFast(lowerCell);
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing t = list[i];
                        if (!IsBelowSelectableThing(t) || hitBuffer.Contains(t)) continue;

                        float dist = (t.DrawPos - clickPos).MagnitudeHorizontal();
                        if (dx == 0 && dz == 0)
                        {
                            hitBuffer.Add(t);
                        }
                        else if (dist < ClickRadius)
                        {
                            hitBuffer.Add(t);
                        }
                    }
                }
            }
        }

        private static bool IsBelowSelectableThing(Thing t)
        {
            if (t == null || !t.Spawned || !t.def.selectable) return false;
            ThingCategory cat = t.def.category;
            return cat == ThingCategory.Item || cat == ThingCategory.Building;
        }

        public static bool SkyBlocksSelection(Map sky, Vector3 clickPos)
        {
            var parms = new TargetingParameters
            {
                mustBeSelectable = true,
                canTargetPawns = true,
                canTargetBuildings = true,
                canTargetItems = true,
                mapObjectTargetsMustBeAutoAttackable = false
            };
            if (GenUI.ThingsUnderMouse(clickPos, 1f, parms).Count > 0)
            {
                return true;
            }

            IntVec3 cell = UI.MouseCell();
            if (sky.zoneManager.ZoneAt(cell) != null) return true;
            return sky.planManager?.PlanAt(cell) != null;
        }

        public static void HandleBelowClick(Selector selector, List<Thing> hits)
        {
            bool shift = Selector.ShiftIsHeld;
            Thing pick = hits[0];
            if (!shift)
            {
                for (int i = 0; i < hits.Count; i++)
                {
                    if (selector.IsSelected(hits[i]))
                    {
                        pick = hits[(i + 1) % hits.Count];
                        break;
                    }
                }
            }

            float now = Time.unscaledTime;
            bool sameKind = pick.def == lastClickDef && pick.Stuff == lastClickStuff;
            bool doubleClick = !shift
                && sameKind
                && now - lastClickRealTime <= DoubleClickSeconds;

            lastClickRealTime = now;
            lastClickDef = pick.def;
            lastClickStuff = pick.Stuff;
            lastClickThingId = pick.thingIDNumber;

            if (doubleClick)
            {
                SelectAllSimilarVisible(selector, pick, additive: false);
                return;
            }

            if (shift)
            {
                if (selector.IsSelected(pick))
                {
                    selector.Deselect(pick);
                    return;
                }
                DropOtherMapSelection(selector, pick.MapHeld);
                AddInPlace(selector, pick);
            }
            else
            {
                selector.ClearSelection();
                AddInPlace(selector, pick);
            }
        }

        private static void SelectAllSimilarVisible(Selector selector, Thing sample, bool additive)
        {
            if (!TryGetBelowView(out Map sky, out Map lower)) return;
            if (!additive)
            {
                selector.ClearSelection();
            }
            else
            {
                DropOtherMapSelection(selector, sample.MapHeld);
            }

            CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(1).ClipInsideMap(lower);
            List<Thing> ofDef = lower.listerThings.ThingsOfDef(sample.def);
            int added = 0;
            for (int i = 0; i < ofDef.Count && added < 200; i++)
            {
                Thing t = ofDef[i];
                if (t == null || !t.Spawned || !t.def.selectable) continue;
                if (sample.Stuff != null && t.Stuff != sample.Stuff) continue;
                if (!view.Contains(t.Position)) continue;
                if (!VisibleFromAbove(t, sky, lower)) continue;
                AddInPlace(selector, t);
                added++;
            }

            // Also match pawns of the same kind when double-clicking a pawn.
            if (sample is Pawn samplePawn && samplePawn.def != null)
            {
                IReadOnlyList<Pawn> pawns = lower.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count && added < 200; i++)
                {
                    Pawn p = pawns[i];
                    if (p == null || p.def != sample.def || !p.def.selectable) continue;
                    if (!view.Contains(p.Position)) continue;
                    if (!VisibleFromAbove(p, sky, lower)) continue;
                    if (selector.IsSelected(p)) continue;
                    AddInPlace(selector, p);
                    added++;
                }
            }
        }

        private static void AddInPlace(Selector selector, Thing thing)
        {
            Find.DesignatorManager?.Deselect();
            List<object> list = SelectedRef(selector);
            if (list.Count >= 200 || list.Contains(thing)) return;

            try
            {
                PlaySelectionSound?.Invoke(selector, new object[] { thing });
            }
            catch
            {
                // Sound helper is best-effort.
            }

            list.Add(thing);
            thing.Notify_ThingSelected();
            SelectionDrawer.Notify_Selected(thing);
        }

        private static void DropOtherMapSelection(Selector selector, Map keepMap)
        {
            List<object> list = SelectedRef(selector);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Map other = null;
                if (list[i] is Thing t) other = t.MapHeld;
                else if (list[i] is Zone z) other = z.Map;
                else if (list[i] is Plan p) other = p.Map;

                if (other != keepMap)
                {
                    selector.Deselect(list[i]);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Selector), "SelectUnderMouse")]
    public static class Patch_Selector_SelectUnderMouse_SeeBelow
    {
        public static bool Prefix(Selector __instance)
        {
            try
            {
                if (!StrataBelowSelection.TryGetBelowView(out Map sky, out Map lower))
                {
                    return true;
                }
                if (!UI.MouseCell().InBounds(sky))
                {
                    return true;
                }

                Vector3 clickPos = UI.MouseMapPosition();
                if (StrataBelowSelection.SkyBlocksSelection(sky, clickPos))
                {
                    return true;
                }

                List<Thing> hits = StrataBelowSelection.SelectablesUnderMouse(sky, lower, clickPos);
                if (hits.Count == 0)
                {
                    return true;
                }

                StrataBelowSelection.HandleBelowClick(__instance, hits);
                return false;
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Below selection failed: " + e.Message, 0x5B71003);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(SelectionDrawer), nameof(SelectionDrawer.DrawSelectionBracketFor))]
    public static class Patch_SelectionDrawer_DrawSelectionBracketFor_SeeBelow
    {
        public static bool Prefix(object obj, out bool __state)
        {
            __state = false;
            if (obj is not Thing thing) return true;
            if (!StrataBelowSelection.IsBelowSelected(thing, out Map sky, out Map lower))
            {
                return true;
            }
            if (!StrataBelowSelection.VisibleFromAbove(thing, sky, lower))
            {
                return false;
            }

            StrataBelowRenderer.EnsureBelowTransform();
            StrataBelowRenderer.OffsetActive = true;
            __state = true;
            return true;
        }

        public static void Postfix(bool __state)
        {
            if (__state)
            {
                StrataBelowRenderer.OffsetActive = false;
            }
        }
    }
}

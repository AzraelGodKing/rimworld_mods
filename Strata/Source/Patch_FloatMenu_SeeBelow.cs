using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    // Right-click on open sky builds the float menu against the map below
    // (same cell), so haul/strip/draft-move options appear for visible things.
    // CrossLevelOrderedJobs already routes the chosen job through stairs when
    // the pawn is not on that map.
    public static class StrataBelowFloatMenu
    {
        internal static bool Redirecting;

        public static bool ShouldRedirect(Vector3 clickPos, out Map sky, out Map lower)
        {
            sky = null;
            lower = null;
            if (Redirecting) return false;
            if (!StrataBelowSelection.TryGetBelowView(out sky, out lower)) return false;
            if (!UI.MouseCell().InBounds(sky)) return false;

            IntVec3 skyCell = clickPos.ToIntVec3();
            if (!skyCell.InBounds(sky)) return false;

            TerrainDef t = sky.terrainGrid.TerrainAt(skyCell);
            if (!UpperDeckUtility.IsSeeThroughGap(t)) return false;

            // Prefer upper-map things / zones when present at the cursor.
            if (StrataBelowSelection.SkyBlocksSelection(sky, clickPos)) return false;

            IntVec3 lowerCell = StrataBelowRenderer.SkyToLowerCell(sky, lower, skyCell);
            return lowerCell.InBounds(lower)
                && !lower.fogGrid.IsFogged(lowerCell)
                && !lower.roofGrid.Roofed(lowerCell);
        }

        public static bool TrySwapCurrentMap(Map target, out sbyte previousIndex)
        {
            previousIndex = 0;
            Game game = Current.Game;
            if (game == null || target == null) return false;
            int idx = Find.Maps.IndexOf(target);
            if (idx < 0) return false;
            previousIndex = game.currentMapIndex;
            game.currentMapIndex = (sbyte)idx;
            return true;
        }

        public static void RestoreCurrentMap(sbyte previousIndex)
        {
            if (Current.Game != null)
            {
                Current.Game.currentMapIndex = previousIndex;
            }
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.GetOptions))]
    public static class Patch_FloatMenuMakerMap_GetOptions_SeeBelow
    {
        public static bool Prefix(
            List<Pawn> selectedPawns,
            Vector3 clickPos,
            ref FloatMenuContext context,
            ref List<FloatMenuOption> __result)
        {
            if (!StrataBelowFloatMenu.ShouldRedirect(clickPos, out Map sky, out Map lower))
            {
                return true;
            }

            try
            {
                if (!StrataBelowFloatMenu.TrySwapCurrentMap(lower, out sbyte prev))
                {
                    return true;
                }

                StrataBelowFloatMenu.Redirecting = true;
                try
                {
                    // Remap click into lower-map space (1:1 or proportional).
                    IntVec3 skyCell = clickPos.ToIntVec3();
                    IntVec3 lowerCell = StrataBelowRenderer.SkyToLowerCell(sky, lower, skyCell);
                    Vector3 lowerClick = lowerCell.ToVector3Shifted();
                    lowerClick.y = clickPos.y;
                    __result = FloatMenuMakerMap.GetOptions(selectedPawns, lowerClick, out context);
                }
                finally
                {
                    StrataBelowFloatMenu.Redirecting = false;
                    StrataBelowFloatMenu.RestoreCurrentMap(prev);
                }

                return false;
            }
            catch (Exception e)
            {
                StrataBelowFloatMenu.Redirecting = false;
                Log.WarningOnce("[Strata] Below float menu failed: " + e.Message, 0x5B71004);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.GetOptions))]
    public static class Patch_FloatMenu_CrossLevelCombat
    {
        public static void Postfix(List<Pawn> selectedPawns, Vector3 clickPos, List<FloatMenuOption> __result)
        {
            if (!StrataCrossLevelCombat.Enabled || __result == null || selectedPawns == null)
            {
                return;
            }

            Map map = Find.CurrentMap;
            if (map == null) return;

            Thing target = StrataCrossLevelCombat.FindAttackTargetAt(map, clickPos, selectedPawns);
            if (target == null || target.MapHeld == null) return;

            for (int i = 0; i < selectedPawns.Count; i++)
            {
                Pawn pawn = selectedPawns[i];
                if (pawn == null || !pawn.Drafted || pawn.Downed || !pawn.Spawned) continue;
                if (pawn.Map == null || pawn.Map == target.MapHeld) continue;
                if (!StrataCrossLevelCombat.AreCrossGapPaired(pawn.Map, target.MapHeld, out _, out _)) continue;

                Verb_LaunchProjectile verb = StrataCrossLevelCombat.GetRangedVerb(pawn);
                if (verb == null) continue;

                IntVec3 stand = StrataCrossLevelCombat.FindFiringCell(pawn, target, verb);
                string label = "Attack".Translate(target.Label, target);
                if (!stand.IsValid)
                {
                    __result.Add(new FloatMenuOption(
                        "CannotHitTarget".Translate() + ": " + target.LabelCap,
                        null));
                    continue;
                }

                Pawn localPawn = pawn;
                Thing localTarget = target;
                __result.Add(new FloatMenuOption(label, () =>
                {
                    StrataCrossLevelCombat.TryStartCrossGapAttack(localPawn, localTarget);
                }));
            }
        }
    }
}

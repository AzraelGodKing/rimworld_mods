using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    [HarmonyPatch(typeof(Building_TurretGun), "TryFindNewTarget")]
    public static class Patch_TurretGun_TryFindNewTarget_CrossLevel
    {
        public static void Postfix(Building_Turret __instance, LocalTargetInfo __result)
        {
            if (!__result.IsValid && StrataCrossLevelCombat.Enabled)
            {
                StrataCrossLevelTurret.TryAutoAcquire(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Building_TurretGun), nameof(Building_TurretGun.GetGizmos))]
    public static class Patch_TurretGun_CrossLevelGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_Turret __instance)
        {
            foreach (Gizmo g in __result)
            {
                yield return g;
            }

            if (!StrataCrossLevelCombat.Enabled
                || __instance.Faction != Faction.OfPlayer
                || !StrataCrossLevelTurret.HasOrder(__instance, out _, out _))
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "Strata_StopCrossBombard".Translate(),
                defaultDesc = "Strata_StopCrossBombardDesc".Translate(),
                icon = TexCommand.ClearPrioritizedWork,
                action = () => StrataCrossLevelTurret.Cancel(__instance),
            };
        }
    }

    [HarmonyPatch(typeof(JobDriver_Wait), "CheckForAutoAttack")]
    public static class Patch_JobDriverWait_CrossLevelOverwatch
    {
        public static void Postfix(JobDriver_Wait __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn == null) return;
            if (pawn.stances?.curStance is Stance_Busy) return;
            StrataCrossLevelAutoEngage.TryOverwatchCrossFire(pawn);
        }
    }

    [HarmonyPatch(typeof(Targeter), nameof(Targeter.ProcessInputEvents))]
    public static class Patch_Targeter_CrossLevelTurret
    {
        public static bool Prefix(Targeter __instance)
        {
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0 || !__instance.IsTargeting)
            {
                return true;
            }
            if (!StrataCrossLevelCombat.Enabled) return true;

            try
            {
                if (TryHandleTurretTarget(__instance))
                {
                    Event.current.Use();
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Cross-level turret targeting failed: " + e.Message, 0x5B7100C);
            }
            return true;
        }

        private static bool TryHandleTurretTarget(Targeter targeter)
        {
            ITargetingSource source = targeter.targetingSource;
            Thing caster = source?.Caster;
            if (!(caster is Building_Turret turret)) return false;

            Verb verb = StrataCrossLevelTurret.LauncherVerb(turret);
            if (verb == null) return false;

            if (!StrataBelowSelection.TryGetBelowView(out Map sky, out Map lower) || sky != Find.CurrentMap)
            {
                // Shooting up while viewing the sky deck: turret on lower, click on sky thing/cell.
                if (Find.CurrentMap == null) return false;
                Map cur = Find.CurrentMap;
                Map paired = StrataCrossLevelCombat.PairedGapMap(turret.Map);
                if (paired == null || cur != paired && cur != turret.Map) return false;

                if (turret.Map != cur && cur == paired)
                {
                    foreach (LocalTargetInfo t in GenUI.TargetsAtMouse(TargetingParameters.ForAttackAny(), thingsOnly: true))
                    {
                        if (t.Thing != null)
                        {
                            return StrataCrossLevelTurret.TryOrder(turret, t.Thing, cur);
                        }
                    }
                    if (StrataCrossLevelTurret.IsArc(verb))
                    {
                        IntVec3 cell = UI.MouseCell();
                        if (cell.InBounds(cur))
                        {
                            return StrataCrossLevelTurret.TryOrder(turret, cell, cur);
                        }
                    }
                }
                return false;
            }

            Vector3 mouse = UI.MouseMapPosition();
            IntVec3 skyCell = mouse.ToIntVec3();
            if (!skyCell.InBounds(sky)) return false;

            TerrainDef terrain = sky.terrainGrid.TerrainAt(skyCell);
            bool openSky = terrain != null && terrain.defName == UpperDeckUtility.OpenSkyDefName;

            if (turret.Map == sky && openSky)
            {
                List<Thing> below = StrataBelowSelection.SelectablesUnderMouse(sky, lower, mouse);
                LocalTargetInfo target;
                if (below.Count > 0)
                {
                    target = below[0];
                }
                else if (StrataCrossLevelTurret.IsArc(verb))
                {
                    IntVec3 lowerCell = StrataBelowRenderer.SkyToLowerCell(sky, lower, skyCell);
                    if (!lowerCell.InBounds(lower)) return false;
                    target = lowerCell;
                }
                else
                {
                    return false;
                }
                return StrataCrossLevelTurret.TryOrder(turret, target, lower);
            }

            if (turret.Map == lower && !openSky)
            {
                foreach (LocalTargetInfo t in GenUI.TargetsAtMouse(TargetingParameters.ForAttackAny(), thingsOnly: true))
                {
                    if (t.Thing != null)
                    {
                        return StrataCrossLevelTurret.TryOrder(turret, t.Thing, sky);
                    }
                }
                if (StrataCrossLevelTurret.IsArc(verb))
                {
                    return StrataCrossLevelTurret.TryOrder(turret, skyCell, sky);
                }
            }

            return false;
        }
    }
}

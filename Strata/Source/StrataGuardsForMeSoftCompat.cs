using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    // Soft-compat for Guards For Me (aRandomKiwi.GuardsForMe). That mod paths
    // and assigns only on the current map, and it clears a bodyguard when the
    // person they follow changes maps. No hard reference: missing mod is a no-op.
    public static class StrataGuardsForMeSoftCompat
    {
        public const string PackageId = "aRandomKiwi.GuardsForMe";

        private static bool bound;
        private static Type compType;
        private static Type spotType;
        private static FieldInfo guardedPawnField;
        private static FieldInfo guardJobOkField;
        private static FieldInfo affectedPatrolField;
        private static FieldInfo affectedKindField;
        private static FieldInfo curWaypointField;
        private static FieldInfo forcedTargetField;
        private static FieldInfo selPawnField;
        private static FieldInfo guardAsJobField;
        private static MethodInfo guardModeMethod;
        private static MethodInfo deathSquadMethod;
        private static MethodInfo setReservedSpotMethod;
        private static MethodInfo clearAffectedSpotMethod;
        private static MethodInfo clearReservedSpotMethod;
        private static MethodInfo clearReservedGuardMethod;
        private static MethodInfo getAffectedGuardMethod;
        private static MethodInfo cancelStandingMethod;
        private static MethodInfo cancelPatrolMethod;
        private static MethodInfo cancelDeathSquadMethod;
        private static MethodInfo getGuardSpotMethod;
        private static MethodInfo getAffectedSpotMethod;
        private static FieldInfo gcField;

        private static Pawn vipHeld;
        private static object vipComp;

        public static bool Active => bound;

        public static void TryPatch(Harmony harmony)
        {
            if (harmony == null || bound)
            {
                return;
            }
            if (!ModsConfig.IsActive(PackageId))
            {
                return;
            }

            compType = AccessTools.TypeByName("aRandomKiwi.GFM.Comp_Guard");
            spotType = AccessTools.TypeByName("aRandomKiwi.GFM.Building_GuardSpot");
            Type utilsType = AccessTools.TypeByName("aRandomKiwi.GFM.Utils");
            Type settingsType = AccessTools.TypeByName("aRandomKiwi.GFM.Settings");
            if (compType == null)
            {
                return;
            }

            guardedPawnField = AccessTools.Field(compType, "guardedPawn");
            guardJobOkField = AccessTools.Field(compType, "guardJobOK");
            affectedPatrolField = AccessTools.Field(compType, "affectedPatrol");
            affectedKindField = AccessTools.Field(compType, "affectedGSKind");
            curWaypointField = AccessTools.Field(compType, "curPatrolWP");
            forcedTargetField = AccessTools.Field(compType, "deathSquadForcedTarget");
            guardModeMethod = AccessTools.Method(compType, "GuardMode");
            deathSquadMethod = AccessTools.Method(compType, "DeathSquadMode");
            setReservedSpotMethod = AccessTools.Method(compType, "setReservedGuardSpot");
            clearAffectedSpotMethod = AccessTools.Method(compType, "clearAffectedGuardSpot");
            clearReservedSpotMethod = AccessTools.Method(compType, "clearReservedGuardSpot");
            cancelStandingMethod = AccessTools.Method(compType, "cancelStandingGuard");
            cancelPatrolMethod = AccessTools.Method(compType, "cancelPatrolMode");
            cancelDeathSquadMethod = AccessTools.Method(compType, "cancelDeathSquad");
            getAffectedSpotMethod = AccessTools.Method(compType, "getAffectedGuardSpot");
            if (spotType != null)
            {
                selPawnField = AccessTools.Field(spotType, "selPawn");
                clearReservedGuardMethod = AccessTools.Method(spotType, "clearReservedGuard");
                getAffectedGuardMethod = AccessTools.Method(spotType, "getAffectedGuard");
            }
            if (settingsType != null)
            {
                guardAsJobField = AccessTools.Field(settingsType, "guardAsJob");
            }
            if (utilsType != null)
            {
                gcField = AccessTools.Field(utilsType, "GCGFM");
                Type gcType = gcField?.FieldType;
                if (gcType != null)
                {
                    getGuardSpotMethod = AccessTools.Method(gcType, "getGuardSpot");
                }
            }

            int n = 0;
            n += PatchTryGive(harmony, "aRandomKiwi.GFM.JobGiver_AIGotoGuardSpot", nameof(GuardSpotPrefix), prefix: true);
            n += PatchTryGive(harmony, "aRandomKiwi.GFM.JobGiver_AIGotoPatrolWaypoint", nameof(PatrolPrefix), prefix: true);
            n += PatchTryGive(harmony, "aRandomKiwi.GFM.JobGiver_AIFollowVIP", nameof(FollowPostfix), prefix: false);
            n += PatchTryGive(harmony, "aRandomKiwi.GFM.JobGiver_AIGotoNearestHostile", nameof(ReactionPostfix), prefix: false);

            MethodInfo tick = AccessTools.Method(compType, "CompTick");
            if (tick != null)
            {
                harmony.Patch(
                    tick,
                    prefix: new HarmonyMethod(typeof(StrataGuardsForMeSoftCompat), nameof(KeepVipPrefix)),
                    postfix: new HarmonyMethod(typeof(StrataGuardsForMeSoftCompat), nameof(KeepVipPostfix)));
                n++;
            }

            MethodInfo stopAll = AccessTools.Method(compType, "stopAllGuardians");
            if (stopAll != null)
            {
                harmony.Patch(stopAll, postfix: new HarmonyMethod(typeof(StrataGuardsForMeSoftCompat), nameof(StopAllPostfix)));
                n++;
            }

            MethodInfo guardGizmos = AccessTools.Method(compType, "CompGetGizmosExtra");
            if (guardGizmos != null)
            {
                harmony.Patch(guardGizmos, postfix: new HarmonyMethod(typeof(StrataGuardsForMeSoftCompat), nameof(BodyguardGizmoPostfix)));
                n++;
            }

            if (spotType != null)
            {
                MethodInfo spotGizmos = AccessTools.Method(spotType, "GetGizmos");
                if (spotGizmos != null)
                {
                    harmony.Patch(spotGizmos, postfix: new HarmonyMethod(typeof(StrataGuardsForMeSoftCompat), nameof(SpotGizmoPostfix)));
                    n++;
                }
            }

            bound = guardedPawnField != null;
            if (bound)
            {
                StrataLog.Verbose("[Strata] Guards For Me soft-compat: linked floors (" + n + " hooks).");
            }
        }

        public static bool IsOnDuty(Pawn pawn)
        {
            if (!bound || pawn == null)
            {
                return false;
            }
            object comp = CompOf(pawn);
            if (comp == null)
            {
                return false;
            }
            bool asJob = guardAsJobField?.GetValue(null) is bool flag && flag;
            int ok = guardJobOkField?.GetValue(comp) is int n ? n : 0;
            if (asJob && ok != 1 && ok != 2)
            {
                return false;
            }
            if (guardedPawnField.GetValue(comp) is Pawn)
            {
                return true;
            }
            if (affectedPatrolField?.GetValue(comp) is string patrol && patrol.Length > 0)
            {
                return true;
            }
            return InvokeBool(guardModeMethod, comp) || InvokeBool(deathSquadMethod, comp);
        }

        public static bool GuardSpotPrefix(Pawn pawn, ref Job __result)
        {
            Thing spot = GuardTarget(pawn);
            if (!NeedsStairs(pawn, spot))
            {
                return true;
            }
            __result = Relay(pawn, spot.Map, spot.Position);
            return false;
        }

        public static bool PatrolPrefix(Pawn pawn, ref Job __result)
        {
            Thing waypoint = Waypoint(pawn);
            if (!NeedsStairs(pawn, waypoint))
            {
                return true;
            }
            __result = Relay(pawn, waypoint.Map, waypoint.Position);
            return false;
        }

        public static void FollowPostfix(Pawn pawn, ref Job __result)
        {
            if (!bound || __result != null || pawn?.Map == null)
            {
                return;
            }
            if (guardedPawnField?.GetValue(CompOf(pawn)) is not Pawn vip || !NeedsStairs(pawn, vip))
            {
                return;
            }
            Job hop = Relay(pawn, vip.Map, vip.Position);
            if (hop != null)
            {
                __result = hop;
            }
        }

        public static void ReactionPostfix(Pawn pawn, ref Job __result)
        {
            if (!bound || __result != null || pawn?.Map == null)
            {
                return;
            }
            object comp = CompOf(pawn);
            if (comp == null || !InvokeBool(deathSquadMethod, comp))
            {
                return;
            }
            if (forcedTargetField?.GetValue(comp) is Thing forced && NeedsStairs(pawn, forced))
            {
                Job hop = Relay(pawn, forced.MapHeld, forced.PositionHeld);
                if (hop != null)
                {
                    __result = hop;
                }
                return;
            }
            if (!TryNearestThreat(pawn, out Thing threat))
            {
                return;
            }
            Job chase = Relay(pawn, threat.Map, threat.Position);
            if (chase != null)
            {
                __result = chase;
            }
        }

        public static void KeepVipPrefix(ThingComp __instance)
        {
            vipHeld = null;
            vipComp = null;
            if (!bound || __instance == null || guardedPawnField == null)
            {
                return;
            }
            if (guardedPawnField.GetValue(__instance) is not Pawn vip)
            {
                return;
            }
            Pawn guard = __instance.parent as Pawn;
            if (guard?.Map == null || vip.Map == null || vip.Map == guard.Map)
            {
                return;
            }
            if (!vip.Spawned || vip.Dead || !ColonyBedUtility.MapsLinked(guard.Map, vip.Map))
            {
                return;
            }
            vipHeld = vip;
            vipComp = __instance;
        }

        public static void KeepVipPostfix(ThingComp __instance)
        {
            if (vipComp != __instance || vipHeld == null || guardedPawnField == null)
            {
                vipHeld = null;
                vipComp = null;
                return;
            }
            Pawn current = guardedPawnField.GetValue(__instance) as Pawn;
            if (current == null && vipHeld.Spawned && !vipHeld.Dead)
            {
                guardedPawnField.SetValue(__instance, vipHeld);
            }
            vipHeld = null;
            vipComp = null;
        }

        public static void StopAllPostfix(ThingComp __instance)
        {
            if (!bound || __instance?.parent is not Pawn self)
            {
                return;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || map == self.Map || !ColonyBedUtility.MapsLinked(self.Map, map))
                {
                    continue;
                }
                foreach (Pawn guard in map.mapPawns.FreeColonists)
                {
                    object comp = CompOf(guard);
                    if (comp != null && guardedPawnField.GetValue(comp) == self)
                    {
                        guardedPawnField.SetValue(comp, null);
                    }
                }
            }
        }

        public static void BodyguardGizmoPostfix(ThingComp __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!bound || __instance?.parent is not Pawn guard || guard.Faction != Faction.OfPlayer)
            {
                return;
            }
            if (CannotFight(guard))
            {
                return;
            }
            List<Pawn> people = PeopleOnLinkedFloors(guard.Map, guard);
            if (people.Count == 0)
            {
                return;
            }
            Texture2D icon = ContentFinder<Texture2D>.Get("UI/GFM_Guard", false);
            var command = new Command_Action
            {
                icon = icon,
                defaultLabel = "Strata_GFM_BodyguardOtherFloor".Translate(),
                defaultDesc = "Strata_GFM_BodyguardOtherFloorDesc".Translate(),
                action = delegate
                {
                    var opts = new List<FloatMenuOption>();
                    for (int i = 0; i < people.Count; i++)
                    {
                        Pawn person = people[i];
                        opts.Add(new FloatMenuOption(FloorLabel(person), delegate
                        {
                            AssignBodyguard(guard, person);
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(opts));
                },
            };
            __result = Append(__result, command);
        }

        public static void SpotGizmoPostfix(Building __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!bound || __instance?.Map == null || spotType == null || !spotType.IsInstanceOfType(__instance))
            {
                return;
            }
            List<Pawn> guards = GuardsForSpot(__instance);
            if (guards.Count == 0)
            {
                return;
            }
            Texture2D icon = ContentFinder<Texture2D>.Get("UI/GFM_AffectGuardToGS", false);
            var command = new Command_Action
            {
                icon = icon,
                defaultLabel = "Strata_GFM_AssignFromFloor".Translate(),
                defaultDesc = "Strata_GFM_AssignFromFloorDesc".Translate(),
                action = delegate
                {
                    var opts = new List<FloatMenuOption>();
                    for (int i = 0; i < guards.Count; i++)
                    {
                        Pawn guard = guards[i];
                        opts.Add(new FloatMenuOption(FloorLabel(guard), delegate
                        {
                            AssignSpot(guard, __instance);
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(opts));
                },
            };
            __result = Append(__result, command);
        }

        private static int PatchTryGive(Harmony harmony, string typeName, string methodName, bool prefix)
        {
            Type type = AccessTools.TypeByName(typeName);
            MethodInfo give = type == null ? null : AccessTools.Method(type, "TryGiveJob");
            if (give == null)
            {
                return 0;
            }
            var hook = new HarmonyMethod(typeof(StrataGuardsForMeSoftCompat), methodName);
            if (prefix)
            {
                harmony.Patch(give, prefix: hook);
            }
            else
            {
                harmony.Patch(give, postfix: hook);
            }
            return 1;
        }

        private static Job Relay(Pawn pawn, Map dest, IntVec3 near)
        {
            if (pawn?.Map == null || dest == null || dest == pawn.Map)
            {
                return null;
            }
            if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Drafted || pawn.InMentalState)
            {
                return null;
            }
            if (!StrataPawnUtility.CanUseLevelPortals(pawn))
            {
                return null;
            }
            if (!ColonyBedUtility.MapsLinked(pawn.Map, dest))
            {
                return null;
            }
            if (pawn.CurJobDef == JobDefOf.EnterPortal || PortalRelayChain.HasIntent(pawn))
            {
                return null;
            }
            return PawnRelay.TryRelayToMap(
                pawn,
                dest,
                touchCooldown: false,
                RelayPurpose.Guard,
                preferArrivalNear: near);
        }

        private static bool NeedsStairs(Pawn pawn, Thing target)
        {
            return bound
                && pawn?.Map != null
                && target?.Map != null
                && target.Map != pawn.Map
                && target.Spawned
                && !target.Destroyed
                && ColonyBedUtility.MapsLinked(pawn.Map, target.Map);
        }

        private static bool TryNearestThreat(Pawn pawn, out Thing threat)
        {
            threat = null;
            int bestDepth = int.MaxValue;
            List<LevelGraph.LevelLink> links = LevelGraph.CopyLinks(LevelGraph.ReachableLevels(pawn.Map));
            for (int i = 0; i < links.Count; i++)
            {
                LevelGraph.LevelLink link = links[i];
                if (link.map == null || link.depth > bestDepth)
                {
                    continue;
                }
                Thing found = FirstHostile(link.map);
                if (found == null)
                {
                    continue;
                }
                threat = found;
                bestDepth = link.depth;
            }
            return threat != null;
        }

        private static Thing FirstHostile(Map map)
        {
            if (map?.attackTargetsCache == null)
            {
                return null;
            }
            try
            {
                foreach (IAttackTarget target in map.attackTargetsCache.TargetsHostileToFaction(Faction.OfPlayer))
                {
                    Thing thing = target?.Thing;
                    if (thing == null || thing.Destroyed || !thing.Spawned || thing.Map != map)
                    {
                        continue;
                    }
                    if (thing is Pawn hostile && (hostile.Dead || hostile.Downed))
                    {
                        continue;
                    }
                    return thing;
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        private static Thing GuardTarget(Pawn pawn)
        {
            object comp = CompOf(pawn);
            if (comp == null)
            {
                return null;
            }
            return getAffectedSpotMethod?.Invoke(comp, null) as Thing;
        }

        private static Thing Waypoint(Pawn pawn)
        {
            object comp = CompOf(pawn);
            if (comp == null || curWaypointField == null)
            {
                return null;
            }
            return curWaypointField.GetValue(comp) as Thing;
        }

        private static void AssignBodyguard(Pawn guard, Pawn person)
        {
            object comp = CompOf(guard);
            if (comp == null || person == null)
            {
                return;
            }
            cancelStandingMethod?.Invoke(comp, null);
            cancelPatrolMethod?.Invoke(comp, null);
            cancelDeathSquadMethod?.Invoke(comp, null);
            guardJobOkField?.SetValue(comp, 0);
            guardedPawnField.SetValue(comp, person);
            if (guard.Map != null)
            {
                guard.Map.pawnDestinationReservationManager.ReleaseAllClaimedBy(guard);
                guard.jobs?.StopAll();
                guard.jobs?.ClearQueuedJobs();
            }
        }

        private static void AssignSpot(Pawn guard, Building spot)
        {
            object comp = CompOf(guard);
            if (comp == null || spot == null)
            {
                return;
            }
            ReleaseSpot(spot);
            ClearSpotsHeldBy(guard);
            clearAffectedSpotMethod?.Invoke(comp, null);
            clearReservedSpotMethod?.Invoke(comp, null);
            selPawnField?.SetValue(spot, guard);
            setReservedSpotMethod?.Invoke(comp, new object[] { spot });
            guardJobOkField?.SetValue(comp, 0);
        }

        private static void ReleaseSpot(Building spot)
        {
            Pawn previous = getAffectedGuardMethod?.Invoke(spot, null) as Pawn;
            if (previous != null)
            {
                object comp = CompOf(previous);
                clearAffectedSpotMethod?.Invoke(comp, null);
                clearReservedSpotMethod?.Invoke(comp, null);
            }
            clearReservedGuardMethod?.Invoke(spot, null);
        }

        private static void ClearSpotsHeldBy(Pawn guard)
        {
            if (gcField == null || getGuardSpotMethod == null)
            {
                return;
            }
            object gc = gcField.GetValue(null);
            if (gc == null || getGuardSpotMethod.Invoke(gc, null) is not IEnumerable spots)
            {
                return;
            }
            foreach (object spot in spots)
            {
                if (spot == null || getAffectedGuardMethod?.Invoke(spot, null) != guard)
                {
                    continue;
                }
                clearReservedGuardMethod?.Invoke(spot, null);
            }
        }

        private static List<Pawn> GuardsForSpot(Building spot)
        {
            var list = new List<Pawn>();
            string kind = spot.def?.defName;
            if (string.IsNullOrEmpty(kind))
            {
                return list;
            }
            Pawn reserved = selPawnField?.GetValue(spot) as Pawn;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || map == spot.Map || !ColonyBedUtility.MapsLinked(spot.Map, map))
                {
                    continue;
                }
                foreach (Pawn pawn in map.mapPawns.FreeColonists)
                {
                    if (pawn == reserved || CannotFight(pawn))
                    {
                        continue;
                    }
                    object comp = CompOf(pawn);
                    if (comp == null || affectedKindField?.GetValue(comp) as string != kind)
                    {
                        continue;
                    }
                    if (LevelGraph.BestFirstStep(pawn.Map, spot.Map, pawn.Position, pawn, spot.Position) == null)
                    {
                        continue;
                    }
                    list.Add(pawn);
                }
            }
            return list;
        }

        private static List<Pawn> PeopleOnLinkedFloors(Map from, Pawn except)
        {
            var list = new List<Pawn>();
            if (from == null)
            {
                return list;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || map == from || !ColonyBedUtility.MapsLinked(from, map))
                {
                    continue;
                }
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn pawn = pawns[j];
                    if (pawn == null || pawn == except || pawn.Dead || pawn.Downed || !pawn.RaceProps.Humanlike)
                    {
                        continue;
                    }
                    if (pawn.Faction == null)
                    {
                        continue;
                    }
                    if (pawn.Faction != Faction.OfPlayer
                        && pawn.Faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Ally
                        && pawn.Faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Neutral)
                    {
                        continue;
                    }
                    list.Add(pawn);
                }
            }
            return list;
        }

        private static bool CannotFight(Pawn pawn)
        {
            return pawn?.story != null
                && (pawn.story.DisabledWorkTagsBackstoryAndTraits & WorkTags.Violent) != 0;
        }

        private static string FloorLabel(Pawn pawn)
        {
            string floor = pawn.Map?.Parent?.LabelCap ?? pawn.Map?.ToString();
            if (floor.NullOrEmpty())
            {
                return pawn.LabelShortCap;
            }
            return pawn.LabelShortCap + " (" + floor + ")";
        }

        private static object CompOf(Pawn pawn)
        {
            if (pawn == null || compType == null)
            {
                return null;
            }
            return pawn.AllComps?.Find(comp => comp != null && compType.IsInstanceOfType(comp));
        }

        private static bool InvokeBool(MethodInfo method, object comp)
        {
            return method != null && method.Invoke(comp, null) is bool value && value;
        }

        private static IEnumerable<Gizmo> Append(IEnumerable<Gizmo> first, Gizmo extra)
        {
            if (first != null)
            {
                foreach (Gizmo gizmo in first)
                {
                    yield return gizmo;
                }
            }
            yield return extra;
        }
    }
}

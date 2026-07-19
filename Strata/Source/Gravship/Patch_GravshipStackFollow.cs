using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Detach linked Strata floors early so abandon cannot cascade-destroy them.
    [HarmonyPatch(typeof(WorldComponent_GravshipController), nameof(WorldComponent_GravshipController.InitiateTakeoff))]
    public static class Patch_Gravship_InitiateTakeoff
    {
        public static void Prefix(Building_GravEngine engine)
        {
            if (engine == null)
            {
                return;
            }
            StrataGravshipPortalTravel.SnapshotHostPortals(engine);
            StrataGravshipFootprintSnapshot.Capture(engine);
            List<Map> levels = StrataGravshipStackUtility.CollectTravellingLevels(engine);
            var stacks = WorldComponent_StrataGravshipStacks.Get();
            stacks?.RememberTakeoffEngine(engine);
            stacks?.MarkTravelling(levels);
            // After travelling floors are known: pull prisoners/babies off the
            // host deck fringe and non-travelling colony digs onto the ship.
            StrataGravshipPawnRescue.RescueAtTakeoff(engine);
        }
    }

    // Vanilla only warns about pawns on the engine map. Flag colony digs /
    // linked floors that will not travel (RescueAtTakeoff still pulls
    // prisoners/babies when launch actually starts).
    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.PreLaunchConfirmation))]
    public static class Patch_Gravship_PreLaunchConfirmation_LinkedLeftBehind
    {
        public static void Postfix(Building_GravEngine engine)
        {
            if (engine?.Map == null)
            {
                return;
            }
            var people = new List<Pawn>();
            var animals = new List<Pawn>();
            StrataGravshipPawnRescue.CollectLeftBehindWarnings(engine, people, animals);
            if (people.Count == 0 && animals.Count == 0)
            {
                return;
            }
            string names = string.Join(", ", people.ConvertAll(p => p.LabelShort));
            if (animals.Count > 0)
            {
                if (names.Length > 0)
                {
                    names += ", ";
                }
                names += string.Join(", ", animals.ConvertAll(p => p.LabelShort));
            }
            Messages.Message(
                "Strata_GravshipLinkedLeftBehindWarn".Translate(names),
                MessageTypeDefOf.CautionInput,
                historical: false);
        }
    }

    // Associate the packed Gravship WorldObject with those floors.
    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.GenerateGravship))]
    public static class Patch_Gravship_GenerateRegisterStack
    {
        public static void Postfix(Building_GravEngine engine, Gravship __result)
        {
            if (__result == null || engine == null)
            {
                return;
            }
            // Must run after vanilla packs+despawns: GravAnchor keeps the map, so
            // any shaft Odyssey skipped would otherwise stay visible on the old site.
            StrataGravshipPortalTravel.SweepLeftBehindHostShafts(__result, engine);
            WorldComponent_StrataGravshipStacks.Get()?.RegisterTakeoff(__result, engine);
        }
    }

    // Detach any remaining children when the launch map is abandoned.
    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.AbandonMap))]
    public static class Patch_Gravship_AbandonMap
    {
        public static void Prefix(Map map)
        {
            if (map?.ChildPocketMaps == null)
            {
                return;
            }
            foreach (Map child in map.ChildPocketMaps.ToList())
            {
                if (!StrataGravshipStackUtility.IsStrataLinkedLevel(child))
                {
                    continue;
                }
                if (child.Parent is PocketMapParent pocket)
                {
                    pocket.sourceMap = null;
                }
            }
        }
    }

    // Never destroy a map registered as travelling with a gravship.
    [HarmonyPatch(typeof(PocketMapUtility), nameof(PocketMapUtility.DestroyPocketMap))]
    public static class Patch_Gravship_DestroyPocketMapGuard
    {
        public static bool Prefix(Map map)
        {
            var comp = WorldComponent_StrataGravshipStacks.Get();
            if (comp != null && comp.IsTravelling(map))
            {
                Log.Message($"[Strata] Blocked DestroyPocketMap on travelling level {map}.");
                return false;
            }
            return true;
        }
    }

    // Rebind after the ship is placed into a destination map.
    [HarmonyPatch(typeof(GravshipPlacementUtility), nameof(GravshipPlacementUtility.PlaceGravshipInMap))]
    public static class Patch_Gravship_PlaceInMap
    {
        public static void Postfix(Gravship gravship, Map map)
        {
            if (gravship == null || map == null)
            {
                return;
            }
            WorldComponent_StrataGravshipStacks.Get()?.CompleteLanding(gravship, map);
        }
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.ArriveExistingMap))]
    public static class Patch_Gravship_ArriveExistingMap
    {
        public static void Postfix(Gravship gravship)
        {
            GravshipLandHooks.TryLand(gravship);
        }
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.ArriveNewMap))]
    public static class Patch_Gravship_ArriveNewMap
    {
        public static void Postfix(Gravship gravship)
        {
            GravshipLandHooks.TryLand(gravship);
        }
    }

    [HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.PostSwapMap))]
    public static class Patch_Gravship_EnginePostSwapMap
    {
        public static void Postfix(Building_GravEngine __instance)
        {
            if (__instance?.Map == null)
            {
                return;
            }
            // Soft prepare only — PlaceGravshipInMap / Arrive* commit the full
            // land (shaft restore + wire). Doing that here spawns temporary
            // shafts that packed stairs then replace, orphaning the underdeck.
            WorldComponent_StrataGravshipStacks.Get()?.PrepareLanding(__instance.Map);
        }
    }

    internal static class GravshipLandHooks
    {
        public static void TryLand(Gravship gravship)
        {
            if (gravship == null)
            {
                return;
            }
            Map host = FindHostMap(gravship);
            if (host == null)
            {
                return;
            }
            WorldComponent_StrataGravshipStacks.Get()?.CompleteLanding(gravship, host);
        }

        // Prefer the packed ship's engine map — never the first GravEngine on any map
        // (GravAnchor / multi-colony can leave an engine behind on the departure site).
        private static Map FindHostMap(Gravship gravship)
        {
            Building_GravEngine engine = gravship?.Engine;
            if (engine?.Map != null
                && !StrataMapUtility.IsUnderground(engine.Map)
                && !StrataMapUtility.IsUpperLevel(engine.Map))
            {
                return engine.Map;
            }

            return Find.CurrentMap != null
                && !StrataMapUtility.IsUnderground(Find.CurrentMap)
                && !StrataMapUtility.IsUpperLevel(Find.CurrentMap)
                && StrataGravshipUtility.FindGravEngineOnMap(Find.CurrentMap) != null
                ? Find.CurrentMap
                : null;
        }
    }
}

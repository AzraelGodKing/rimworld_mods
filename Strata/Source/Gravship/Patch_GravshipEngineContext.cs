using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Odyssey treats grav engines as map-local. Gravship-linked Strata B+/A+ floors
    // inherit the host engine for requirements, placement, launch, and kidnapping checks.
    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.GetPlayerGravEngine_NewTemp))]
    public static class Patch_Gravship_GetPlayerGravEngine
    {
        public static void Postfix(Map map, ref Building_GravEngine __result)
        {
            if (__result != null || !StrataGravshipUtility.OdysseyActive)
            {
                return;
            }
            __result = StrataGravshipUtility.FindGravEngine(map);
        }
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.PlayerHasGravEngine), new[] { typeof(Map) })]
    public static class Patch_Gravship_PlayerHasGravEngineOnMap
    {
        public static void Postfix(Map map, ref bool __result)
        {
            if (__result || !StrataGravshipUtility.OdysseyActive)
            {
                return;
            }
            __result = StrataGravshipUtility.FindGravEngine(map) != null;
        }
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.IsOnboardGravship))]
    public static class Patch_Gravship_IsOnboardGravship
    {
        public static bool Prefix(IntVec3 cell, Building_GravEngine engine, ref bool __result)
        {
            return StrataGravshipUtility.AllowVanillaOnboardCheck(cell, engine, ref __result);
        }

        public static void Postfix(IntVec3 cell, Building_GravEngine engine, ref bool __result)
        {
            StrataGravshipUtility.ApplyOnboardPostfix(cell, engine, ref __result);
        }
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.IsOnboardGravship_NewTemp))]
    public static class Patch_Gravship_IsOnboardGravshipNewTemp
    {
        public static bool Prefix(IntVec3 cell, Building_GravEngine engine, ref bool __result)
        {
            return StrataGravshipUtility.AllowVanillaOnboardCheck(cell, engine, ref __result);
        }

        public static void Postfix(IntVec3 cell, Building_GravEngine engine, ref bool __result)
        {
            StrataGravshipUtility.ApplyOnboardPostfix(cell, engine, ref __result);
        }
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.InsideFootprint))]
    public static class Patch_Gravship_InsideFootprint
    {
        public static void Postfix(IntVec3 loc, Map map, ref bool __result)
        {
            if (__result || map == null || !StrataGravshipUtility.OdysseyActive)
            {
                return;
            }
            Building_GravEngine engine = StrataGravshipUtility.FindGravEngine(map);
            if (engine != null && StrataGravshipUtility.IsLinkedDeckCell(map, loc, engine))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.ValidSubstructureAt))]
    public static class Patch_Gravship_ValidSubstructureAt
    {
        public static void Postfix(Building_GravEngine __instance, IntVec3 cell, ref bool __result)
        {
            if (__result || __instance == null || !StrataGravshipUtility.OdysseyActive)
            {
                return;
            }
            Map map = Find.CurrentMap;
            if (map != null && map != __instance.Map
                && StrataGravshipUtility.IsLinkedDeckCell(map, cell, __instance))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.OnValidSubstructure))]
    public static class Patch_Gravship_OnValidSubstructure
    {
        public static void Postfix(Building_GravEngine __instance, Thing thing, ref bool __result)
        {
            if (__result || __instance == null || thing?.Map == null
                || !StrataGravshipUtility.OdysseyActive)
            {
                return;
            }
            if (thing.Map != __instance.Map
                && StrataGravshipUtility.IsLinkedDeckCell(thing.Map, thing.Position, __instance))
            {
                __result = true;
                return;
            }

            // Host shafts: accept if the origin cell is on Valid substructure even
            // when OccupiedRect has a fringe cell Odyssey rejects (2x2 stairs).
            if (thing.Map == __instance.Map
                && StrataGravshipUtility.IsGravshipHostShaft(thing)
                && thing.def.bringAlongOnGravship
                && __instance.ValidSubstructureAt(thing.Position))
            {
                __result = true;
            }
        }
    }

    // Pawns on gravship-linked Strata decks count as aboard for launch warnings.
    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.ConsideredKidnapped))]
    public static class Patch_Gravship_ConsideredKidnapped
    {
        public static void Postfix(Building_GravEngine engine, Pawn pawn, ref bool __result)
        {
            if (!__result || engine?.Map == null || pawn?.Map == null
                || !StrataGravshipUtility.IsGravshipLinkedLevel(pawn.Map))
            {
                return;
            }
            Map root = StrataGravshipUtility.FindGravshipStackRoot(pawn.Map);
            if (root == engine.Map)
            {
                __result = false;
            }
        }
    }

    // Gravship facilities/thrusters on linked B+/A+ decks stay connected to the host engine.
    [HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.LooselyConnectedToGravEngine))]
    public static class Patch_Gravship_LooselyConnectedToGravEngine
    {
        public static bool Prefix(Building_GravEngine __instance, Thing thing, ref bool __result)
        {
            if (thing?.Map == null || __instance?.Map == null || thing.Map == __instance.Map
                || !StrataGravshipUtility.IsGravshipLinkedLevel(thing.Map))
            {
                return true;
            }
            Map root = StrataGravshipUtility.FindGravshipStackRoot(thing.Map);
            if (root == __instance.Map)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}

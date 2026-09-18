using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Underground fog: open explored cells clear; MakeFog wall faces that border
    // revealed open stay visible; deep undug rock stays fogged (AZR-57 / AZR-232).
    //
    // Deferred mineable fill means GenStep_StrataFog often runs before rock exists.
    // FloodUnfog then walks the whole map (no MakeFog blockers) and leaves rock
    // revealed after spawn. Arrival fog is re-applied after SpawnMineablesChunked.
    //
    // Do NOT blanket-Refog every mineable on Unfog (old AZR-57): that fought
    // FloodUnfogAdjacent wall-face reveal and left circular black arrival rims.
    // Only Refog mineable MakeFog that does not touch a revealed open cell.

    internal static class StrataUndergroundFog
    {
        internal static bool Suppressing;

        public static bool IsMineableMakeFog(Building edifice)
        {
            return edifice != null && edifice.def.MakeFog && edifice.def.mineable;
        }

        // Revealed walkable/open: unfogged and not a MakeFog blocker (doors count as open).
        public static bool IsRevealedOpen(FogGrid fog, Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map) || fog.IsFogged(cell))
            {
                return false;
            }

            Building edifice = cell.GetEdifice(map);
            return edifice == null || !edifice.def.MakeFog || edifice.def.IsDoor;
        }

        public static bool TouchesRevealedOpen(FogGrid fog, Map map, IntVec3 cell)
        {
            for (int i = 0; i < 8; i++)
            {
                if (IsRevealedOpen(fog, map, cell + GenAdj.AdjacentCells[i]))
                {
                    return true;
                }
            }

            return false;
        }

        // Broader AZR-232 gate: any unfogged neighbor, including revealed rock faces.
        public static bool TouchesRevealedSpace(FogGrid fog, Map map, IntVec3 cell)
        {
            for (int i = 0; i < 8; i++)
            {
                IntVec3 n = cell + GenAdj.AdjacentCells[i];
                if (n.InBounds(map) && !fog.IsFogged(n))
                {
                    return true;
                }
            }

            return false;
        }

        public static IntVec3 FindFogRoot(Map map)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            if (MapGenerator.PlayerStartSpot.IsValid && MapGenerator.PlayerStartSpot.InBounds(map))
            {
                return MapGenerator.PlayerStartSpot;
            }

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing != null && thing.Spawned && thing.Position.InBounds(map))
                {
                    return thing.Position;
                }
            }

            return map.Center;
        }

        // Deep rock = mineable MakeFog not adjacent to revealed open space.
        public static void RefogDeepRock(Map map)
        {
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            FogGrid fog = map.fogGrid;
            Suppressing = true;
            try
            {
                foreach (IntVec3 cell in map.AllCells)
                {
                    if (fog.IsFogged(cell))
                    {
                        continue;
                    }

                    Building edifice = cell.GetEdifice(map);
                    if (!IsMineableMakeFog(edifice))
                    {
                        continue;
                    }

                    if (TouchesRevealedOpen(fog, map, cell))
                    {
                        continue;
                    }

                    fog.Refog(new CellRect(cell.x, cell.z, 1, 1));
                }
            }
            finally
            {
                Suppressing = false;
            }
        }

        // Whole-map Refog + arrival FloodUnfog. Call only when MakeFog rock is present.
        public static void ApplyArrivalFog(Map map)
        {
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            IntVec3 root = FindFogRoot(map);
            if (!root.IsValid || !root.InBounds(map))
            {
                root = map.Center;
            }

            map.fogGrid.Refog(CellRect.WholeMap(map));
            FloodFillerFog.FloodUnfog(root, map);
            RefogDeepRock(map);
        }

        public static void HealStuckDugFog(Map map)
        {
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            FogGrid fog = map.fogGrid;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!fog.IsFogged(cell))
                {
                    continue;
                }

                Building edifice = cell.GetEdifice(map);
                if (edifice != null && edifice.def.MakeFog)
                {
                    continue;
                }

                if (!TouchesRevealedSpace(fog, map, cell))
                {
                    continue;
                }

                fog.FloodUnfogAdjacent(cell, sendLetters: false);
            }

            RefogDeepRock(map);
        }
    }

    // Selective deep-rock Refog after Unfog — keeps edge faces, hides volume.
    [HarmonyPatch(typeof(FogGrid), nameof(FogGrid.Unfog))]
    public static class Patch_Unfog_DeepRock
    {
        private static readonly AccessTools.FieldRef<FogGrid, Map> MapField =
            AccessTools.FieldRefAccess<FogGrid, Map>("map");

        public static void Postfix(FogGrid __instance, IntVec3 c)
        {
            if (StrataUndergroundFog.Suppressing)
            {
                return;
            }

            Map map = MapField(__instance);
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            Building edifice = c.GetEdifice(map);
            if (!StrataUndergroundFog.IsMineableMakeFog(edifice))
            {
                return;
            }

            if (StrataUndergroundFog.TouchesRevealedOpen(__instance, map, c))
            {
                return;
            }

            StrataUndergroundFog.Suppressing = true;
            try
            {
                __instance.Refog(new CellRect(c.x, c.z, 1, 1));
            }
            finally
            {
                StrataUndergroundFog.Suppressing = false;
            }
        }
    }

    [HarmonyPatch(typeof(FogGrid), nameof(FogGrid.Notify_FogBlockerRemoved))]
    public static class Patch_FogBlockerRemoved_Underground
    {
        private static readonly AccessTools.FieldRef<FogGrid, Map> MapField =
            AccessTools.FieldRefAccess<FogGrid, Map>("map");

        public static void Postfix(FogGrid __instance, Thing thing)
        {
            if (Current.ProgramState != ProgramState.Playing || thing == null)
            {
                return;
            }

            Map map = MapField(__instance);
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            IntVec3 cell = thing.Position;
            if (!cell.InBounds(map) || !__instance.IsFogged(cell))
            {
                return;
            }

            Building edifice = cell.GetEdifice(map);
            if (edifice != null && edifice.def.MakeFog)
            {
                return;
            }

            if (!StrataUndergroundFog.TouchesRevealedSpace(__instance, map, cell))
            {
                return;
            }

            bool sendLetters = map.generatorDef == null
                || !map.generatorDef.ignoreAreaRevealedLetter;
            __instance.FloodUnfogAdjacent(thing, sendLetters);
        }

        public static void HealStuckDugFog(Map map)
        {
            StrataUndergroundFog.HealStuckDugFog(map);
        }
    }
}

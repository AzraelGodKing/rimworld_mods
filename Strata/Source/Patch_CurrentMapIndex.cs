using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Vanilla Game.CurrentMap is `maps[currentMapIndex]` with no upper bound check.
    // Pocket create/destroy can leave that index past maps.Count and spam
    // ArgumentOutOfRangeException until the view is reassigned.
    //
    // Never Harmony-patch get_CurrentMap — it is read hundreds of times per frame
    // and any Prefix shows up as ~8–13% of frame in Dubs. Clamp only when the maps
    // list changes (and when see-below restores a possibly stale index).
    public static class Patch_CurrentMapIndex
    {
        private static readonly AccessTools.FieldRef<Game, List<Map>> MapsRef =
            AccessTools.FieldRefAccess<Game, List<Map>>("maps");

        private static bool warnedThisSession;

        public static void ClampIfNeeded(Game game)
        {
            if (game == null)
            {
                return;
            }

            sbyte index = game.currentMapIndex;
            if (index < 0)
            {
                return;
            }

            List<Map> maps = MapsRef(game);
            int count = maps != null ? maps.Count : 0;
            if (count == 0)
            {
                game.currentMapIndex = -1;
                return;
            }

            if ((uint)index < (uint)count)
            {
                return;
            }

            game.currentMapIndex = 0;
            if (!warnedThisSession)
            {
                warnedThisSession = true;
                Log.Warning("[Strata] CurrentMap index was out of range (index="
                    + index + ", maps=" + count
                    + "); clamped to map 0. Further repairs this session are silent.");
            }
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
    public static class Patch_DeinitAndRemoveMap_ClampCurrentMapIndex
    {
        // After vanilla reindexes, force a bounds check in case nested pocket
        // teardown left a stale value mid-cascade.
        public static void Postfix(Game __instance)
        {
            Patch_CurrentMapIndex.ClampIfNeeded(__instance);
        }
    }
}

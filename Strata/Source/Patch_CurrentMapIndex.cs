using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Vanilla Game.CurrentMap is `maps[currentMapIndex]` with no upper bound check.
    // Destroying / generating pocket levels can leave the index past maps.Count;
    // every MapUpdate then throws ArgumentOutOfRangeException (parameter: index)
    // and the view flickers until CurrentMap is assigned again (e.g. surface).
    [HarmonyPatch(typeof(Game), "get_CurrentMap")]
    public static class Patch_CurrentMapIndex
    {
        private static readonly FieldInfo CurrentMapIndexField =
            AccessTools.Field(typeof(Game), "currentMapIndex");

        private static readonly FieldInfo MapsField =
            AccessTools.Field(typeof(Game), "maps");

        private static bool warnedThisSession;

        public static bool Prefix(Game __instance, ref Map __result)
        {
            if (CurrentMapIndexField == null || MapsField == null)
            {
                return true;
            }

            sbyte index = (sbyte)CurrentMapIndexField.GetValue(__instance);
            if (index < 0)
            {
                __result = null;
                return false;
            }

            var maps = (List<Map>)MapsField.GetValue(__instance);
            if (maps == null || maps.Count == 0)
            {
                CurrentMapIndexField.SetValue(__instance, (sbyte)(-1));
                __result = null;
                return false;
            }

            if (index < maps.Count)
            {
                return true;
            }

            sbyte repaired = 0;
            CurrentMapIndexField.SetValue(__instance, repaired);
            __result = maps[repaired];
            if (!warnedThisSession)
            {
                warnedThisSession = true;
                Log.Warning("[Strata] CurrentMap index was out of range (index="
                    + index + ", maps=" + maps.Count
                    + "); clamped to map 0. Further repairs this session are silent.");
            }
            return false;
        }
    }
}

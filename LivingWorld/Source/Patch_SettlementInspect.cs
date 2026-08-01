using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetInspectString))]
    public static class Patch_Settlement_GetInspectString
    {
        public static void Postfix(Settlement __instance, ref string __result)
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            SettlementMood mood = comp?.TryGetMood(__instance);
            if (mood == null)
            {
                return;
            }

            string line = mood.ProsperityLabelKey().Translate();
            if (!string.IsNullOrEmpty(mood.epithet))
            {
                line = line + " · " + mood.epithet.Translate();
            }

            if (string.IsNullOrEmpty(__result))
            {
                __result = line;
            }
            else
            {
                __result = __result + "\n" + line;
            }
        }
    }

    [HarmonyPatch(typeof(Settlement), "get_Label")]
    public static class Patch_Settlement_Label
    {
        public static void Postfix(Settlement __instance, ref string __result)
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            SettlementMood mood = comp?.TryGetMood(__instance);
            if (mood == null || string.IsNullOrEmpty(mood.epithet))
            {
                return;
            }
            __result = __result + " (" + mood.epithet.Translate() + ")";
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    [HarmonyPatch]
    public static class Patch_TryAffectGoodwillWith_FamilyJoin
    {
        public static bool Prepare()
        {
            foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(Faction)))
            {
                if (m.Name == "TryAffectGoodwillWith") return true;
            }
            return false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(Faction)))
            {
                if (m.Name == "TryAffectGoodwillWith")
                    yield return m;
            }
        }

        public static bool Prefix()
        {
            return !FamilyJoinUtility.SuppressGoodwillPenalty;
        }
    }
}

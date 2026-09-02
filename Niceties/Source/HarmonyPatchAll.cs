using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Niceties
{
    internal static class HarmonyPatchAll
    {
        internal static void Apply(Harmony harmony, string logPrefix)
        {
            foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                if (!IsHarmonyPatchClass(type))
                {
                    continue;
                }

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    Log.Error(logPrefix + " Harmony patch class " + type.Name + " failed: " + e.Message);
                }
            }
        }

        /// <summary>
        /// CreateClassProcessor treats any type with Prefix/Postfix/Cleanup as a patch
        /// class. Skip types that do not actually have [HarmonyPatch].
        /// </summary>
        private static bool IsHarmonyPatchClass(Type type)
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length > 0)
            {
                return true;
            }

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance
                | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

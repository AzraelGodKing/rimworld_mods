using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Marks a static parameterless method that must run on Game.FinalizeInit so
    // tick-stamped / ID-keyed statics from a previously loaded save cannot leak.
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class StrataSessionResetAttribute : Attribute
    {
    }

    public static class StrataSessionReset
    {
        public static void Run()
        {
            Assembly asm = typeof(StrataSessionResetAttribute).Assembly;
            foreach (Type type in AccessTools.GetTypesFromAssembly(asm))
            {
                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }
                catch
                {
                    continue;
                }
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.GetCustomAttribute<StrataSessionResetAttribute>() == null)
                    {
                        continue;
                    }
                    if (method.GetParameters().Length != 0)
                    {
                        continue;
                    }
                    try
                    {
                        method.Invoke(null, null);
                    }
                    catch (Exception e)
                    {
                        Log.Warning("[Strata] Session reset failed on "
                            + type.FullName + "." + method.Name + ": " + e);
                    }
                }
            }
        }
    }
}

using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    internal static class ReflectionUtil
    {
        internal static Assembly FindAssembly(string assemblyNamePart)
        {
            foreach (ModContentPack pack in LoadedModManager.RunningMods)
            {
                if (pack.assemblies?.loadedAssemblies == null)
                {
                    continue;
                }
                foreach (Assembly asm in pack.assemblies.loadedAssemblies)
                {
                    if (asm.GetName().Name.IndexOf(assemblyNamePart, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return asm;
                    }
                }
            }
            return null;
        }

        internal static Type TypeIn(string typeName, Assembly asm = null)
        {
            Type t = AccessTools.TypeByName(typeName);
            if (t != null)
            {
                return t;
            }
            if (asm == null)
            {
                return null;
            }
            return asm.GetType(typeName, throwOnError: false);
        }

        internal static object GetMember(object target, string name)
        {
            if (target == null)
            {
                return null;
            }
            Type type = target.GetType();
            PropertyInfo prop = AccessTools.Property(type, name);
            if (prop != null)
            {
                return prop.GetValue(target, null);
            }
            FieldInfo field = AccessTools.Field(type, name);
            return field?.GetValue(target);
        }

        internal static float GetFloat(object target, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                object val = GetMember(target, names[i]);
                if (val is float f)
                {
                    return f;
                }
                if (val is double d)
                {
                    return (float)d;
                }
                if (val is int n)
                {
                    return n;
                }
            }
            return 0f;
        }

        internal static void InvokeVoid(object target, string methodName, params object[] args)
        {
            if (target == null)
            {
                return;
            }
            MethodInfo method = AccessTools.Method(target.GetType(), methodName);
            method?.Invoke(target, args);
        }
    }
}

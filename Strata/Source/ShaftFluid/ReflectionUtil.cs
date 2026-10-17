using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    internal static class ReflectionUtil
    {
        private struct MemberKey : IEquatable<MemberKey>
        {
            public Type type;
            public string name;

            public bool Equals(MemberKey other)
            {
                return type == other.type && name == other.name;
            }

            public override bool Equals(object obj)
            {
                return obj is MemberKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((type != null ? type.GetHashCode() : 0) * 397)
                        ^ (name != null ? name.GetHashCode() : 0);
                }
            }
        }

        private struct Getter
        {
            public bool missing;
            public Func<object, object> read;
        }

        private static readonly Dictionary<MemberKey, Getter> getters = new Dictionary<MemberKey, Getter>();
        private static readonly Dictionary<MemberKey, MethodInfo> methods = new Dictionary<MemberKey, MethodInfo>();

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
            if (target == null || name == null)
            {
                return null;
            }
            Getter getter = GetterFor(target.GetType(), name);
            if (getter.missing || getter.read == null)
            {
                return null;
            }
            return getter.read(target);
        }

        internal static float GetFloat(object target, params string[] names)
        {
            if (names == null)
            {
                return 0f;
            }
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
            if (target == null || methodName == null)
            {
                return;
            }
            MethodInfo method = MethodFor(target.GetType(), methodName);
            if (method == null)
            {
                return;
            }
            method.Invoke(target, args);
        }

        private static Getter GetterFor(Type type, string name)
        {
            var key = new MemberKey { type = type, name = name };
            if (getters.TryGetValue(key, out Getter cached))
            {
                return cached;
            }
            Getter bound = BindGetter(type, name);
            getters[key] = bound;
            return bound;
        }

        private static Getter BindGetter(Type type, string name)
        {
            PropertyInfo prop = AccessTools.Property(type, name);
            if (prop != null)
            {
                MethodInfo getter = prop.GetGetMethod(nonPublic: true);
                if (getter != null && !prop.PropertyType.IsValueType)
                {
                    try
                    {
                        Func<object, object> typed = AccessTools.MethodDelegate<Func<object, object>>(getter);
                        return new Getter { read = typed };
                    }
                    catch (Exception)
                    {
                        // Fall through to the cached PropertyInfo path.
                    }
                }
                return new Getter { read = target => prop.GetValue(target, null) };
            }

            FieldInfo field = AccessTools.Field(type, name);
            if (field == null)
            {
                return new Getter { missing = true };
            }
            if (!field.FieldType.IsValueType)
            {
                try
                {
                    AccessTools.FieldRef<object, object> fieldRef = AccessTools.FieldRefAccess<object, object>(field);
                    return new Getter { read = target => fieldRef(target) };
                }
                catch (Exception)
                {
                    // Fall through to FieldInfo.GetValue.
                }
            }
            return new Getter { read = field.GetValue };
        }

        private static MethodInfo MethodFor(Type type, string name)
        {
            var key = new MemberKey { type = type, name = name };
            if (methods.TryGetValue(key, out MethodInfo cached))
            {
                return cached;
            }
            MethodInfo method = AccessTools.Method(type, name);
            methods[key] = method;
            return method;
        }
    }
}

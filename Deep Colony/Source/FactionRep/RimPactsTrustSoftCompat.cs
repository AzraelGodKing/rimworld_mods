using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Fail-open RimPacts trust. No project reference.
    /// RimPacts.WorldComponent_RimPacts.OffsetTrust(Faction, int, string).
    /// </summary>
    [StaticConstructorOnStartup]
    public static class RimPactsTrustSoftCompat
    {
        private static MethodInfo offsetTrust;
        private static object offsetTarget;
        private static bool scanned;
        private static bool logged;

        static RimPactsTrustSoftCompat()
        {
            if (!SoftCompat.RimPactsLoaded) return;
            Log.Message("[DeepColony] RimPacts Diplomacy present (trust hook on first event).");
        }

        public static void Notify(Faction faction, int trustDelta)
        {
            if (!SoftCompat.RimPactsLoaded) return;
            if (!DeepColonySettings.Get.enableDiplomacyCompat) return;
            if (faction == null || faction.IsPlayer || trustDelta == 0) return;
            if (!EnsureResolved()) return;
            try
            {
                ParameterInfo[] p = offsetTrust.GetParameters();
                object instance = offsetTrust.IsStatic ? null : offsetTarget;
                var args = new object[p.Length];
                int filled = 0;
                for (int i = 0; i < p.Length; i++)
                {
                    Type t = p[i].ParameterType;
                    if (typeof(Faction).IsAssignableFrom(t) && filled == 0)
                    {
                        args[i] = faction;
                        filled = 1;
                    }
                    else if ((t == typeof(int) || t == typeof(float)) && filled == 1)
                    {
                        args[i] = t == typeof(float) ? (object)(float)trustDelta : trustDelta;
                        filled = 2;
                    }
                    else if (t == typeof(string))
                    {
                        args[i] = "DeepColony";
                    }
                    else if (t == typeof(bool))
                    {
                        args[i] = false;
                    }
                    else if (t.IsValueType)
                    {
                        args[i] = Activator.CreateInstance(t);
                    }
                    else
                    {
                        args[i] = null;
                    }
                }
                offsetTrust.Invoke(instance, args);
            }
            catch (Exception e)
            {
                if (logged) return;
                logged = true;
                Log.Warning("[DeepColony] RimPacts OffsetTrust threw: " + e.Message);
            }
        }

        private static bool EnsureResolved()
        {
            if (!scanned)
            {
                scanned = true;
                TryFindMethod();
            }
            if (offsetTrust == null) return false;
            if (offsetTrust.IsStatic) return true;
            if (TryBindWorldComponent()) return true;
            return false;
        }

        private static void TryFindMethod()
        {
            try
            {
                Type hint = AccessTools.TypeByName("RimPacts.WorldComponent_RimPacts");
                if (TryBindOffsetTrust(hint)) return;

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name != "RimPacts") continue;
                    Type[] types;
                    try
                    {
                        types = asm.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        types = e.Types;
                    }
                    if (types == null) continue;
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (TryBindOffsetTrust(types[i])) return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] RimPacts trust resolve failed: " + e.Message);
            }
        }

        private static bool TryBindOffsetTrust(Type t)
        {
            if (t == null) return false;
            MethodInfo m = AccessTools.DeclaredMethod(t, "OffsetTrust");
            if (m == null) return false;
            ParameterInfo[] p = m.GetParameters();
            if (p.Length < 2) return false;
            if (!typeof(Faction).IsAssignableFrom(p[0].ParameterType)) return false;
            offsetTrust = m;
            Log.Message("[DeepColony] RimPacts trust hook ready (" + t.FullName + ").");
            return true;
        }

        private static bool TryBindWorldComponent()
        {
            Type t = offsetTrust.DeclaringType;
            World world = Find.World;
            if (world?.components == null || t == null) return false;
            if (offsetTarget != null)
            {
                WorldComponent existing = offsetTarget as WorldComponent;
                if (existing != null && world.components.Contains(existing))
                    return true;
                offsetTarget = null;
            }
            for (int c = 0; c < world.components.Count; c++)
            {
                WorldComponent comp = world.components[c];
                if (comp != null && t.IsInstanceOfType(comp))
                {
                    offsetTarget = comp;
                    return true;
                }
            }
            return false;
        }
    }
}

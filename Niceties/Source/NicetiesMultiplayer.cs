using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Niceties
{
    /// <summary>
    /// Soft Multiplayer bridge by reflection (no Multiplayer.API dependency). When the
    /// Multiplayer mod is absent everything here is a no-op.
    /// </summary>
    internal static class NicetiesMultiplayer
    {
        private const string MultiplayerId = "rwmt.Multiplayer";

        private static bool resolved;
        private static bool loaded;
        private static PropertyInfo isInMultiplayer;
        private static MethodInfo registerSyncMethod;

        internal static bool Loaded
        {
            get
            {
                Resolve();
                return loaded;
            }
        }

        /// <summary>
        /// True inside a Multiplayer session. If the mod is loaded but the session flag
        /// cannot be read, report true so the save's host snapshot is kept.
        /// </summary>
        internal static bool InSession
        {
            get
            {
                Resolve();
                if (!loaded)
                {
                    return false;
                }

                if (isInMultiplayer == null)
                {
                    return true;
                }

                try
                {
                    return (bool)isInMultiplayer.GetValue(null, null);
                }
                catch
                {
                    return true;
                }
            }
        }

        internal static void RegisterSyncMethod(Type type, string methodName)
        {
            Resolve();
            if (!loaded || registerSyncMethod == null)
            {
                return;
            }

            MethodInfo target = AccessTools.Method(type, methodName);
            if (target == null)
            {
                Log.Warning("[Niceties] Multiplayer sync target " + type.Name + "." + methodName + " not found.");
                return;
            }

            try
            {
                registerSyncMethod.Invoke(null, new object[] { target, null });
            }
            catch (Exception e)
            {
                Log.Warning("[Niceties] Multiplayer sync registration failed for " + type.Name + "." + methodName
                    + ": " + (e.InnerException ?? e).Message);
            }
        }

        private static void Resolve()
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            loaded = ModLister.GetActiveModWithIdentifier(MultiplayerId, ignorePostfix: true) != null;
            if (!loaded)
            {
                return;
            }

            Type mp = AccessTools.TypeByName("Multiplayer.API.MP");
            if (mp == null)
            {
                Log.Warning("[Niceties] Multiplayer is active but Multiplayer.API.MP was not found; "
                    + "save settings stay host-baked and the share-room gizmo is not synced.");
                return;
            }

            FieldInfo enabled = AccessTools.Field(mp, "enabled");
            if (enabled != null && enabled.GetValue(null) is bool on && !on)
            {
                loaded = false;
                return;
            }

            isInMultiplayer = AccessTools.Property(mp, "IsInMultiplayer");
            foreach (MethodInfo m in mp.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name != "RegisterSyncMethod")
                {
                    continue;
                }

                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length == 2 && ps[0].ParameterType == typeof(MethodInfo))
                {
                    registerSyncMethod = m;
                    break;
                }
            }
        }
    }
}

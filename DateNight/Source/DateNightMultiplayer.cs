#nullable enable
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Soft Multiplayer support by reflection (AZR-560); no Multiplayer API DLL is
    /// shipped or referenced. Every UI action that changes the game goes through
    /// one of the methods registered here, so Multiplayer runs it on all clients.
    /// Without Multiplayer this is a no-op.
    /// Mod settings are not synced: every player needs the host's Date Night
    /// settings (see COMPATIBILITY.md).
    /// </summary>
    internal static class DateNightMultiplayer
    {
        private const string MultiplayerId = "rwmt.Multiplayer";

        internal static void RegisterAll()
        {
            MethodInfo? register = ResolveRegister();
            if (register == null)
            {
                return;
            }
            Register(register, typeof(DateNightAskOut), nameof(DateNightAskOut.Plan));
            Register(register, typeof(DateNightAskOut), nameof(DateNightAskOut.Cancel));
            Register(register, typeof(DateNightBaby), nameof(DateNightBaby.SetTrying));
            // Mismatch alert "copy hours" option edits two timetables.
            Register(register, typeof(DateNightUtility), nameof(DateNightUtility.CopyHours));
        }

        private static MethodInfo? ResolveRegister()
        {
            if (ModLister.GetActiveModWithIdentifier(MultiplayerId, ignorePostfix: true) == null)
            {
                return null;
            }
            Type? mp = AccessTools.TypeByName("Multiplayer.API.MP");
            if (mp == null)
            {
                Log.Warning("[DateNight] Multiplayer is active but Multiplayer.API.MP was not found; "
                    + "Date Night's buttons are not synced.");
                return null;
            }
            FieldInfo? enabled = AccessTools.Field(mp, "enabled");
            if (enabled != null && enabled.GetValue(null) is bool on && !on)
            {
                return null;
            }
            foreach (MethodInfo m in mp.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] ps = m.GetParameters();
                if (m.Name == "RegisterSyncMethod" && ps.Length == 2 && ps[0].ParameterType == typeof(MethodInfo))
                {
                    return m;
                }
            }
            Log.Warning("[DateNight] Multiplayer.API.MP.RegisterSyncMethod(MethodInfo, ...) not found; "
                + "Date Night's buttons are not synced.");
            return null;
        }

        private static void Register(MethodInfo register, Type type, string methodName)
        {
            MethodInfo? target = AccessTools.Method(type, methodName);
            if (target == null)
            {
                Log.Warning("[DateNight] Multiplayer sync target " + type.Name + "." + methodName + " not found.");
                return;
            }
            try
            {
                register.Invoke(null, new object?[] { target, null });
            }
            catch (Exception e)
            {
                Log.Warning("[DateNight] Multiplayer sync registration failed for " + type.Name + "." + methodName
                    + ": " + (e.InnerException ?? e).Message);
            }
        }
    }
}

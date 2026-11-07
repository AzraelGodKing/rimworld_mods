#nullable enable
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Azrael
{
    /// <summary>
    /// Soft Multiplayer support by reflection; no Multiplayer API DLL is shipped or
    /// referenced. Safe-removal Deconstruct is Azrael's only game-changing action and
    /// goes through <see cref="AzraelPlayerCommand.DesignateDeconstruct"/>, registered
    /// here so Multiplayer runs it on every client. Without Multiplayer this is a no-op.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class AzraelMultiplayer
    {
        private const string MultiplayerId = "rwmt.Multiplayer";

        static AzraelMultiplayer()
        {
            try
            {
                MethodInfo? register = ResolveRegister();
                if (register != null)
                {
                    Register(register, typeof(AzraelPlayerCommand), nameof(AzraelPlayerCommand.DesignateDeconstruct));
                }
            }
            catch (Exception e)
            {
                Log.Warning("[Azrael] Multiplayer sync setup failed: " + e.Message);
            }
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
                Log.Warning("[Azrael] Multiplayer is active but Multiplayer.API.MP was not found; "
                    + "safe-removal Deconstruct is not synced.");
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
            Log.Warning("[Azrael] Multiplayer.API.MP.RegisterSyncMethod(MethodInfo, ...) not found; "
                + "safe-removal Deconstruct is not synced.");
            return null;
        }

        private static void Register(MethodInfo register, Type type, string methodName)
        {
            MethodInfo? target = AccessTools.Method(type, methodName);
            if (target == null)
            {
                Log.Warning("[Azrael] Multiplayer sync target " + type.Name + "." + methodName + " not found.");
                return;
            }
            try
            {
                register.Invoke(null, new object?[] { target, null });
            }
            catch (Exception e)
            {
                Log.Warning("[Azrael] Multiplayer sync registration failed for " + type.Name + "." + methodName
                    + ": " + (e.InnerException ?? e).Message);
            }
        }
    }
}

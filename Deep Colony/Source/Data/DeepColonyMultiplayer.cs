using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Soft Multiplayer support by reflection (AZR-629); no Multiplayer API DLL is
    /// shipped or referenced. Every player action that changes the game goes through
    /// <see cref="DeepColonyPlayerCommand.Submit"/>, registered here so Multiplayer runs
    /// it on all clients. Pawn jobs (counselling) are already synced by Multiplayer.
    /// Without Multiplayer this is a no-op.
    /// Mod settings are not synced: every player needs the host's Deep Colony settings.
    /// </summary>
    internal static class DeepColonyMultiplayer
    {
        private const string MultiplayerId = "rwmt.Multiplayer";

        internal static void RegisterAll()
        {
            MethodInfo register = ResolveRegister();
            if (register == null)
            {
                return;
            }
            Register(register, typeof(DeepColonyPlayerCommand), nameof(DeepColonyPlayerCommand.Submit));
        }

        private static MethodInfo ResolveRegister()
        {
            if (ModLister.GetActiveModWithIdentifier(MultiplayerId, ignorePostfix: true) == null)
            {
                return null;
            }
            Type mp = AccessTools.TypeByName("Multiplayer.API.MP");
            if (mp == null)
            {
                Log.Warning("[DeepColony] Multiplayer is active but Multiplayer.API.MP was not found; "
                    + "Deep Colony's buttons are not synced.");
                return null;
            }
            FieldInfo enabled = AccessTools.Field(mp, "enabled");
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
            Log.Warning("[DeepColony] Multiplayer.API.MP.RegisterSyncMethod(MethodInfo, ...) not found; "
                + "Deep Colony's buttons are not synced.");
            return null;
        }

        private static void Register(MethodInfo register, Type type, string methodName)
        {
            MethodInfo target = AccessTools.Method(type, methodName);
            if (target == null)
            {
                Log.Warning("[DeepColony] Multiplayer sync target " + type.Name + "." + methodName + " not found.");
                return;
            }
            try
            {
                register.Invoke(null, new object[] { target, null });
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Multiplayer sync registration failed for " + type.Name + "." + methodName
                    + ": " + (e.InnerException ?? e).Message);
            }
        }
    }
}

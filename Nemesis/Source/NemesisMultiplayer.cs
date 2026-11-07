#nullable enable
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Soft Multiplayer support by reflection (AZR-553); no Multiplayer API DLL is shipped
    /// or referenced. Every player action that changes a hunt goes through
    /// <see cref="NemesisPlayerCommand"/>, registered here so Multiplayer runs it on all
    /// clients. Comms console replies are a job, which Multiplayer already syncs.
    /// Without Multiplayer this is a no-op. Mod settings are not synced: every player needs
    /// the host's Nemesis settings (see COMPATIBILITY.md).
    /// </summary>
    internal static class NemesisMultiplayer
    {
        private const string MultiplayerId = "rwmt.Multiplayer";

        internal static void RegisterAll()
        {
            MethodInfo? register = ResolveRegister();
            if (register == null)
                return;
            Register(register, nameof(NemesisPlayerCommand.BuyLead));
            Register(register, nameof(NemesisPlayerCommand.PostBounty));
            Register(register, nameof(NemesisPlayerCommand.Reply));
            Register(register, nameof(NemesisPlayerCommand.Resolve));
        }

        private static MethodInfo? ResolveRegister()
        {
            if (ModLister.GetActiveModWithIdentifier(MultiplayerId, ignorePostfix: true) == null)
                return null;
            Type? mp = AccessTools.TypeByName("Multiplayer.API.MP");
            if (mp == null)
            {
                Log.Warning("[Nemesis] Multiplayer is active but Multiplayer.API.MP was not found; "
                    + "Nemesis's buttons are not synced.");
                return null;
            }
            FieldInfo? enabled = AccessTools.Field(mp, "enabled");
            if (enabled != null && enabled.GetValue(null) is bool on && !on)
                return null;
            foreach (MethodInfo m in mp.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] ps = m.GetParameters();
                if (m.Name == "RegisterSyncMethod" && ps.Length == 2 && ps[0].ParameterType == typeof(MethodInfo))
                    return m;
            }
            Log.Warning("[Nemesis] Multiplayer.API.MP.RegisterSyncMethod(MethodInfo, ...) not found; "
                + "Nemesis's buttons are not synced.");
            return null;
        }

        private static void Register(MethodInfo register, string methodName)
        {
            MethodInfo? target = AccessTools.Method(typeof(NemesisPlayerCommand), methodName);
            if (target == null)
            {
                Log.Warning("[Nemesis] Multiplayer sync target NemesisPlayerCommand." + methodName + " not found.");
                return;
            }
            try
            {
                register.Invoke(null, new object?[] { target, null });
            }
            catch (Exception e)
            {
                Log.Warning("[Nemesis] Multiplayer sync registration failed for " + methodName + ": "
                    + (e.InnerException ?? e).Message);
            }
        }
    }
}

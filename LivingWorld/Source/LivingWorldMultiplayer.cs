using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Soft Multiplayer support by reflection; no Multiplayer API DLL is shipped or
    /// referenced. The simulation runs on the settings saved with the game, and every
    /// UI action that changes the game goes through a method registered here, so
    /// Multiplayer runs it on all clients. Without Multiplayer this is a no-op.
    /// </summary>
    internal static class LivingWorldMultiplayer
    {
        private const string MultiplayerId = "rwmt.Multiplayer";

        private static PropertyInfo inMultiplayer;
        private static PropertyInfo isHosting;

        internal static void RegisterAll()
        {
            MethodInfo register = ResolveRegister();
            if (register == null)
            {
                return;
            }
            Register(register, typeof(LivingWorldMultiplayer), nameof(ApplySettings));
            Register(register, typeof(LivingWorldDebugCommand), nameof(LivingWorldDebugCommand.Enqueue));
            Register(register, typeof(LivingWorldRemoval), nameof(LivingWorldRemoval.Run));
        }

        /// <summary>True while in a Multiplayer session where this client is not the host.</summary>
        internal static bool IsClientNotHost
        {
            get
            {
                try
                {
                    return inMultiplayer?.GetValue(null) is bool inMp && inMp
                        && isHosting?.GetValue(null) is bool host && !host;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>After Mod Options close in a game: copy the player's settings into the game.</summary>
        internal static void PushSettingsToGame()
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp?.SimSettings == null || LivingWorldMod.Global == null
                || Current.ProgramState != ProgramState.Playing)
            {
                return;
            }
            string payload = LivingWorldMod.Global.Serialize();
            if (payload == comp.SimSettings.Serialize())
            {
                return;
            }
            if (IsClientNotHost)
            {
                Messages.Message("LivingWorld_Settings_HostOnly".Translate(), MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }
            ApplySettings(payload);
        }

        /// <summary>Sync method: replaces the game's sim settings on every client.</summary>
        public static void ApplySettings(string payload)
        {
            GameComponent_LivingWorld.Get?.SimSettings?.ReadFrom(payload);
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
                Log.Warning("[LivingWorld] Multiplayer is active but Multiplayer.API.MP was not found; "
                    + "Living World's settings and debug actions are not synced.");
                return null;
            }
            FieldInfo enabled = AccessTools.Field(mp, "enabled");
            if (enabled != null && enabled.GetValue(null) is bool on && !on)
            {
                return null;
            }
            inMultiplayer = AccessTools.Property(mp, "IsInMultiplayer");
            isHosting = AccessTools.Property(mp, "IsHosting");
            foreach (MethodInfo m in mp.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] ps = m.GetParameters();
                if (m.Name == "RegisterSyncMethod" && ps.Length == 2 && ps[0].ParameterType == typeof(MethodInfo))
                {
                    return m;
                }
            }
            Log.Warning("[LivingWorld] Multiplayer.API.MP.RegisterSyncMethod(MethodInfo, ...) not found; "
                + "Living World's settings and debug actions are not synced.");
            return null;
        }

        private static void Register(MethodInfo register, Type type, string methodName)
        {
            MethodInfo target = AccessTools.Method(type, methodName);
            if (target == null)
            {
                Log.Warning("[LivingWorld] Multiplayer sync target " + type.Name + "." + methodName + " not found.");
                return;
            }
            try
            {
                register.Invoke(null, new object[] { target, null });
            }
            catch (Exception e)
            {
                Log.Warning("[LivingWorld] Multiplayer sync registration failed for " + type.Name + "." + methodName
                    + ": " + (e.InnerException ?? e).Message);
            }
        }
    }
}

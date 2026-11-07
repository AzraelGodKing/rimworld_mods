using System;
using System.Reflection;
using Verse;

namespace NanoHiveReforged
{
    /// <summary>
    /// Soft Multiplayer support by reflection; no Multiplayer API DLL is shipped
    /// or referenced, and NanoHive has no Harmony. Every hive button changes the
    /// game only through the methods registered here, so Multiplayer runs them on
    /// all clients. Gameplay values live in the save, so local mod settings never
    /// change how a hive ticks. Without Multiplayer this is a no-op.
    /// </summary>
    internal static class NanoHiveMultiplayer
    {
        private const string MultiplayerId = "rwmt.Multiplayer";

        internal static readonly string[] SyncMethods =
        {
            nameof(Building_NanoHiveReforged.SetToggle),
            nameof(Building_NanoHiveReforged.SetRange),
            nameof(Building_NanoHiveReforged.SetRepairRate),
            nameof(Building_NanoHiveReforged.MakeColonyDefault),
            nameof(Building_NanoHiveReforged.CopyToAllHives),
        };

        internal static void RegisterAll()
        {
            MethodInfo register = ResolveRegister();
            if (register == null)
            {
                return;
            }
            foreach (string name in SyncMethods)
            {
                MethodInfo target = typeof(Building_NanoHiveReforged).GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
                if (target == null)
                {
                    Log.Warning("[NanoHive Reforged] Multiplayer sync target " + name + " not found.");
                    continue;
                }
                try
                {
                    register.Invoke(null, new object[] { target, null });
                }
                catch (Exception e)
                {
                    Log.Warning("[NanoHive Reforged] Multiplayer sync registration failed for " + name + ": "
                        + (e.InnerException ?? e).Message);
                }
            }
        }

        private static MethodInfo ResolveRegister()
        {
            if (ModLister.GetActiveModWithIdentifier(MultiplayerId, ignorePostfix: true) == null)
            {
                return null;
            }
            Type mp = GenTypes.GetTypeInAnyAssembly("Multiplayer.API.MP");
            if (mp == null)
            {
                Log.Warning("[NanoHive Reforged] Multiplayer is active but Multiplayer.API.MP was not found; hive buttons are not synced.");
                return null;
            }
            FieldInfo enabled = mp.GetField("enabled", BindingFlags.Public | BindingFlags.Static);
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
            Log.Warning("[NanoHive Reforged] Multiplayer.API.MP.RegisterSyncMethod(MethodInfo, ...) not found; hive buttons are not synced.");
            return null;
        }
    }

    [StaticConstructorOnStartup]
    internal static class NanoHiveInit
    {
        static NanoHiveInit()
        {
            NanoHiveMultiplayer.RegisterAll();
        }
    }
}

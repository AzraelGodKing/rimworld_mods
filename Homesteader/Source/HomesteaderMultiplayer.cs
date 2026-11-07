#nullable enable
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Every UI action that changes the game goes through one of these methods
    /// (AZR-527). Multiplayer, when present, runs them on all clients; without it
    /// they are plain calls. Parameters are only types Multiplayer can sync.
    /// </summary>
    public static class HomesteaderSync
    {
        /// <summary>Settings window, on close: the save's gameplay settings.</summary>
        public static void SetGameplay(bool spoilageTriage, float allergyFlareIntensity, float favoriteFoodMoodFactor,
            float coopEggIntervalFactor, bool enableKatsEffect, bool larderMood, bool rainAwareWater, bool beesNeedBloom)
        {
            GameComponent_HomesteaderGameplay? save = GameComponent_HomesteaderGameplay.Instance;
            if (save == null)
            {
                return;
            }

            HomesteaderGameplaySettings next = save.Values.Copy();
            next.spoilageTriage = spoilageTriage;
            next.allergyFlareIntensity = allergyFlareIntensity;
            next.favoriteFoodMoodFactor = favoriteFoodMoodFactor;
            next.coopEggIntervalFactor = coopEggIntervalFactor;
            next.enableKatsEffect = enableKatsEffect;
            next.larderMood = larderMood;
            next.rainAwareWater = rainAwareWater;
            next.beesNeedBloom = beesNeedBloom;
            save.Set(next);
        }

        /// <summary>Tastes tab god-mode button.</summary>
        public static void RerollTastes(Pawn pawn)
        {
            FavoriteFoodUtility.Comp?.RerollTastes(pawn);
        }

        /// <summary>Pantry "Add bill" button. The camera jump stays local.</summary>
        public static void AddBill(RecipeDef recipe, Thing table)
        {
            if (recipe != null && table is Building_WorkTable workTable && workTable.Spawned && workTable.billStack != null)
            {
                workTable.billStack.AddBill(new Bill_Production(recipe));
            }
        }
    }

    /// <summary>
    /// Soft Multiplayer support by reflection, the Date Night pattern: no
    /// Multiplayer API DLL is shipped or referenced, and without Multiplayer this
    /// does nothing. Mod settings are not synced, but gameplay settings live in the
    /// save, so clients run the host's values.
    /// </summary>
    internal static class HomesteaderMultiplayer
    {
        private const string MultiplayerId = "rwmt.Multiplayer";

        internal static void RegisterAll()
        {
            MethodInfo? register = ResolveRegister();
            if (register == null)
            {
                return;
            }

            Register(register, nameof(HomesteaderSync.SetGameplay));
            Register(register, nameof(HomesteaderSync.RerollTastes));
            Register(register, nameof(HomesteaderSync.AddBill));
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
                Log.Warning("[Homesteader] Multiplayer is active but Multiplayer.API.MP was not found; "
                    + "Homesteader's buttons are not synced.");
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

            Log.Warning("[Homesteader] Multiplayer.API.MP.RegisterSyncMethod(MethodInfo, ...) not found; "
                + "Homesteader's buttons are not synced.");
            return null;
        }

        private static void Register(MethodInfo register, string methodName)
        {
            MethodInfo? target = AccessTools.Method(typeof(HomesteaderSync), methodName);
            if (target == null)
            {
                Log.Warning("[Homesteader] Multiplayer sync target HomesteaderSync." + methodName + " not found.");
                return;
            }

            try
            {
                register.Invoke(null, new object?[] { target, null });
            }
            catch (Exception e)
            {
                Log.Warning("[Homesteader] Multiplayer sync registration failed for HomesteaderSync." + methodName
                    + ": " + (e.InnerException ?? e).Message);
            }
        }
    }
}

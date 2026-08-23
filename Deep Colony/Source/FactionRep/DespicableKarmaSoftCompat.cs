using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Fail-open Despicable 2 Hero Karma. No project reference.
    /// Uses HeroKarmaBridge.ApplyOutcome when a hero is assigned.
    /// Skips execute/release — Despicable already patches those.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class DespicableKarmaSoftCompat
    {
        private static readonly MethodInfo applyOutcome;
        private static bool logged;

        static DespicableKarmaSoftCompat()
        {
            if (!SoftCompat.Despicable2Loaded) return;
            try
            {
                Type bridge = AccessTools.TypeByName("Despicable.HeroKarma.HeroKarmaBridge");
                if (bridge == null) return;
                foreach (MethodInfo m in AccessTools.GetDeclaredMethods(bridge))
                {
                    if (m == null || m.Name != "ApplyOutcome" || !m.IsStatic) continue;
                    ParameterInfo[] p = m.GetParameters();
                    if (p.Length < 8) continue;
                    if (p[0].ParameterType != typeof(int) || p[1].ParameterType != typeof(int)) continue;
                    applyOutcome = m;
                    break;
                }
                if (applyOutcome != null)
                    Log.Message("[DeepColony] Despicable 2 Hero Karma hook ready (fail-open).");
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Despicable 2 karma hook failed: " + e.Message);
            }
        }

        public static void Notify(int karmaDelta, int standingDelta, string eventKey, string label, Pawn target, Faction faction)
        {
            if (applyOutcome == null) return;
            if (!DeepColonySettings.Get.enableDiplomacyCompat) return;
            if (karmaDelta == 0 && standingDelta == 0) return;
            try
            {
                ParameterInfo[] p = applyOutcome.GetParameters();
                var args = new object[p.Length];
                args[0] = karmaDelta;
                args[1] = standingDelta;
                args[2] = eventKey ?? "DeepColony";
                args[3] = label ?? eventKey;
                args[4] = label ?? "";
                args[5] = label ?? "";
                if (p.Length > 6) args[6] = standingDelta != 0 ? (label ?? "") : "";
                // tokens
                if (p.Length > 7) args[7] = null;
                if (p.Length > 8) args[8] = target?.GetUniqueLoadID();
                if (p.Length > 9) args[9] = faction?.loadID ?? 0;
                applyOutcome.Invoke(null, args);
            }
            catch (Exception e)
            {
                if (logged) return;
                logged = true;
                Log.Warning("[DeepColony] Despicable 2 ApplyOutcome threw: " + e.Message);
            }
        }
    }
}

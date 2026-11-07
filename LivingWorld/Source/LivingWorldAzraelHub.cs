using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Soft link to the Azrael storyteller hub. When it is loaded, its per-storyteller
    /// <c>Azrael.StorytellerIncidentWeights.TrafficFactor(string)</c> scales NPC caravan traffic.
    /// Without it (or on any error) every factor is 1, so Living World plays the same alone.
    /// </summary>
    public static class LivingWorldAzraelHub
    {
        public const string SpawnChanceKey = "spawnChance";
        public const string DivertChanceKey = "divertChance";

        private const float MaxFactor = 4f;

        private static bool resolved;
        private static Func<string, float> trafficFactor;
        private static bool warned;

        public static float TrafficFactor(string key)
        {
            Func<string, float> getter = Resolve();
            if (getter == null)
            {
                return 1f;
            }
            try
            {
                float f = getter(key);
                if (float.IsNaN(f) || float.IsInfinity(f))
                {
                    return 1f;
                }
                return Mathf.Clamp(f, 0f, MaxFactor);
            }
            catch (Exception e)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning("[LivingWorld] Azrael TrafficFactor failed, using 1: " + e.Message);
                }
                return 1f;
            }
        }

        private static Func<string, float> Resolve()
        {
            if (resolved)
            {
                return trafficFactor;
            }
            resolved = true;
            try
            {
                Type t = AccessTools.TypeByName("Azrael.StorytellerIncidentWeights");
                MethodInfo m = t == null
                    ? null
                    : AccessTools.Method(t, "TrafficFactor", new[] { typeof(string) });
                if (m != null && m.IsStatic && m.ReturnType == typeof(float))
                {
                    trafficFactor = (Func<string, float>)Delegate.CreateDelegate(typeof(Func<string, float>), m);
                }
            }
            catch (Exception e)
            {
                Log.Warning("[LivingWorld] Could not bind Azrael TrafficFactor, using 1: " + e.Message);
                trafficFactor = null;
            }
            return trafficFactor;
        }
    }
}

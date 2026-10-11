using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Niceties
{
    /// <summary>
    /// Guard for Harmony patches that must name a private engine method by string.
    /// A missing target skips that one patch and names the player-facing feature it drops.
    /// </summary>
    internal static class PatchTargets
    {
        private static readonly HashSet<string> Warned = new HashSet<string>();

        internal static bool Exists(Type type, string methodName, Type[] args, string feature)
        {
            if (AccessTools.Method(type, methodName, args) != null)
            {
                return true;
            }

            string key = type.FullName + "." + methodName;
            if (Warned.Add(key))
            {
                Log.Warning("[Niceties] " + key + " not found in this RimWorld build; \"" + feature
                    + "\" is off until Niceties is updated. Everything else still works.");
            }

            return false;
        }
    }
}

#nullable enable
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
        internal const string HideCryptosleep = "hide cryptosleep pawns from the colonist bar";
        internal const string RoommateSleep = "no disturbed sleep for roommates";

        private static readonly HashSet<string> Warned = new HashSet<string>();
        private static readonly HashSet<string> Missing = new HashSet<string>();

        /// <summary>True when a patch for this feature was skipped; shown in Mod Options (AZR-533).</summary>
        internal static bool Unavailable(string feature) => Missing.Contains(feature);

        internal static bool Exists(Type type, string methodName, Type[] args, string feature)
        {
            if (AccessTools.Method(type, methodName, args) != null)
            {
                return true;
            }

            Missing.Add(feature);
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

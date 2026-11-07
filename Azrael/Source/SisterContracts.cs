#nullable enable
using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Azrael
{
    /// <summary>
    /// Read-only probes into sister mods through their public members only, so a private
    /// refactor in a sister mod cannot break the hub. No sister assembly is referenced and
    /// no sister static constructor is forced. Every probe is fail-open: a missing mod,
    /// renamed member or exception reads as "nothing there".
    /// </summary>
    internal static class SisterContracts
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        /// <summary>Nemesis <c>GameComponent_Nemesis.IsEngaged</c>: active hunt, truce, or pending resolution.</summary>
        internal static bool NemesisEngaged()
        {
            try
            {
                object? nemesis = StaticInstance("Nemesis.GameComponent_Nemesis");
                return nemesis?.GetType().GetProperty("IsEngaged", PublicInstance)?.GetValue(nemesis) is true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Nemesis <c>GameComponent_Nemesis.FindNemesisPawn()</c> returns a live pawn.</summary>
        internal static bool NemesisPawnParked()
        {
            try
            {
                object? nemesis = StaticInstance("Nemesis.GameComponent_Nemesis");
                MethodInfo? find = nemesis?.GetType().GetMethod("FindNemesisPawn", PublicInstance, null, Type.EmptyTypes, null);
                return find?.Invoke(nemesis, null) is Pawn p && !p.Destroyed && !p.Dead;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Deep Colony <c>GameComp_DeepColony.familyLetters</c> (public list).</summary>
        internal static int DeepColonyFamilyLetterCount()
        {
            try
            {
                object? deepColony = StaticInstance("DeepColony.GameComp_DeepColony");
                return deepColony?.GetType().GetField("familyLetters", PublicInstance)?.GetValue(deepColony) is ICollection letters
                    ? letters.Count
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Living World <c>LivingWorldCompatApi.IsHandlerRegistered(Type)</c>: true when a handler declared on
        /// <paramref name="consumerTypeName"/> is registered. Null when Living World predates the
        /// compat API, so the caller can fall back to type presence.
        /// </summary>
        internal static bool? LivingWorldHandlerRegistered(string consumerTypeName)
        {
            try
            {
                Type? api = AccessTools.TypeByName("LivingWorld.LivingWorldCompatApi");
                Type? consumer = AccessTools.TypeByName(consumerTypeName);
                if (consumer == null)
                {
                    return false;
                }
                MethodInfo? isRegistered = api?.GetMethod("IsHandlerRegistered", PublicStatic, null, new[] { typeof(Type) }, null);
                return isRegistered == null ? (bool?)null : isRegistered.Invoke(null, new object[] { consumer }) is true;
            }
            catch
            {
                return null;
            }
        }

        private static object? StaticInstance(string typeName)
        {
            if (Current.Game == null)
            {
                return null;
            }
            Type? type = AccessTools.TypeByName(typeName);
            return type?.GetProperty("Instance", PublicStatic)?.GetValue(null);
        }
    }
}

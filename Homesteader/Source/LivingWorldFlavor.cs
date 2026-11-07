#nullable enable
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Living World flavor (HS-S01, AZR-722): when the colony hears of a famine or
    /// sees war refugees, homesteaders remark on their own stores. Messages only, no
    /// game state. Registered by reflection like Deep Colony's consumer, so nothing
    /// changes without Living World. Living World raises events inside the sim tick,
    /// so the pantry rescan here runs on every Multiplayer client alike.
    /// </summary>
    internal static class LivingWorldFlavor
    {
        private const string FamineRumor = "FamineRumor";
        private const string RefugeeFlight = "RefugeeFlight";

        private static FieldInfo? kindField;
        private static FieldInfo? seenField;
        private static FieldInfo? settlementField;
        private static FieldInfo? factionAField;

        internal static void TryRegister()
        {
            try
            {
                Register();
            }
            catch (Exception e)
            {
                Log.Warning("[Homesteader] Living World flavor did not register: " + (e.InnerException ?? e).Message);
            }
        }

        private static void Register()
        {
            Type? signals = AccessTools.TypeByName("LivingWorld.LivingWorldSignals");
            Type? worldEvent = AccessTools.TypeByName("LivingWorld.WorldEvent");
            MethodInfo? register = signals == null ? null : AccessTools.DeclaredMethod(signals, "Register");
            if (worldEvent == null || register == null || register.GetParameters().Length != 1)
            {
                return;
            }

            kindField = AccessTools.Field(worldEvent, "kind");
            seenField = AccessTools.Field(worldEvent, "seenByPlayer");
            settlementField = AccessTools.Field(worldEvent, "settlementLabel");
            factionAField = AccessTools.Field(worldEvent, "factionAName");
            if (kindField == null || seenField == null)
            {
                Log.Warning("[Homesteader] Living World event fields changed; Living World flavor is off.");
                return;
            }

            MethodInfo onEvent = AccessTools.Method(typeof(LivingWorldFlavor), nameof(OnEvent));
            Delegate handler = Delegate.CreateDelegate(register.GetParameters()[0].ParameterType, onEvent);
            register.Invoke(null, new object[] { handler });
        }

        // Matches LivingWorld.LivingWorldSignals.WorldEventHandler(WorldEvent).
        private static void OnEvent(object ev)
        {
            if (ev == null || !(seenField!.GetValue(ev) is bool seen) || !seen)
            {
                return;
            }

            string kind = kindField!.GetValue(ev)?.ToString() ?? string.Empty;
            if (kind != FamineRumor && kind != RefugeeFlight)
            {
                return;
            }

            Map? home = Find.AnyPlayerHomeMap;
            if (home == null)
            {
                return;
            }

            string place = Place(ev);
            if (kind == RefugeeFlight)
            {
                Messages.Message("Homesteader_LwRefugees".Translate(place), MessageTypeDefOf.NeutralEvent, historical: false);
                return;
            }

            int kinds = PantryUtility.RefreshMap(home).preserveKinds;
            string text = kinds > 0
                ? "Homesteader_LwFamineStocked".Translate(place, kinds)
                : "Homesteader_LwFamineEmpty".Translate(place);
            Messages.Message(text, MessageTypeDefOf.NeutralEvent, historical: false);
        }

        private static string Place(object ev)
        {
            if (settlementField?.GetValue(ev) is string settlement && settlement.Length > 0)
            {
                return settlement;
            }

            if (factionAField?.GetValue(ev) is string faction && faction.Length > 0)
            {
                return faction;
            }

            return "Homesteader_LwFarAway".Translate();
        }
    }
}

using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>Fail-open Living World consumer. Flavor letters only — no goodwill.</summary>
    [StaticConstructorOnStartup]
    public static class LivingWorldSoftCompat
    {
        private static FieldInfo kindField;
        private static FieldInfo seenField;
        private static FieldInfo factionANameField;
        private static FieldInfo settlementLabelField;

        static LivingWorldSoftCompat()
        {
            try
            {
                TryRegister();
            }
            catch (Exception e)
            {
                Log.Warning("[Homesteader] Living World soft-compat failed to register: " + e.Message);
            }
        }

        private static void TryRegister()
        {
            Type signals = AccessTools.TypeByName("LivingWorld.LivingWorldSignals");
            if (signals == null)
            {
                return;
            }

            MethodInfo register = AccessTools.DeclaredMethod(signals, "Register");
            if (register == null)
            {
                return;
            }

            Type worldEventType = AccessTools.TypeByName("LivingWorld.WorldEvent");
            if (worldEventType == null)
            {
                return;
            }

            kindField = AccessTools.Field(worldEventType, "kind");
            seenField = AccessTools.Field(worldEventType, "seenByPlayer");
            factionANameField = AccessTools.Field(worldEventType, "factionAName");
            settlementLabelField = AccessTools.Field(worldEventType, "settlementLabel");
            if (kindField == null || seenField == null)
            {
                return;
            }

            MethodInfo onEvent = AccessTools.Method(typeof(LivingWorldSoftCompat), nameof(OnLivingWorldEvent));
            Delegate handler = Delegate.CreateDelegate(register.GetParameters()[0].ParameterType, onEvent);
            register.Invoke(null, new object[] { handler });
            Log.Message("[Homesteader] Registered Living World pantry flavor (fail-open).");
        }

        private static void OnLivingWorldEvent(object ev)
        {
            if (HomesteaderMod.Settings == null || !HomesteaderMod.Settings.livingWorldFlavor)
            {
                return;
            }

            if (ev == null || !(bool)seenField.GetValue(ev))
            {
                return;
            }

            string kind = kindField.GetValue(ev)?.ToString() ?? string.Empty;
            if (kind != "FamineRumor" && kind != "RefugeeFlight")
            {
                return;
            }

            GameComponent_HomesteaderYard yard = GameComponent_HomesteaderYard.Get();
            if (yard == null)
            {
                return;
            }

            if (Find.TickManager.TicksGame - yard.lastLwLetterTick < 180000)
            {
                return;
            }

            yard.lastLwLetterTick = Find.TickManager.TicksGame;
            string place = settlementLabelField?.GetValue(ev) as string;
            string faction = factionANameField?.GetValue(ev) as string;
            string where = !place.NullOrEmpty() ? place : (faction ?? "the outlands");
            string key = kind == "FamineRumor"
                ? "Homesteader_LwFamineFlavor"
                : "Homesteader_LwRefugeeFlavor";
            Find.LetterStack.ReceiveLetter(
                "Homesteader_LwFlavorLabel".Translate(),
                key.Translate(where),
                LetterDefOf.NeutralEvent);
        }
    }

    /// <summary>Fail-open Stormproof drought queries. Vanilla Drought still works without Stormproof.</summary>
    public static class StormproofSoftCompat
    {
        private static bool droughtProtectTried;
        private static MethodInfo droughtProtect;

        public static bool IsDrought(Map map)
        {
            if (map == null)
            {
                return false;
            }

            GameConditionDef drought = DefDatabase<GameConditionDef>.GetNamedSilentFail("Drought")
                ?? DefDatabase<GameConditionDef>.GetNamedSilentFail("DroughtInitial");
            if (drought != null && map.gameConditionManager.ConditionIsActive(drought))
            {
                return true;
            }

            GameConditionDef dry = DefDatabase<GameConditionDef>.GetNamedSilentFail("Stormproof_DryLightning");
            return dry != null && map.gameConditionManager.ConditionIsActive(dry);
        }

        public static bool DroughtProtected(Map map)
        {
            if (map == null)
            {
                return false;
            }

            if (!droughtProtectTried)
            {
                droughtProtectTried = true;
                Type type = AccessTools.TypeByName("Stormproof.HazardProtection");
                droughtProtect = type == null ? null : AccessTools.Method(type, "DroughtProtecting", new[] { typeof(Map) });
            }

            if (droughtProtect == null)
            {
                return false;
            }

            try
            {
                return (bool)droughtProtect.Invoke(null, new object[] { map });
            }
            catch
            {
                return false;
            }
        }
    }
}

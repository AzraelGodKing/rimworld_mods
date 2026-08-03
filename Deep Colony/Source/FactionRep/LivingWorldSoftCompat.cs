using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Fail-open Living World consumer. No project reference — reflection only.
    /// Maps visible war news onto existing faction drift buffers.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class LivingWorldSoftCompat
    {
        private static FieldInfo kindField;
        private static FieldInfo seenField;
        private static FieldInfo factionAIdField;
        private static FieldInfo factionBIdField;

        static LivingWorldSoftCompat()
        {
            try
            {
                TryRegister();
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Living World soft-compat failed to register: " + e.Message);
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

            ParameterInfo[] pars = register.GetParameters();
            if (pars.Length != 1)
            {
                return;
            }

            Type handlerType = pars[0].ParameterType;
            Type worldEventType = AccessTools.TypeByName("LivingWorld.WorldEvent");
            if (worldEventType == null)
            {
                return;
            }

            kindField = AccessTools.Field(worldEventType, "kind");
            seenField = AccessTools.Field(worldEventType, "seenByPlayer");
            factionAIdField = AccessTools.Field(worldEventType, "factionAId");
            factionBIdField = AccessTools.Field(worldEventType, "factionBId");
            if (kindField == null || seenField == null)
            {
                return;
            }

            MethodInfo onEvent = AccessTools.Method(typeof(LivingWorldSoftCompat), nameof(OnLivingWorldEvent));
            Delegate handler = Delegate.CreateDelegate(handlerType, onEvent);
            register.Invoke(null, new object[] { handler });
            Log.Message("[DeepColony] Registered Living World goodwill consumer (fail-open).");
        }

        // Must match LivingWorld.LivingWorldSignals.WorldEventHandler(WorldEvent).
        private static void OnLivingWorldEvent(object ev)
        {
            if (ev == null || !(bool)seenField.GetValue(ev))
            {
                return;
            }

            object kindObj = kindField.GetValue(ev);
            string kind = kindObj?.ToString() ?? string.Empty;
            if (kind != "DecisiveVictory" && kind != "Betrayal" && kind != "RefugeeFlight")
            {
                return;
            }

            int aId = factionAIdField != null ? (int)factionAIdField.GetValue(ev) : -1;
            int bId = factionBIdField != null ? (int)factionBIdField.GetValue(ev) : -1;
            Faction a = FindFaction(aId);
            Faction b = FindFaction(bId);

            if (kind == "DecisiveVictory")
            {
                ApplySharedEnemyBoost(winner: a, loser: b);
                ApplyAllySympathy(loser: b, magnitude: 1.5f);
                return;
            }

            if (kind == "Betrayal")
            {
                if (IsFriendly(b))
                {
                    GameComp_DeepColony.Instance?.AddFactionDrift(a, -1.5f);
                    GameComp_DeepColony.Instance?.AddFactionDrift(b, 1f);
                }
                return;
            }

            if (kind == "RefugeeFlight")
            {
                ApplyAllySympathy(loser: a, magnitude: 1f);
            }
        }

        private static void ApplySharedEnemyBoost(Faction winner, Faction loser)
        {
            if (winner == null || loser == null)
            {
                return;
            }
            if (loser.HostileTo(Faction.OfPlayer) && !winner.HostileTo(Faction.OfPlayer))
            {
                GameComp_DeepColony.Instance?.AddFactionDrift(winner, 1.25f);
            }
            else if (loser.HostileTo(Faction.OfPlayer))
            {
                GameComp_DeepColony.Instance?.AddFactionDrift(winner, 0.75f);
            }
        }

        private static void ApplyAllySympathy(Faction loser, float magnitude)
        {
            if (loser == null || loser.IsPlayer)
            {
                return;
            }
            if (IsFriendly(loser))
            {
                GameComp_DeepColony.Instance?.AddFactionDrift(loser, magnitude);
            }
        }

        private static bool IsFriendly(Faction faction)
        {
            if (faction == null || faction.IsPlayer || faction.defeated)
            {
                return false;
            }
            return faction.PlayerRelationKind == FactionRelationKind.Ally
                || faction.GoodwillWith(Faction.OfPlayer) >= 50;
        }

        private static Faction FindFaction(int id)
        {
            if (id < 0)
            {
                return null;
            }
            foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
            {
                if (f.loadID == id)
                {
                    return f;
                }
            }
            return null;
        }
    }
}

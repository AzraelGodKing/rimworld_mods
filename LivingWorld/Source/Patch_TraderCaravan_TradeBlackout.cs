using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Soft trade blackout: factions locked in total war are less likely to send traders.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_TraderCaravan_TradeBlackout
    {
        // Protected override. DeclaredMethod returns null if the override is gone, so
        // Harmony fails this class loudly instead of binding to the base IncidentWorker.
        public static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(IncidentWorker_TraderCaravanArrival), "CanFireNowSub");
        }

        public static void Postfix(IncidentParms parms, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (settings == null || !settings.enabled || !settings.tradeBlackoutEnabled)
            {
                return;
            }
            Faction faction = parms.faction;
            if (faction == null)
            {
                return;
            }
            if (LivingWorldDiplomacy.FactionUnderTradeBlackout(faction))
            {
                // Cautious: most of the time block; rare trickle still allowed. Seeded by
                // faction and hour so every Multiplayer client rolls the same, even when
                // CanFireNow is asked from UI code.
                int seed = Gen.HashCombineInt(faction.loadID, Find.TickManager.TicksGame / 2500);
                if (!Rand.ChanceSeeded(0.15f, seed))
                {
                    __result = false;
                }
            }
        }
    }
}

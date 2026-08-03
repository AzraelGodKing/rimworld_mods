using HarmonyLib;
using RimWorld;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Soft trade blackout: factions locked in total war are less likely to send traders.
    /// </summary>
    [HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), "CanFireNowSub")]
    public static class Patch_TraderCaravan_TradeBlackout
    {
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
                // Cautious: most of the time block; rare trickle still allowed.
                if (!Rand.Chance(0.15f))
                {
                    __result = false;
                }
            }
        }
    }
}

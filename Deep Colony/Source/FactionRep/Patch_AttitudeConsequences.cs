using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    /// <summary>B12 — attitude consequences for trade, caravans, and raids (settings-gated).</summary>
    [HarmonyPatch(typeof(Tradeable), nameof(Tradeable.GetPriceFor))]
    public static class Patch_Tradeable_GetPriceFor_Attitude
    {
        public static void Postfix(Tradeable __instance, TradeAction action, ref float __result)
        {
            if (!FactionAttitudeUtility.ConsequencesActive) return;
            Faction faction = TradeSession.trader?.Faction;
            if (faction == null || faction.IsPlayer) return;

            if (action == TradeAction.PlayerBuys)
                __result *= FactionAttitudeUtility.TradeBuyFactor(faction);
            else if (action == TradeAction.PlayerSells)
                __result *= FactionAttitudeUtility.TradeSellFactor(faction);
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), "CanFireNowSub")]
    public static class Patch_TraderCaravan_Attitude
    {
        public static void Postfix(IncidentParms parms, ref bool __result)
        {
            if (!__result) return;
            if (!FactionAttitudeUtility.ConsequencesActive) return;
            if (parms?.faction == null) return;
            if (FactionAttitudeUtility.ShouldBlockTraderCaravan(parms.faction))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class Patch_RaidEnemy_AttitudePoints
    {
        public static void Prefix(IncidentParms parms)
        {
            if (!FactionAttitudeUtility.ConsequencesActive) return;
            if (parms?.faction == null) return;
            float factor = FactionAttitudeUtility.RaidPointsFactor(parms.faction);
            if (System.Math.Abs(factor - 1f) < 0.001f) return;
            parms.points *= factor;
        }
    }

    [HarmonyPatch(typeof(TradeDeal), nameof(TradeDeal.TryExecute))]
    public static class Patch_TradeDeal_AttitudeRefuse
    {
        public static bool Prefix(ref bool __result, ref bool actuallyTraded)
        {
            if (!FactionAttitudeUtility.ConsequencesActive) return true;
            Faction faction = TradeSession.trader?.Faction;
            if (faction == null) return true;
            if (!FactionAttitudeUtility.ShouldRefuseTrade(faction)) return true;

            Messages.Message(
                "DC_TradeRefusedAttitude".Translate(faction.Name),
                MessageTypeDefOf.RejectInput,
                false);
            __result = false;
            actuallyTraded = false;
            return false;
        }
    }
}

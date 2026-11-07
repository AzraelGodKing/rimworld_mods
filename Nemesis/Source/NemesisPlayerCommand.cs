#nullable enable
using RimWorld;
using Verse;

namespace Nemesis
{
    public enum NemesisReply
    {
        TauntBack,
        OfferTruce,
        DemandSurrender,
    }

    /// <summary>
    /// Every player action that changes hunt state goes through one of these methods: the
    /// dossier buttons, the capture dialog, and the comms console job. They take only
    /// synced values (hunt ID, map, numbers, enums), so <see cref="NemesisMultiplayer"/>
    /// can register them as Multiplayer sync methods. Without Multiplayer they run directly.
    /// </summary>
    public static class NemesisPlayerCommand
    {
        public static void BuyLead(int huntId, Map? map)
        {
            NemesisData? h = GameComponent_Nemesis.Instance?.HuntById(huntId);
            if (h != null)
                NemesisInformants.TryBuyLead(h, map);
        }

        public static void PostBounty(int huntId, Map? map, int silver)
        {
            NemesisData? h = GameComponent_Nemesis.Instance?.HuntById(huntId);
            if (h != null)
                NemesisInformants.TryPostBounty(h, map, silver);
        }

        public static void Reply(int huntId, NemesisReply reply)
        {
            NemesisData? h = GameComponent_Nemesis.Instance?.HuntById(huntId);
            if (h == null || !h.active) return;
            switch (reply)
            {
                case NemesisReply.TauntBack:
                    NemesisCommsReplies.DoTauntBack(h);
                    break;
                case NemesisReply.OfferTruce:
                    NemesisCommsReplies.DoOfferTruce(h);
                    break;
                case NemesisReply.DemandSurrender:
                    NemesisCommsReplies.DoDemandSurrender(h);
                    break;
            }
        }

        /// <summary>Capture resolution choice (execute / release / keep / truce).</summary>
        public static void Resolve(int huntId, NemesisOutcome outcome)
        {
            NemesisData? h = GameComponent_Nemesis.Instance?.HuntById(huntId);
            if (h != null)
                Dialog_NemesisResolution.ApplyOutcome(h, outcome);
        }
    }
}

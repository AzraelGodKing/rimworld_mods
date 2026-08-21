using Verse;

namespace DeepColony
{
    public enum FactionRepReason : byte
    {
        Other = 0,
        Raid = 1,
        SharedEnemyRaid = 2,
        TradeCaravan = 3,
        SuccessfulTrade = 4,
        Gift = 5,
        SharedKill = 6,
        Grudge = 7,
        LwVictory = 8,
        LwBetrayal = 9,
        LwRefugee = 10,
        IdleAlly = 11,
        IdleEnemy = 12,
        Envoy = 13,
        Debug = 14,
        Tribute = 15,
        EnvoyVisit = 16,
        EnvoyPresent = 17,
        FamilyDefect = 18
    }

    public class FactionRepLedgerEntry : IExposable
    {
        public int factionId = -1;
        public FactionRepReason reason;
        public float amount;
        public int ticksGame;
        public int count = 1;

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionId, "factionId", -1);
            Scribe_Values.Look(ref reason, "reason", FactionRepReason.Other);
            Scribe_Values.Look(ref amount, "amount", 0f);
            Scribe_Values.Look(ref ticksGame, "ticksGame", 0);
            Scribe_Values.Look(ref count, "count", 1);
        }

        public string ReasonLabel()
        {
            return ("DC_RepReason_" + reason).Translate();
        }
    }

    public enum FactionAttitude : byte
    {
        Neutral = 0,
        Cordial = 1,
        Indebted = 2,
        Wary = 3,
        Vengeful = 4
    }
}

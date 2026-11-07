using Verse;

namespace DeepColony
{
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
}

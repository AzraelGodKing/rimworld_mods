using System.Collections.Generic;
using Verse;

namespace Strata
{
    public enum RelayPurpose
    {
        Work,
        Food,
        Rest,
        Haul,
    }

    // Lightweight coordination so a whole colony of idle pawns doesn't stampede
    // to the same level for one job or one free bed. Each relaying pawn stakes a
    // short-lived claim on (target level, purpose); others check the claim count
    // against a cap before committing to the same destination. Purely in-memory
    // and time-expiring - nothing to save, and a missed cleanup just frees the
    // slot early.
    public static class RelayClaims
    {
        private struct Claim
        {
            public int mapId;
            public RelayPurpose purpose;
            public int expiresTick;
        }

        private const int ClaimTicks = 2500;

        private static readonly Dictionary<int, Claim> claims = new Dictionary<int, Claim>();

        // Claims are tick-stamped per pawn ID; stale entries from a previous
        // save would count against caps in the next one. Cleared on game load.
        internal static void ResetSession()
        {
            claims.Clear();
        }

        public static int ActiveCount(Map map, RelayPurpose purpose)
        {
            int now = Find.TickManager.TicksGame;
            int count = 0;
            foreach (Claim c in claims.Values)
            {
                if (c.mapId == map.uniqueID && c.purpose == purpose && c.expiresTick > now)
                {
                    count++;
                }
            }
            return count;
        }

        // True if the pawn may relay to this destination without exceeding the
        // cap. A pawn's own existing claim doesn't count against it.
        public static bool CanClaim(Pawn pawn, Map map, RelayPurpose purpose, int cap)
        {
            if (cap <= 0)
            {
                return false;
            }
            int count = ActiveCount(map, purpose);
            if (claims.TryGetValue(pawn.thingIDNumber, out Claim existing)
                && existing.mapId == map.uniqueID && existing.purpose == purpose
                && existing.expiresTick > Find.TickManager.TicksGame)
            {
                count--;
            }
            return count < cap;
        }

        public static void Register(Pawn pawn, Map map, RelayPurpose purpose)
        {
            claims[pawn.thingIDNumber] = new Claim
            {
                mapId = map.uniqueID,
                purpose = purpose,
                expiresTick = Find.TickManager.TicksGame + ClaimTicks,
            };
        }
    }
}

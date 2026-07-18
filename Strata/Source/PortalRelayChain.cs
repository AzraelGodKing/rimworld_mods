using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Every relay only issues one EnterPortal. Vanilla clears the job queue after
    // OnEntered, so multi-hop trips (B2 → B1 → surface → A1) must keep commuting
    // until the destination map. Rest finishes with LayDown; other purposes clear
    // the intent and let vanilla AI take over on arrival.
    public static class PortalRelayChain
    {
        private class Intent
        {
            public int destMapId;
            public RelayPurpose purpose;
            public int preferredBedId; // Rest only; -1 = any
        }

        private static readonly Dictionary<int, Intent> intents = new Dictionary<int, Intent>();
        private static readonly List<int> pending = new List<int>();

        internal static void ResetSession()
        {
            intents.Clear();
            pending.Clear();
        }

        public static void Mark(
            Pawn pawn,
            Map destMap,
            RelayPurpose purpose,
            Building_Bed preferredBed = null)
        {
            if (pawn == null || destMap == null)
            {
                return;
            }

            intents[pawn.thingIDNumber] = new Intent
            {
                destMapId = destMap.uniqueID,
                purpose = purpose,
                preferredBedId = preferredBed?.thingIDNumber ?? -1,
            };
        }

        public static bool HasIntent(Pawn pawn)
        {
            return pawn != null && intents.ContainsKey(pawn.thingIDNumber);
        }

        public static bool HasIntent(Pawn pawn, RelayPurpose purpose)
        {
            return pawn != null
                && intents.TryGetValue(pawn.thingIDNumber, out Intent intent)
                && intent.purpose == purpose;
        }

        public static void NotifyPortalArrival(Pawn pawn)
        {
            if (pawn == null || !intents.ContainsKey(pawn.thingIDNumber))
            {
                return;
            }

            if (!pending.Contains(pawn.thingIDNumber))
            {
                pending.Add(pawn.thingIDNumber);
            }
        }

        internal static void Tick()
        {
            if (pending.Count == 0)
            {
                return;
            }

            for (int i = pending.Count - 1; i >= 0; i--)
            {
                int id = pending[i];
                pending.RemoveAt(i);
                Continue(id);
            }
        }

        private static void Continue(int pawnId)
        {
            if (!intents.TryGetValue(pawnId, out Intent intent))
            {
                return;
            }

            Pawn pawn = FindPawn(pawnId);
            if (pawn?.jobs == null || !pawn.Spawned || pawn.Downed || pawn.Dead)
            {
                intents.Remove(pawnId);
                return;
            }

            // Drafted colonists drop automated relays; robots have no draft.
            if (pawn.Drafted && pawn.IsColonist)
            {
                intents.Remove(pawnId);
                return;
            }

            Map destMap = FindMap(intent.destMapId);
            if (destMap == null)
            {
                intents.Remove(pawnId);
                return;
            }

            if (pawn.Map != destMap)
            {
                Building_Bed bed = intent.preferredBedId > 0
                    ? FindBedById(destMap, intent.preferredBedId)
                    : null;
                Job hop = PawnRelay.TryRelayToMap(
                    pawn,
                    destMap,
                    touchCooldown: false,
                    intent.purpose,
                    bed);
                if (hop != null)
                {
                    pawn.jobs.StartJob(hop, JobCondition.InterruptForced);
                    return;
                }

                intents.Remove(pawnId);
                return;
            }

            intents.Remove(pawnId);

            if (intent.purpose == RelayPurpose.Rest)
            {
                FinishRest(pawn, intent.preferredBedId);
            }
            // Food / work / medical / joy / haul: idle on the destination map so
            // the next think pass runs vanilla jobgivers where the need is.
        }

        private static void FinishRest(Pawn pawn, int preferredBedId)
        {
            Building_Bed bed = ResolveRestArrivalBed(pawn, preferredBedId);
            if (bed == null || !pawn.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                return;
            }

            // Never claim a different bed on arrival — ownership was decided
            // before the commute (owned bed or homeless claim).
            pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
        }

        private static Building_Bed ResolveRestArrivalBed(Pawn pawn, int preferredBedId)
        {
            // Owned bed on this map first — never land in a random double.
            Building_Bed owned = pawn.ownership?.OwnedBed;
            if (owned != null && owned.Spawned && owned.Map == pawn.Map)
            {
                return owned;
            }

            Building_Bed preferred = FindBedById(pawn.Map, preferredBedId);
            if (preferred != null)
            {
                return preferred;
            }

            return null;
        }

        private static Building_Bed FindBedById(Map map, int thingId)
        {
            if (map == null || thingId <= 0)
            {
                return null;
            }

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is Building_Bed bed && bed.thingIDNumber == thingId)
                {
                    return bed;
                }
            }

            return null;
        }

        private static Map FindMap(int uniqueId)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].uniqueID == uniqueId)
                {
                    return maps[i];
                }
            }

            return null;
        }

        private static Pawn FindPawn(int thingId)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                IReadOnlyList<Pawn> pawns = maps[i].mapPawns.AllPawnsSpawned;
                for (int j = 0; j < pawns.Count; j++)
                {
                    if (pawns[j].thingIDNumber == thingId)
                    {
                        return pawns[j];
                    }
                }
            }

            return null;
        }
    }
}

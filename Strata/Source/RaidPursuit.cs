using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Strata
{
    // Raids follow you through the stairwells. When a hostile group has nobody
    // left to fight on its level (the colony is hiding on another floor), the
    // raiders find an unsealed stairwell or powered elevator and pursue -
    // exactly the same "relay the pawn, let vanilla AI take over on arrival"
    // trick the colonists use. A sealed stairwell stops them cold, so sealing
    // becomes a real defensive decision instead of flavor.
    public class MapComponent_RaidPursuit : MapComponent
    {
        private const int PursuitInterval = 300;
        private const int EnrollInterval = 250;
        private const int MaxPerPulse = 4;
        private const int MessageCooldown = 5000;

        private int lastMessageTick = -99999;

        private readonly List<Pawn> pursuerBuffer = new List<Pawn>();

        public MapComponent_RaidPursuit(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.raidPursuitEnabled)
            {
                return;
            }
            // Stagger per map so tall bases don't pulse every level on one tick.
            int tick = Find.TickManager.TicksGame + map.uniqueID * 37;
            if (tick % EnrollInterval == 0)
            {
                EnrollStrays();
            }
            if (tick % PursuitInterval == 0)
            {
                TryPursue();
            }
        }

        // ---- Sending raiders through a portal ----

        private void TryPursue()
        {
            if (!RelevantMap() || map.mapPawns.FreeColonistsSpawnedCount > 0)
            {
                return;
            }

            pursuerBuffer.Clear();
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (IsPursuer(pawns[i]))
                {
                    pursuerBuffer.Add(pawns[i]);
                }
            }
            if (pursuerBuffer.Count == 0)
            {
                return;
            }

            // Chase toward the nearest level that still has colonists on it.
            MapPortal firstStep = null;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                if (link.map.mapPawns.FreeColonistsSpawnedCount > 0)
                {
                    firstStep = link.firstStep;
                    break;
                }
            }
            if (firstStep == null || !firstStep.Spawned || !firstStep.IsEnterable(out _))
            {
                return;
            }

            int sent = 0;
            foreach (Pawn raider in pursuerBuffer)
            {
                if (sent >= MaxPerPulse)
                {
                    break;
                }
                if (!raider.CanReach(firstStep, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }
                raider.GetLord()?.Notify_PawnLost(raider, PawnLostCondition.ForcedToJoinOtherLord);
                raider.jobs.StopAll();
                raider.jobs.StartJob(JobMaker.MakeJob(JobDefOf.EnterPortal, firstStep), JobCondition.InterruptForced);
                sent++;
            }

            if (sent > 0 && Find.TickManager.TicksGame - lastMessageTick > MessageCooldown)
            {
                lastMessageTick = Find.TickManager.TicksGame;
                bool goingDown = StrataDepth.Of(LevelGraph.OtherMapSafe(firstStep)) > StrataDepth.Of(map);
                Messages.Message(
                    "Raiders have found the " + firstStep.def.label + " and are coming " +
                    (goingDown ? "down" : "up") + " after your colonists!",
                    new LookTargets(firstStep), MessageTypeDefOf.ThreatBig);
            }
        }

        private bool IsPursuer(Pawn pawn)
        {
            if (pawn.Dead || pawn.Downed || pawn.InMentalState || pawn.IsPrisoner)
            {
                return false;
            }
            if (!pawn.RaceProps.Humanlike && !pawn.RaceProps.IsMechanoid)
            {
                return false;
            }
            if (pawn.Faction == null || !pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }
            if (pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                return false;
            }
            // A group already giving up and leaving is not a pursuit.
            Lord lord = pawn.GetLord();
            if (lord?.CurLordToil is LordToil_PanicFlee || lord?.CurLordToil is LordToil_ExitMap)
            {
                return false;
            }
            return true;
        }

        // ---- Receiving raiders on the far side ----

        // Called from the stairs' OnEntered the moment a pawn steps off the
        // portal. A lord-less hostile must be enrolled BEFORE its first think
        // on the new map: vanilla's lordless AI is "leave the map", and on a
        // pocket map the way out is the stairs it just came down - which read
        // as raiders turning around and running straight back up.
        public static void NotifyPortalArrival(Pawn pawn, Map map)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.raidPursuitEnabled)
            {
                return;
            }
            if (pawn == null || map == null || pawn.Dead || pawn.Downed || pawn.IsPrisoner)
            {
                return;
            }
            if (!pawn.RaceProps.Humanlike && !pawn.RaceProps.IsMechanoid)
            {
                return;
            }
            if (pawn.Faction == null || !pawn.Faction.HostileTo(Faction.OfPlayer)
                || pawn.GetLord() != null)
            {
                return;
            }
            EnrollIntoAssault(pawn, map);
        }

        private static void EnrollIntoAssault(Pawn pawn, Map map)
        {
            // Join an assault already in progress from the same faction so
            // arrivals fight as one group.
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord lord = lords[i];
                if (lord.faction == pawn.Faction && lord.LordJob is LordJob_AssaultColony
                    && !(lord.CurLordToil is LordToil_PanicFlee) && !(lord.CurLordToil is LordToil_ExitMap)
                    && lord.CanAddPawn(pawn))
                {
                    lord.AddPawn(pawn);
                    return;
                }
            }
            bool underground = StrataMapUtility.IsUnderground(map);
            var lordJob = new LordJob_AssaultColony(pawn.Faction,
                canKidnap: !underground, canTimeoutOrFlee: !underground);
            LordMaker.MakeNewLord(pawn.Faction, lordJob, map, new List<Pawn> { pawn });
        }

        // A pawn that walks through a portal leaves its lord behind on the old
        // map. Any lord-less hostile gets enrolled into a fresh assault lord so
        // it fights (or keeps pursuing via TryPursue) instead of standing
        // around - even on a level with no colonists, since a lordless raider
        // otherwise takes vanilla's leave-the-map job back up the stairs.
        private void EnrollStrays()
        {
            if (!RelevantMap())
            {
                return;
            }

            Dictionary<Faction, List<Pawn>> strays = null;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.Dead || pawn.Downed || pawn.InMentalState || pawn.IsPrisoner)
                {
                    continue;
                }
                if (!pawn.RaceProps.Humanlike && !pawn.RaceProps.IsMechanoid)
                {
                    continue;
                }
                if (pawn.Faction == null || !pawn.Faction.HostileTo(Faction.OfPlayer))
                {
                    continue;
                }
                if (pawn.GetLord() != null || pawn.CurJobDef == JobDefOf.EnterPortal)
                {
                    continue;
                }
                strays ??= new Dictionary<Faction, List<Pawn>>();
                if (!strays.TryGetValue(pawn.Faction, out List<Pawn> list))
                {
                    strays[pawn.Faction] = list = new List<Pawn>();
                }
                list.Add(pawn);
            }
            if (strays == null)
            {
                return;
            }

            foreach (KeyValuePair<Faction, List<Pawn>> group in strays)
            {
                for (int i = 0; i < group.Value.Count; i++)
                {
                    EnrollIntoAssault(group.Value[i], map);
                }
            }
        }

        private bool RelevantMap()
        {
            return map.IsPlayerHome || StrataMapUtility.IsUnderground(map);
        }
    }
}

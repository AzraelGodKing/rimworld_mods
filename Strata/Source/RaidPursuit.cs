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

        // Hostiles that stepped off a portal this tick, enrolled next tick:
        // vanilla's EnterPortal job strips the pawn's lord AFTER OnEntered
        // runs, so enrolling during arrival gets silently undone.
        private readonly List<Pawn> pendingArrivals = new List<Pawn>();

        // Assault time already served on the previous level, carried across the
        // transit so a pursuit's give-up clock doesn't reset at every stairwell.
        // Transient (transit takes seconds); keyed by pawn ID so nothing leaks.
        private static readonly Dictionary<int, int> assaultCredit = new Dictionary<int, int>();

        // Transit credit from one save means nothing in another (pawn IDs
        // collide across saves). Cleared on game load.
        internal static void ResetSession()
        {
            assaultCredit.Clear();
        }

        public MapComponent_RaidPursuit(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.raidPursuitEnabled)
            {
                pendingArrivals.Clear();
                return;
            }
            if (pendingArrivals.Count > 0)
            {
                ProcessPendingArrivals();
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
            RaidCoordinator.Tick(map);
        }

        private void ProcessPendingArrivals()
        {
            for (int i = pendingArrivals.Count - 1; i >= 0; i--)
            {
                Pawn pawn = pendingArrivals[i];
                pendingArrivals.RemoveAt(i);
                if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.Map != map)
                {
                    continue;
                }
                if (pawn.Faction == null || !pawn.Faction.HostileTo(Faction.OfPlayer)
                    || pawn.GetLord() != null)
                {
                    continue;
                }
                EnrollIntoAssault(pawn, map);
                // Vanilla's lordless AI may already have handed them a
                // leave-the-map job (back up the stairs) - force a re-think
                // now that they have assault duties.
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
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
            Map targetLevel = null;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                if (link.map.mapPawns.FreeColonistsSpawnedCount > 0)
                {
                    targetLevel = link.map;
                    break;
                }
            }
            if (targetLevel == null)
            {
                return;
            }
            MapPortal firstStep = LevelGraph.BestFirstStep(map, targetLevel, IntVec3.Invalid);
            if (firstStep == null || !firstStep.Spawned)
            {
                return;
            }
            if (!firstStep.IsEnterable(out _))
            {
                // Sealed against them. Raiders don't shrug - they batter the
                // shaft. Break the stairwell and the way down is gone for
                // everyone; that is the price of hiding.
                TrySiege(firstStep);
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
                Lord sourceLord = raider.GetLord();
                if (sourceLord != null
                    && (sourceLord.LordJob is LordJob_AssaultColony || sourceLord.LordJob is LordJob_StrataAssault))
                {
                    if (assaultCredit.Count > 500)
                    {
                        assaultCredit.Clear(); // stale transit entries; bounded either way
                    }
                    assaultCredit[raider.thingIDNumber] = sourceLord.ticksInToil;
                }
                sourceLord?.Notify_PawnLost(raider, PawnLostCondition.ForcedToJoinOtherLord);
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

        // Send pursuers to batter a sealed portal. Vanilla melee handles the
        // rest; stairwells have hit points, and destroying the entrance
        // removes the way down (a level with colonists on it stays alive).
        private void TrySiege(MapPortal sealedPortal)
        {
            if (!StrataPortalUtility.IsSealedPortal(sealedPortal))
            {
                return; // not sealed, just transiently unenterable
            }
            // Bottom landings have no hit points and can't be destroyed -
            // raiders sealed in from below would batter them forever (with a
            // threat message promising progress that can never come). They
            // are simply trapped until the player unseals; only the top-side
            // entrance can be broken.
            if (!sealedPortal.def.useHitPoints || !sealedPortal.def.destroyable)
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
                if (raider.CurJobDef == JobDefOf.AttackMelee
                    || !raider.CanReach(sealedPortal, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }
                Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, sealedPortal);
                job.expiryInterval = 2000;
                raider.jobs.StartJob(job, JobCondition.InterruptForced);
                sent++;
            }
            if (sent > 0 && Find.TickManager.TicksGame - lastMessageTick > MessageCooldown)
            {
                lastMessageTick = Find.TickManager.TicksGame;
                Messages.Message(
                    "Raiders are battering the sealed " + sealedPortal.def.label + "!",
                    new LookTargets(sealedPortal), MessageTypeDefOf.ThreatBig);
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
        // portal. The actual enrollment happens on the NEXT tick, because the
        // vanilla EnterPortal job strips the pawn's lord a few lines after
        // OnEntered returns - enrolling here would be silently undone, leaving
        // the raider lordless, and vanilla's lordless AI walks them right back
        // up the stairs they came down.
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
            if (pawn.Faction == null || !pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                return;
            }
            map.GetComponent<MapComponent_RaidPursuit>()?.pendingArrivals.Add(pawn);
        }

        private static void EnrollIntoAssault(Pawn pawn, Map map)
        {
            int credit = 0;
            if (assaultCredit.TryGetValue(pawn.thingIDNumber, out int stored))
            {
                credit = stored;
                assaultCredit.Remove(pawn.thingIDNumber);
            }
            // Join a pursuit assault already in progress from the same faction
            // so arrivals fight as one group (its clock stands in for the
            // credit). Only Strata's own lord qualifies: vanilla assault lords
            // can decide to steal or give up and walk the whole group back up
            // the stairs.
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord lord = lords[i];
                if (lord.faction == pawn.Faction && lord.LordJob is LordJob_StrataAssault
                    && lord.CurLordToil is LordToil_AssaultColony
                    && lord.CanAddPawn(pawn))
                {
                    lord.AddPawn(pawn);
                    return;
                }
            }
            LordMaker.MakeNewLord(pawn.Faction, new LordJob_StrataAssault(pawn.Faction, credit), map,
                new List<Pawn> { pawn });
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

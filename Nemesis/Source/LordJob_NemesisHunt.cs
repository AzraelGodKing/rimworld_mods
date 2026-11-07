#nullable enable
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Nemesis
{
    /// <summary>
    /// Lord for a nemesis assault (AZR-554). The captain hunts the fixation target first (and
    /// carries them off when kidnap is allowed), escorts guard the captain, and everyone pulls
    /// back once the raid has lost most of its fighters. With an Odyssey shuttle drop the
    /// lord waits for the shuttle to unload the captain, and the retreat goes back to the
    /// shuttle (AZR-706).
    /// </summary>
    public class LordJob_NemesisHunt : LordJob
    {
        private const float CollapseFraction = 0.35f;
        private const float CommanderStepInFraction = 0.6f;
        private const int CommanderMaxHoldTicks = 5000;
        private const int ArrivalTimeoutTicks = 6000;

        private Faction? faction;
        private int huntId = -1;
        private bool kidnap;
        private NemesisTactic tactic;
        private IntVec3 stageCell = IntVec3.Invalid;
        private TransportShip? shuttle;
        private bool awaitingArrival;
        private int startTick = -1;
        private int peakStrength;
        private bool commanderReleased;
        private bool retreatAnnounced;

        public LordJob_NemesisHunt()
        {
        }

        public LordJob_NemesisHunt(Faction faction, int huntId, bool kidnap, NemesisTactic tactic,
            IntVec3 stageCell, TransportShip? shuttle = null)
        {
            this.faction = faction;
            this.huntId = huntId;
            this.kidnap = kidnap;
            this.tactic = tactic;
            this.stageCell = stageCell;
            this.shuttle = shuttle;
            awaitingArrival = shuttle != null;
        }

        public int HuntId => huntId;
        public bool Kidnap => kidnap;
        public NemesisTactic Tactic => tactic;
        public IntVec3 StageCell => stageCell;

        /// <summary>The captain holds at the landing point (Commander, until released).</summary>
        public bool CaptainHoldsBack => tactic == NemesisTactic.Commander && !commanderReleased;

        public override bool ShouldExistWithoutPawns => awaitingArrival;
        public override bool AddFleeToil => false;
        public override bool GuiltyOnDowned => true;

        public NemesisData? Hunt => GameComponent_Nemesis.Instance?.HuntById(huntId);

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            LordToil_NemesisHunt hunt = new LordToil_NemesisHunt();
            graph.StartingToil = hunt;

            Thing? ship = shuttle?.shipThing;
            LordToil retreat = ship != null
                ? new LordToil_EnterShuttleOrLeave(ship, LocomotionUrgency.Jog, canDig: false, interruptCurrentJob: true)
                : new LordToil_ExitMap(LocomotionUrgency.Jog, canDig: false, interruptCurrentJob: true);
            graph.AddToil(retreat);

            Transition toRetreat = new Transition(hunt, retreat);
            toRetreat.AddTrigger(new Trigger_TickCondition(RaidCollapsed, 120));
            toRetreat.AddPostAction(new TransitionAction_Custom(AnnounceRetreat));
            graph.AddTransition(toRetreat);
            return graph;
        }

        public override void LordJobTick()
        {
            base.LordJobTick();
            if (Find.TickManager.TicksGame % 60 != 0) return;
            if (startTick < 0) startTick = Find.TickManager.TicksGame;

            if (awaitingArrival)
                TryTakeArrivedCaptain();
            if (shuttle != null)
                TryExtractCaptain();
            UpdateCommander();
        }

        /// <summary>Fighters of the hunt faction still standing on this map; tracks the peak.</summary>
        private int CurrentStrength()
        {
            Map? map = lord?.Map;
            if (map == null || faction == null) return 0;
            List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(faction);
            int standing = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (!p.Dead && !p.Downed && !p.IsPrisoner && (p.RaceProps.Humanlike || p.RaceProps.IsMechanoid))
                    standing++;
            }
            if (standing > peakStrength) peakStrength = standing;
            return standing;
        }

        /// <summary>Raid points collapsed: most of the fighters that came are down, dead or gone.</summary>
        private bool RaidCollapsed()
        {
            if (awaitingArrival || lord == null || lord.ownedPawns.Count == 0) return false;
            int standing = CurrentStrength();
            return peakStrength >= 2 && standing <= peakStrength * CollapseFraction;
        }

        private void UpdateCommander()
        {
            if (!CaptainHoldsBack || awaitingArrival || lord == null) return;
            int standing = CurrentStrength();
            bool bleeding = peakStrength >= 2 && standing <= peakStrength * CommanderStepInFraction;
            bool waitedLongEnough = startTick >= 0 && Find.TickManager.TicksGame - startTick >= CommanderMaxHoldTicks;
            if (!bleeding && !waitedLongEnough) return;
            commanderReleased = true;
            lord.CurLordToil?.UpdateAllDuties();
        }

        /// <summary>Shuttle drop: once the shuttle has unloaded the captain, take them into this lord.</summary>
        private void TryTakeArrivedCaptain()
        {
            Map? map = lord?.Map;
            NemesisData? h = Hunt;
            Pawn? captain = GameComponent_Nemesis.Instance?.FindNemesisPawnFor(h);
            if (lord == null || map == null || h == null || !h.active)
            {
                awaitingArrival = false;
                return;
            }
            if (captain != null && captain.Spawned && captain.Map == map && captain.GetLord() == null)
            {
                awaitingArrival = false;
                lord.AddPawn(captain);
                if (!stageCell.IsValid) stageCell = captain.Position;
                if (faction != null)
                {
                    NemesisProgression.TrySpawnMechEscort(map, faction, h, captain);
                    NemesisProgression.TrySpawnSoftEscorts(map, faction, h, captain);
                }
                return;
            }
            bool shuttleGone = shuttle == null || shuttle.Disposed || shuttle.shipThing == null || shuttle.shipThing.Destroyed;
            bool timedOut = startTick >= 0 && Find.TickManager.TicksGame - startTick >= ArrivalTimeoutTicks;
            if (shuttleGone || timedOut)
                awaitingArrival = false;
        }

        /// <summary>
        /// The captain boarded the shuttle on the way out: take them out of it and park them as a
        /// world nemesis (the shuttle would otherwise discard its passengers), then send it away.
        /// </summary>
        private void TryExtractCaptain()
        {
            if (shuttle == null || shuttle.Disposed) return;
            Thing? ship = shuttle.shipThing;
            if (ship == null || !ship.Spawned) return;
            ThingOwner? inner = shuttle.TransporterComp?.innerContainer;
            NemesisData? h = Hunt;
            if (inner == null || h == null) return;

            Pawn? captain = null;
            for (int i = 0; i < inner.Count; i++)
            {
                if (inner[i] is Pawn p && p.thingIDNumber == h.nemesisPawnId)
                {
                    captain = p;
                    break;
                }
            }
            bool nobodyLeft = lord == null || lord.ownedPawns.Count == 0;
            if (captain == null && !nobodyLeft) return;

            if (captain != null)
            {
                inner.Remove(captain);
                NemesisPawnUtil.ParkAsWorldNemesis(captain);
                h.cachedNemesis = captain;
                NemesisTells.RecordNote(h, "Nemesis_Note_ShuttleExtract".Translate());
            }
            SendShuttleAway();
        }

        private void SendShuttleAway()
        {
            if (shuttle == null || shuttle.Disposed) return;
            Thing? ship = shuttle.shipThing;
            if (ship != null && ship.Spawned && shuttle.curJob?.def != ShipJobDefOf.FlyAway)
                shuttle.ForceJob(ShipJobDefOf.FlyAway);
            shuttle = null;
        }

        private void AnnounceRetreat()
        {
            if (retreatAnnounced) return;
            retreatAnnounced = true;
            NemesisData? h = Hunt;
            if (h == null || lord == null) return;
            Pawn? captain = GameComponent_Nemesis.Instance?.FindNemesisPawnFor(h);
            Messages.Message(
                "Nemesis_Msg_Retreat".Translate(h.nemesisName ?? "Nemesis_Phrase_Someone".Translate()),
                captain != null && captain.Spawned ? new LookTargets(captain) : LookTargets.Invalid,
                MessageTypeDefOf.PositiveEvent);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (shuttle != null)
                TryExtractCaptain();
            SendShuttleAway();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref huntId, "huntId", -1);
            Scribe_Values.Look(ref kidnap, "kidnap");
            Scribe_Values.Look(ref tactic, "tactic");
            Scribe_Values.Look(ref stageCell, "stageCell", IntVec3.Invalid);
            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_Values.Look(ref awaitingArrival, "awaitingArrival");
            Scribe_Values.Look(ref startTick, "startTick", -1);
            Scribe_Values.Look(ref peakStrength, "peakStrength");
            Scribe_Values.Look(ref commanderReleased, "commanderReleased");
            Scribe_Values.Look(ref retreatAnnounced, "retreatAnnounced");
        }
    }

    /// <summary>
    /// The assault toil: the captain gets <c>Nemesis_HuntTarget</c> aimed at the fixation target
    /// (or holds the landing point as Commander), everyone else escorts the captain or assaults.
    /// Duties are refreshed every 250 ticks so a target who walks onto the map is picked up.
    /// </summary>
    public class LordToil_NemesisHunt : LordToil
    {
        public override bool ForceHighStoryDanger => true;
        public override bool AllowSatisfyLongNeeds => false;

        public override void LordToilTick()
        {
            if (Find.TickManager.TicksGame % 250 == 0)
                UpdateAllDuties();
        }

        public override void UpdateAllDuties()
        {
            if (!(lord.LordJob is LordJob_NemesisHunt job)) return;
            NemesisData? h = job.Hunt;
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
            Pawn? captain = h != null ? comp?.FindNemesisPawnFor(h) : null;
            Pawn? target = h != null && h.targetMode == NemesisTargetMode.Pawn ? comp?.FindTargetPawn(h) : null;
            bool targetHere = target != null && target.Spawned && target.Map == lord.Map && !target.Dead;
            bool captainHere = captain != null && captain.Spawned && captain.Map == lord.Map;

            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn p = lord.ownedPawns[i];
                if (p.mindState == null) continue;
                if (p == captain)
                {
                    if (job.CaptainHoldsBack && job.StageCell.IsValid)
                        p.mindState.duty = new PawnDuty(DutyDefOf.Defend, job.StageCell, 10f);
                    else if (targetHere)
                        p.mindState.duty = new PawnDuty(NemesisDefOf.Nemesis_HuntTarget, target);
                    else
                        p.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
                else if (captainHere && !p.RaceProps.Humanlike)
                {
                    // Mounts and mechs stay with the captain.
                    p.mindState.duty = new PawnDuty(DutyDefOf.Escort, captain, 6f);
                }
                else
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
            }
        }
    }
}

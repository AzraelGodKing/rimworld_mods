using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Strata
{
    // The lord for raiders who pursued the colony through a shaft. It mimics a
    // regular vanilla raid - they can give up and retreat, leave satisfied
    // after wrecking enough of the colony, kidnap a downed colonist, or turn
    // to stealing - with one difference: the steal impulse is delayed. Vanilla
    // raiders start far from the base and walk in; pursuers arrive at a landing
    // INSIDE the base, so the vanilla "high value things around" trigger fired
    // the moment they stepped off the stairs and marched them straight back up
    // with loot, which read as raiders U-turning without a fight.
    public class LordJob_StrataAssault : LordJob
    {
        // Roughly an in-game hour of assault before looting becomes tempting -
        // stands in for the approach walk vanilla raiders get for free.
        private const int StealDelayTicks = 2500;

        private static readonly IntRange AssaultTimeBeforeGiveUp = new IntRange(26000, 38000);

        // Never give up in under this once on a fresh level, even with a lot
        // of inherited assault time - they did just march down the stairs.
        private const int MinAssaultTicks = 2500;

        private Faction faction;

        // Time already spent assaulting on previous levels: a pursuit is one
        // raid, and its give-up clock must not reset at every stairwell.
        private int inheritedAssaultTicks;

        public override bool GuiltyOnDowned => true;

        public LordJob_StrataAssault()
        {
        }

        public LordJob_StrataAssault(Faction faction, int inheritedAssaultTicks = 0)
        {
            this.faction = faction;
            this.inheritedAssaultTicks = inheritedAssaultTicks;
        }

        public override StateGraph CreateGraph()
        {
            var graph = new StateGraph();
            var assault = new LordToil_AssaultColony(attackDownedIfStarving: false);
            graph.AddToil(assault);
            var exit = new LordToil_ExitMap(LocomotionUrgency.Jog, canDig: false, interruptCurrentJob: true);
            exit.useAvoidGrid = true;
            graph.AddToil(exit);

            if (faction != null && faction.def.humanlikeFaction)
            {
                // Give up eventually - the same window as a vanilla assault,
                // minus the time already spent assaulting on levels above.
                var timeout = new Transition(assault, exit);
                var ticksPassed = new Trigger_TicksPassed(
                    Mathf.Max(MinAssaultTicks, AssaultTimeBeforeGiveUp.RandomInRange - inheritedAssaultTicks));
                ticksPassed.WithFilter(new TriggerFilter_MapExitable());
                timeout.AddTrigger(ticksPassed);
                timeout.AddPreAction(new TransitionAction_Message(
                    "MessageRaidersGivenUpLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
                graph.AddTransition(timeout);

                // Leave satisfied after doing enough damage.
                var satisfied = new Transition(assault, exit);
                var damageTaken = new Trigger_FractionColonyDamageTaken(new FloatRange(0.25f, 0.35f).RandomInRange, 900f);
                damageTaken.WithFilter(new TriggerFilter_MapExitable());
                satisfied.AddTrigger(damageTaken);
                satisfied.AddPreAction(new TransitionAction_Message(
                    "MessageRaidersSatisfiedLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
                graph.AddTransition(satisfied);

                // Kidnap a downed victim - by definition only after a fight.
                LordToil kidnapStart = graph.AttachSubgraph(new LordJob_Kidnap().CreateGraph()).StartingToil;
                var kidnap = new Transition(assault, kidnapStart);
                kidnap.AddTrigger(new Trigger_KidnapVictimPresent());
                kidnap.AddPreAction(new TransitionAction_Message(
                    "MessageRaidersKidnapping".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
                graph.AddTransition(kidnap);

                // Turn to looting like vanilla, but only after committing to
                // the assault for a while.
                LordToil stealStart = graph.AttachSubgraph(new LordJob_Steal().CreateGraph()).StartingToil;
                var steal = new Transition(assault, stealStart);
                steal.AddTrigger(new Trigger_HighValueThingsAroundDelayed(StealDelayTicks));
                steal.AddPreAction(new TransitionAction_Message(
                    "MessageRaidersStealing".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
                graph.AddTransition(steal);
            }

            if (faction != null && faction == Faction.OfMechanoids)
            {
                var gameEnd = new Transition(assault, exit);
                gameEnd.AddTrigger(new Trigger_GameEnding());
                gameEnd.AddPreAction(new TransitionAction_Message("MechanoidsMovingOn".Translate()));
                graph.AddTransition(gameEnd);
            }

            var peace = new Transition(assault, exit);
            peace.AddTrigger(new Trigger_BecameNonHostileToPlayer());
            graph.AddTransition(peace);
            return graph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref inheritedAssaultTicks, "inheritedAssaultTicks", 0);
        }
    }

    // Vanilla's steal trigger, gated behind time spent in the assault toil.
    public class Trigger_HighValueThingsAroundDelayed : Trigger_HighValueThingsAround
    {
        private readonly int delayTicks;

        public Trigger_HighValueThingsAroundDelayed(int delayTicks)
        {
            this.delayTicks = delayTicks;
        }

        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            return lord.ticksInToil >= delayTicks && base.ActivateOn(lord, signal);
        }
    }
}

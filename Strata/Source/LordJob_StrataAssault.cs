using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Strata
{
    // The lord for raiders who pursued the colony through a shaft. Vanilla's
    // LordJob_AssaultColony has half a dozen ways to decide the raid is over -
    // timeout, satisfied-with-damage, kidnapping, and (even with the flee
    // flags off) STEALING, which triggers the moment high-value things are
    // around and marches the raiders back out the way they came. On a pocket
    // map every one of those exits is the stairs, which reads as raiders
    // U-turning the moment they arrive.
    //
    // This graph has exactly one job: assault the colony. The only way out is
    // the fight actually ending (faction no longer hostile).
    public class LordJob_StrataAssault : LordJob
    {
        private Faction faction;

        public override bool GuiltyOnDowned => true;

        public LordJob_StrataAssault()
        {
        }

        public LordJob_StrataAssault(Faction faction)
        {
            this.faction = faction;
        }

        public override StateGraph CreateGraph()
        {
            var graph = new StateGraph();
            var assault = new LordToil_AssaultColony(attackDownedIfStarving: false);
            graph.AddToil(assault);
            var exit = new LordToil_ExitMap(LocomotionUrgency.Jog, canDig: false, interruptCurrentJob: true);
            exit.useAvoidGrid = true;
            graph.AddToil(exit);
            var peace = new Transition(assault, exit);
            peace.AddTrigger(new Trigger_BecameNonHostileToPlayer());
            graph.AddTransition(peace);
            return graph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
        }
    }
}

using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Nemesis
{
    /// <summary>
    /// Assault that prioritizes the fixation pawn, then flees to map edge when the raid collapses.
    /// </summary>
    public class LordJob_NemesisHunt : LordJob
    {
        private Faction faction;
        private int focusPawnId = -1;
        private bool canKidnap;
        private bool canFlee = true;

        public int FocusPawnId => focusPawnId;

        public override bool GuiltyOnDowned => true;

        public LordJob_NemesisHunt()
        {
        }

        public LordJob_NemesisHunt(Faction faction, int focusPawnId, bool canKidnap = false, bool canFlee = true)
        {
            this.faction = faction;
            this.focusPawnId = focusPawnId;
            this.canKidnap = canKidnap;
            this.canFlee = canFlee;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            LordToil_NemesisHunt hunt = new LordToil_NemesisHunt(focusPawnId);
            graph.AddToil(hunt);

            if (!canFlee)
            {
                if (canKidnap && faction != null && faction.def.humanlikeFaction)
                {
                    LordToil kidnapStart = graph.AttachSubgraph(new LordJob_Kidnap().CreateGraph()).StartingToil;
                    Transition kidnap = new Transition(hunt, kidnapStart);
                    kidnap.AddTrigger(new Trigger_KidnapVictimPresent());
                    graph.AddTransition(kidnap);
                }
                return graph;
            }

            LordToil_ExitMap exit = new LordToil_ExitMap(LocomotionUrgency.Jog, canDig: false, interruptCurrentJob: true);
            exit.useAvoidGrid = true;
            graph.AddToil(exit);

            Transition lost = new Transition(hunt, exit);
            lost.AddTrigger(new Trigger_FractionPawnsLost(0.5f));
            lost.AddPreAction(new TransitionAction_Message(
                "Nemesis_Message_HuntFlee".Translate(faction?.Name ?? "Nemesis_Phrase_Someone".Translate())));
            graph.AddTransition(lost);

            Transition hurt = new Transition(hunt, exit);
            hurt.AddTrigger(new Trigger_NemesisFocusCritical(focusPawnId));
            hurt.AddPreAction(new TransitionAction_Message(
                "Nemesis_Message_HuntFlee".Translate(faction?.Name ?? "Nemesis_Phrase_Someone".Translate())));
            graph.AddTransition(hurt);

            Transition timeout = new Transition(hunt, exit);
            timeout.AddTrigger(new Trigger_TicksPassed(Rand.RangeInclusive(22000, 34000)));
            timeout.AddPreAction(new TransitionAction_Message(
                "Nemesis_Message_HuntFlee".Translate(faction?.Name ?? "Nemesis_Phrase_Someone".Translate())));
            graph.AddTransition(timeout);

            if (canKidnap && faction != null && faction.def.humanlikeFaction)
            {
                LordToil kidnapStart = graph.AttachSubgraph(new LordJob_Kidnap().CreateGraph()).StartingToil;
                Transition kidnap = new Transition(hunt, kidnapStart);
                kidnap.AddTrigger(new Trigger_KidnapVictimPresent());
                graph.AddTransition(kidnap);
            }

            Transition peace = new Transition(hunt, exit);
            peace.AddTrigger(new Trigger_BecameNonHostileToPlayer());
            graph.AddTransition(peace);

            return graph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref focusPawnId, "focusPawnId", -1);
            Scribe_Values.Look(ref canKidnap, "canKidnap", false);
            Scribe_Values.Look(ref canFlee, "canFlee", true);
        }
    }

    public class LordToil_NemesisHunt : LordToil
    {
        private readonly int focusPawnId;

        public LordToil_NemesisHunt(int focusPawnId)
        {
            this.focusPawnId = focusPawnId;
        }

        public override void UpdateAllDuties()
        {
            Pawn focus = ResolveFocus();
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn p = lord.ownedPawns[i];
                if (p == null || p.Dead) continue;
                if (focus != null && focus.Spawned && focus.Map == Map && !focus.Dead && !focus.Downed)
                    p.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony, focus);
                else
                    p.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
            }
        }

        private Pawn ResolveFocus()
        {
            int id = focusPawnId;
            if (lord?.LordJob is LordJob_NemesisHunt huntJob)
                id = huntJob.FocusPawnId;
            if (id < 0) return null;
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            Pawn cached = comp?.FindTargetPawn();
            if (cached != null && cached.thingIDNumber == id) return cached;
            return null;
        }
    }

    /// <summary>Fire when the on-map nemesis (or any owned pawn matching focus hunt) is critically hurt.</summary>
    public class Trigger_NemesisFocusCritical : Trigger
    {
        private readonly int focusPawnId;

        public Trigger_NemesisFocusCritical(int focusPawnId)
        {
            this.focusPawnId = focusPawnId;
        }

        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            if (signal.type != TriggerSignalType.Tick) return false;
            if (lord?.ownedPawns == null) return false;
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn p = lord.ownedPawns[i];
                if (p == null || p.Dead || !p.Spawned) continue;
                // Flee when any hunter is badly hurt, or the fixation target is downed (mission collapse).
                if (p.health?.summaryHealth != null && p.health.summaryHealth.SummaryHealthPercent < 0.35f)
                    return true;
            }

            if (focusPawnId < 0) return false;
            Pawn focus = GameComponent_Nemesis.Instance?.FindTargetPawn();
            if (focus != null && focus.thingIDNumber == focusPawnId && (focus.Dead || focus.Downed))
                return true;
            return false;
        }
    }
}

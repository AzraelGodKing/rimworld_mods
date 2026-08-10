using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Wardrobe
{
    public class GameComponent_Wardrobe : GameComponent
    {
        private List<WardrobePawnState> states = new List<WardrobePawnState>();

        // Transient swap intent for JobDriver (not scribed).
        public readonly Dictionary<int, WardrobeTrigger> pendingEnter = new Dictionary<int, WardrobeTrigger>();
        public readonly HashSet<int> pendingRestore = new HashSet<int>();

        public GameComponent_Wardrobe(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref states, "states", LookMode.Deep);
            if (states == null)
            {
                states = new List<WardrobePawnState>();
            }
        }

        public WardrobePawnState GetState(Pawn pawn, bool create)
        {
            if (pawn == null)
            {
                return null;
            }

            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].pawnId == pawn.thingIDNumber)
                {
                    return states[i];
                }
            }

            if (!create)
            {
                return null;
            }

            WardrobePawnState created = new WardrobePawnState { pawnId = pawn.thingIDNumber };
            states.Add(created);
            return created;
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % WardrobeUtility.ThinkInterval != 0)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                List<Pawn> colonists = map?.mapPawns?.FreeColonistsSpawned;
                if (colonists == null)
                {
                    continue;
                }

                for (int i = 0; i < colonists.Count; i++)
                {
                    TickPawn(colonists[i]);
                }
            }
        }

        private void TickPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Drafted)
            {
                return;
            }

            WardrobePawnState state = GetState(pawn, create: false);
            if (state == null || !state.AnyEnabled)
            {
                return;
            }

            if (state.cooldownTicks > 0)
            {
                state.cooldownTicks -= WardrobeUtility.ThinkInterval;
                return;
            }

            if (pawn.CurJobDef == WardrobeDefOf.Wardrobe_ChangeOutfit)
            {
                return;
            }

            WardrobeTrigger desired = WardrobeUtility.DesiredTrigger(pawn);

            if (desired == state.activeTrigger)
            {
                return;
            }

            // Leaving a managed mode → restore snapshot.
            if (desired == WardrobeTrigger.None && state.IsManaged)
            {
                TryStartRestore(pawn, state);
                return;
            }

            // Entering or switching modes.
            if (desired != WardrobeTrigger.None && desired != state.activeTrigger)
            {
                // Switch: restore first if already managed, else enter.
                if (state.IsManaged)
                {
                    TryStartRestore(pawn, state);
                }
                else
                {
                    TryStartEnter(pawn, state, desired);
                }
            }
        }

        private void TryStartEnter(Pawn pawn, WardrobePawnState state, WardrobeTrigger trigger)
        {
            if (!state.EnabledFor(trigger))
            {
                return;
            }

            ApparelPolicy policy = WardrobeUtility.FindPolicy(state.PolicyIdFor(trigger));
            Zone_Stockpile stock = WardrobeUtility.FindStockpile(pawn.Map, state.stockpileId);
            if (policy == null || stock == null)
            {
                return;
            }

            List<Apparel> gear = WardrobeUtility.FindPolicyApparelInStockpile(pawn, stock, policy);
            if (gear.Count == 0)
            {
                return;
            }

            pendingEnter[pawn.thingIDNumber] = trigger;
            pendingRestore.Remove(pawn.thingIDNumber);
            Job job = JobMaker.MakeJob(WardrobeDefOf.Wardrobe_ChangeOutfit);
            job.expiryInterval = 5000;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private void TryStartRestore(Pawn pawn, WardrobePawnState state)
        {
            if (!state.IsManaged)
            {
                return;
            }

            pendingRestore.Add(pawn.thingIDNumber);
            pendingEnter.Remove(pawn.thingIDNumber);
            Job job = JobMaker.MakeJob(WardrobeDefOf.Wardrobe_ChangeOutfit);
            job.expiryInterval = 5000;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}

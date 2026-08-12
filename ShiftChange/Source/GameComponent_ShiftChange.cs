using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ShiftChange
{
    public class GameComponent_ShiftChange : GameComponent
    {
        private List<ShiftChangeRule> rules = new List<ShiftChangeRule>();
        private List<PawnShiftState> states = new List<PawnShiftState>();
        private int nextRuleId = 1;

        public GameComponent_ShiftChange(Game game)
        {
        }

        public static GameComponent_ShiftChange Get =>
            Current.Game?.GetComponent<GameComponent_ShiftChange>();

        public IReadOnlyList<ShiftChangeRule> Rules => rules;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref rules, "rules", LookMode.Deep);
            Scribe_Collections.Look(ref states, "states", LookMode.Deep);
            Scribe_Values.Look(ref nextRuleId, "nextRuleId", 1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                rules ??= new List<ShiftChangeRule>();
                states ??= new List<PawnShiftState>();
                if (nextRuleId < 1)
                {
                    nextRuleId = 1;
                }
            }
        }

        public override void GameComponentTick()
        {
            ShiftChangeSettings settings = ShiftChangeMod.Settings;
            if (settings == null || !settings.enabled)
            {
                return;
            }

            if (Find.TickManager.TicksGame % 30 != 0)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Pawn> colonists = maps[m]?.mapPawns?.FreeColonistsSpawned;
                if (colonists == null)
                {
                    continue;
                }

                for (int i = 0; i < colonists.Count; i++)
                {
                    TickPawn(colonists[i], settings);
                }
            }
        }

        private void TickPawn(Pawn pawn, ShiftChangeSettings settings)
        {
            if (pawn == null || pawn.Dead || !pawn.RaceProps.Humanlike)
            {
                return;
            }

            ShiftChangeRule sleepRule = FindRule(pawn.thingIDNumber, ShiftChangeTriggerKind.Sleep);
            if (sleepRule == null || !sleepRule.enabled)
            {
                return;
            }

            PawnShiftState state = GetOrCreateState(pawn.thingIDNumber);
            bool onSleep = ShiftChangeUtility.IsSleepSchedule(pawn);
            int now = Find.TickManager.TicksGame;

            if (onSleep && !state.triggerActive)
            {
                state.triggerActive = true;
                if (!state.managed && now - state.lastSwapTick >= settings.swapCooldownTicks)
                {
                    BeginApply(pawn, sleepRule, state, now);
                }
            }
            else if (!onSleep && state.triggerActive)
            {
                state.triggerActive = false;
                if (state.managed && now - state.lastSwapTick >= settings.swapCooldownTicks)
                {
                    BeginRestore(pawn, sleepRule, state, now);
                }
            }
        }

        private void BeginApply(Pawn pawn, ShiftChangeRule rule, PawnShiftState state, int now)
        {
            if (rule.ResolvePolicy() == null)
            {
                return;
            }

            if (ShiftChangeUtility.FindWardrobe(pawn, rule) == null)
            {
                return;
            }

            // Avoid interrupting critical jobs mid-fight / fleeing.
            if (pawn.Downed || pawn.InMentalState || pawn.Drafted)
            {
                return;
            }

            JobDef cur = pawn.CurJobDef;
            if (cur != null && (cur == JobDefOf.Flee || cur == JobDefOf.AttackMelee || cur == JobDefOf.AttackStatic))
            {
                return;
            }

            state.snapshotApparelIds = ShiftChangeUtility.SnapshotWornApparelIds(pawn);
            state.activeRuleId = rule.ruleId;
            state.wantsRestore = false;
            state.lastSwapTick = now;

            if (ShiftChangeUtility.TryStartApplyJob(pawn, rule))
            {
                state.managed = true;
            }
        }

        private void BeginRestore(Pawn pawn, ShiftChangeRule rule, PawnShiftState state, int now)
        {
            if (pawn.Downed || pawn.InMentalState || pawn.Drafted)
            {
                state.wantsRestore = true;
                return;
            }

            state.wantsRestore = true;
            state.lastSwapTick = now;
            if (ShiftChangeUtility.TryStartRestoreJob(pawn, rule))
            {
                // managed cleared when restore job finishes successfully
            }
        }

        public void NotifyApplyFinished(Pawn pawn, bool success)
        {
            if (pawn == null)
            {
                return;
            }

            PawnShiftState state = GetState(pawn.thingIDNumber);
            if (state == null)
            {
                return;
            }

            state.managed = success || state.managed;
            state.lastSwapTick = Find.TickManager.TicksGame;
        }

        public void NotifyRestoreFinished(Pawn pawn, bool success)
        {
            if (pawn == null)
            {
                return;
            }

            PawnShiftState state = GetState(pawn.thingIDNumber);
            if (state == null)
            {
                return;
            }

            if (success)
            {
                state.managed = false;
                state.wantsRestore = false;
                state.activeRuleId = -1;
                state.snapshotApparelIds.Clear();
            }

            state.lastSwapTick = Find.TickManager.TicksGame;
        }

        public bool IsManaged(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            PawnShiftState state = GetState(pawn.thingIDNumber);
            return state != null && state.managed;
        }

        public ShiftChangeRule FindRule(int pawnId, ShiftChangeTriggerKind trigger)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                ShiftChangeRule r = rules[i];
                if (r != null && r.pawnId == pawnId && r.trigger == trigger)
                {
                    return r;
                }
            }

            return null;
        }

        public ShiftChangeRule FindRuleById(int ruleId)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] != null && rules[i].ruleId == ruleId)
                {
                    return rules[i];
                }
            }

            return null;
        }

        public PawnShiftState GetState(int pawnId)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i] != null && states[i].pawnId == pawnId)
                {
                    return states[i];
                }
            }

            return null;
        }

        public PawnShiftState GetOrCreateState(int pawnId)
        {
            PawnShiftState state = GetState(pawnId);
            if (state != null)
            {
                return state;
            }

            state = new PawnShiftState { pawnId = pawnId };
            states.Add(state);
            return state;
        }

        public ShiftChangeRule GetOrCreateSleepRule(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            ShiftChangeRule existing = FindRule(pawn.thingIDNumber, ShiftChangeTriggerKind.Sleep);
            if (existing != null)
            {
                return existing;
            }

            ShiftChangeRule rule = new ShiftChangeRule
            {
                ruleId = nextRuleId++,
                pawnId = pawn.thingIDNumber,
                trigger = ShiftChangeTriggerKind.Sleep,
                enabled = false,
                replaceMode = true,
            };
            rules.Add(rule);
            return rule;
        }

        public void RemoveRulesForPawn(int pawnId)
        {
            rules.RemoveAll(r => r == null || r.pawnId == pawnId);
            states.RemoveAll(s => s == null || s.pawnId == pawnId);
        }
    }
}

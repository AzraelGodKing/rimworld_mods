using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ShiftChange
{
    public class GameComponent_ShiftChange : GameComponent
    {
        private List<ShiftChangeRule> rules = new List<ShiftChangeRule>();
        private List<PawnShiftState> states = new List<PawnShiftState>();
        private Dictionary<int, int> apparelClaims = new Dictionary<int, int>();
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
            Scribe_Collections.Look(ref apparelClaims, "apparelClaims", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref nextRuleId, "nextRuleId", 1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                rules ??= new List<ShiftChangeRule>();
                states ??= new List<PawnShiftState>();
                apparelClaims ??= new Dictionary<int, int>();
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

            if (Find.TickManager.TicksGame % 20 != 0)
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

            // Skip while a shift job is already running.
            JobDef curDef = pawn.CurJobDef;
            if (curDef == ShiftChangeDefOf.ShiftChange_Apply
                || curDef == ShiftChangeDefOf.ShiftChange_Restore)
            {
                return;
            }

            PawnShiftState state = GetOrCreateState(pawn.thingIDNumber);
            ShiftChangeRule desired = FindDesiredRule(pawn, settings, state);
            int now = Find.TickManager.TicksGame;

            if (desired != null)
            {
                state.wantsRestore = false;
                state.hysteresisUntilTick = -99999;

                if (!state.managed || state.activeRuleId != desired.ruleId)
                {
                    if (now - state.lastSwapTick >= settings.swapCooldownTicks)
                    {
                        BeginApply(pawn, desired, state, now);
                    }
                }

                return;
            }

            // No desired rule — restore after hysteresis if managed.
            if (state.managed || state.wantsRestore)
            {
                if (state.hysteresisUntilTick < 0)
                {
                    state.hysteresisUntilTick = now + settings.hysteresisTicks;
                }

                if (now >= state.hysteresisUntilTick
                    && now - state.lastSwapTick >= settings.swapCooldownTicks)
                {
                    ShiftChangeRule restoreRule = FindRuleById(state.activeRuleId)
                        ?? FindAnyRuleForPawn(pawn.thingIDNumber);
                    BeginRestore(pawn, restoreRule, state, now);
                }
            }
        }

        /// <summary>
        /// Priority: Sleep schedule → Ideology ritual lord → WorkType (pending or current job).
        /// </summary>
        public ShiftChangeRule FindDesiredRule(Pawn pawn, ShiftChangeSettings settings, PawnShiftState state)
        {
            ShiftChangeRule sleep = FindRule(pawn.thingIDNumber, ShiftChangeTriggerKind.Sleep);
            if (sleep != null && sleep.enabled && ShiftChangeUtility.IsSleepSchedule(pawn))
            {
                return sleep;
            }

            if (settings.ritualTriggersEnabled)
            {
                ShiftChangeRule ritual = FindRule(pawn.thingIDNumber, ShiftChangeTriggerKind.Ritual);
                if (ritual != null && ritual.enabled && ShiftChangeUtility.IsInIdeologyRitual(pawn))
                {
                    return ritual;
                }
            }

            if (settings.workTriggersEnabled)
            {
                string workName = state.pendingWorkTypeDefName;
                if (string.IsNullOrEmpty(workName))
                {
                    WorkTypeDef wt = ShiftChangeUtility.WorkTypeOfJob(pawn.CurJob);
                    workName = wt?.defName;
                }

                if (!string.IsNullOrEmpty(workName))
                {
                    ShiftChangeRule work = FindWorkRule(pawn.thingIDNumber, workName);
                    if (work != null && work.enabled)
                    {
                        return work;
                    }
                }
            }

            return null;
        }

        public void NotifyWorkJobIssued(Pawn pawn, WorkTypeDef workType)
        {
            ShiftChangeSettings settings = ShiftChangeMod.Settings;
            if (settings == null || !settings.enabled || !settings.workTriggersEnabled)
            {
                return;
            }

            if (pawn == null || workType == null)
            {
                return;
            }

            ShiftChangeRule rule = FindWorkRule(pawn.thingIDNumber, workType.defName);
            if (rule == null || !rule.enabled)
            {
                return;
            }

            PawnShiftState state = GetOrCreateState(pawn.thingIDNumber);
            state.pendingWorkTypeDefName = workType.defName;

            if (state.managed && state.activeRuleId == rule.ruleId)
            {
                return;
            }

            if (pawn.Downed || pawn.Drafted || pawn.InMentalState)
            {
                return;
            }

            JobDef curDef = pawn.CurJobDef;
            if (curDef == ShiftChangeDefOf.ShiftChange_Apply
                || curDef == ShiftChangeDefOf.ShiftChange_Restore)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (now - state.lastSwapTick < settings.swapCooldownTicks)
            {
                return;
            }

            BeginApply(pawn, rule, state, now);
        }

        public void NotifyRitualStarted(IEnumerable<Pawn> participants)
        {
            ShiftChangeSettings settings = ShiftChangeMod.Settings;
            if (settings == null || !settings.enabled || !settings.ritualTriggersEnabled)
            {
                return;
            }

            if (participants == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            foreach (Pawn pawn in participants)
            {
                if (pawn == null || !pawn.IsColonist || !pawn.Spawned)
                {
                    continue;
                }

                ShiftChangeRule rule = FindRule(pawn.thingIDNumber, ShiftChangeTriggerKind.Ritual);
                if (rule == null || !rule.enabled)
                {
                    continue;
                }

                PawnShiftState state = GetOrCreateState(pawn.thingIDNumber);
                if (state.managed && state.activeRuleId == rule.ruleId)
                {
                    continue;
                }

                if (now - state.lastSwapTick < settings.swapCooldownTicks)
                {
                    continue;
                }

                BeginApply(pawn, rule, state, now);
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

            if (pawn.Downed || pawn.InMentalState || pawn.Drafted)
            {
                return;
            }

            JobDef cur = pawn.CurJobDef;
            if (cur != null && (cur == JobDefOf.Flee || cur == JobDefOf.AttackMelee || cur == JobDefOf.AttackStatic))
            {
                return;
            }

            // Keep the original civilian snapshot across rule switches.
            if (!state.managed || state.snapshotApparelIds == null || state.snapshotApparelIds.Count == 0)
            {
                state.snapshotApparelIds = ShiftChangeUtility.SnapshotWornApparelIds(pawn);
            }

            state.activeRuleId = rule.ruleId;
            state.wantsRestore = false;
            state.hysteresisUntilTick = -99999;
            state.lastSwapTick = now;
            state.applyJobQueued = true;

            if (ShiftChangeUtility.TryStartApplyJob(pawn, rule))
            {
                state.managed = true;
            }
            else
            {
                state.applyJobQueued = false;
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
            state.pendingWorkTypeDefName = null;
            ShiftChangeUtility.TryStartRestoreJob(pawn, rule);
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

            state.applyJobQueued = false;
            state.managed = success || state.managed;
            state.lastSwapTick = Find.TickManager.TicksGame;
            // Clear pending work hint once dressed — tick/job loop will resume work.
            state.pendingWorkTypeDefName = null;
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
                ReleaseClaimsForPawn(pawn.thingIDNumber);
                state.managed = false;
                state.wantsRestore = false;
                state.activeRuleId = -1;
                state.snapshotApparelIds.Clear();
                state.reservedApparelIds.Clear();
                state.pendingWorkTypeDefName = null;
                state.hysteresisUntilTick = -99999;
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

        public bool TryClaimApparel(int apparelId, int pawnId)
        {
            if (apparelId <= 0)
            {
                return false;
            }

            if (apparelClaims.TryGetValue(apparelId, out int owner) && owner != pawnId)
            {
                // Stale claim: owner gone / not managed anymore.
                PawnShiftState ownerState = GetState(owner);
                if (ownerState != null && ownerState.managed)
                {
                    return false;
                }

                apparelClaims.Remove(apparelId);
            }

            apparelClaims[apparelId] = pawnId;
            PawnShiftState state = GetOrCreateState(pawnId);
            if (!state.reservedApparelIds.Contains(apparelId))
            {
                state.reservedApparelIds.Add(apparelId);
            }

            return true;
        }

        public bool IsClaimedByOther(int apparelId, int pawnId)
        {
            return apparelClaims.TryGetValue(apparelId, out int owner) && owner != pawnId;
        }

        public void ReleaseClaimsForPawn(int pawnId)
        {
            if (apparelClaims == null || apparelClaims.Count == 0)
            {
                return;
            }

            List<int> drop = new List<int>();
            foreach (KeyValuePair<int, int> kv in apparelClaims)
            {
                if (kv.Value == pawnId)
                {
                    drop.Add(kv.Key);
                }
            }

            for (int i = 0; i < drop.Count; i++)
            {
                apparelClaims.Remove(drop[i]);
            }
        }

        public ShiftChangeRule FindRule(int pawnId, ShiftChangeTriggerKind trigger)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                ShiftChangeRule r = rules[i];
                if (r != null && r.pawnId == pawnId && r.trigger == trigger
                    && trigger != ShiftChangeTriggerKind.WorkType)
                {
                    return r;
                }
            }

            return null;
        }

        public ShiftChangeRule FindWorkRule(int pawnId, string workTypeDefName)
        {
            if (string.IsNullOrEmpty(workTypeDefName))
            {
                return null;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                ShiftChangeRule r = rules[i];
                if (r != null
                    && r.pawnId == pawnId
                    && r.trigger == ShiftChangeTriggerKind.WorkType
                    && r.workTypeDefName == workTypeDefName)
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

        public ShiftChangeRule FindAnyRuleForPawn(int pawnId)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] != null && rules[i].pawnId == pawnId)
                {
                    return rules[i];
                }
            }

            return null;
        }

        public List<ShiftChangeRule> RulesForPawn(int pawnId)
        {
            List<ShiftChangeRule> list = new List<ShiftChangeRule>();
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] != null && rules[i].pawnId == pawnId)
                {
                    list.Add(rules[i]);
                }
            }

            return list;
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

        public ShiftChangeRule GetOrCreateSleepRule(Pawn pawn) =>
            GetOrCreateRule(pawn, ShiftChangeTriggerKind.Sleep, null);

        public ShiftChangeRule GetOrCreateRitualRule(Pawn pawn) =>
            GetOrCreateRule(pawn, ShiftChangeTriggerKind.Ritual, null);

        public ShiftChangeRule GetOrCreateWorkRule(Pawn pawn, string workTypeDefName) =>
            GetOrCreateRule(pawn, ShiftChangeTriggerKind.WorkType, workTypeDefName);

        public ShiftChangeRule GetOrCreateRule(Pawn pawn, ShiftChangeTriggerKind trigger, string workTypeDefName)
        {
            if (pawn == null)
            {
                return null;
            }

            ShiftChangeRule existing = trigger == ShiftChangeTriggerKind.WorkType
                ? FindWorkRule(pawn.thingIDNumber, workTypeDefName)
                : FindRule(pawn.thingIDNumber, trigger);
            if (existing != null)
            {
                return existing;
            }

            ShiftChangeRule rule = new ShiftChangeRule
            {
                ruleId = nextRuleId++,
                pawnId = pawn.thingIDNumber,
                trigger = trigger,
                workTypeDefName = workTypeDefName,
                enabled = false,
                replaceMode = true,
            };
            rules.Add(rule);
            return rule;
        }

        public void RemoveRulesForPawn(int pawnId)
        {
            ReleaseClaimsForPawn(pawnId);
            rules.RemoveAll(r => r == null || r.pawnId == pawnId);
            states.RemoveAll(s => s == null || s.pawnId == pawnId);
        }
    }
}

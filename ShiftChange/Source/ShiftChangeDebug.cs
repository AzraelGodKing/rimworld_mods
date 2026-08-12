using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace ShiftChange
{
    [StaticConstructorOnStartup]
    public static class ShiftChangeDebug
    {
        private const string Cat = "Shift Change";

        static ShiftChangeDebug() { }

        [DebugAction(Cat, "Enable Sleep rule on selected (nude/civilian policy if any)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void EnableSleepRuleSelected()
        {
            GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
            if (comp == null)
            {
                Messages.Message("[Shift Change] No game component.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            int n = 0;
            foreach (Pawn p in SelectedColonists())
            {
                ShiftChangeRule rule = comp.GetOrCreateSleepRule(p);
                rule.enabled = true;
                if (string.IsNullOrEmpty(rule.apparelPolicyName)
                    && Current.Game?.outfitDatabase != null)
                {
                    List<ApparelPolicy> all = Current.Game.outfitDatabase.AllOutfits;
                    for (int i = 0; i < all.Count; i++)
                    {
                        if (all[i] != null
                            && all[i].label != null
                            && (all[i].label.IndexOf("nude", System.StringComparison.OrdinalIgnoreCase) >= 0
                                || all[i].label.IndexOf("sleep", System.StringComparison.OrdinalIgnoreCase) >= 0
                                || all[i].label.IndexOf("civilian", System.StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            rule.apparelPolicyName = all[i].label;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(rule.apparelPolicyName) && all.Count > 0)
                    {
                        rule.apparelPolicyName = all[0].label;
                    }
                }

                n++;
            }

            Messages.Message(n > 0
                    ? $"[Shift Change] Enabled Sleep rule on {n} pawn(s)."
                    : "[Shift Change] Select colonists.",
                n > 0 ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                historical: false);
        }

        [DebugAction(Cat, "Force apply shift on selected",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceApply()
        {
            GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
            int n = 0;
            foreach (Pawn p in SelectedColonists())
            {
                ShiftChangeRule rule = comp?.FindRule(p.thingIDNumber, ShiftChangeTriggerKind.Sleep);
                if (rule == null)
                {
                    continue;
                }

                PawnShiftState state = comp.GetOrCreateState(p.thingIDNumber);
                state.snapshotApparelIds = ShiftChangeUtility.SnapshotWornApparelIds(p);
                state.activeRuleId = rule.ruleId;
                state.managed = true;
                if (ShiftChangeUtility.TryStartApplyJob(p, rule))
                {
                    n++;
                }
            }

            Messages.Message($"[Shift Change] Started apply on {n} pawn(s).",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Force restore shift on selected",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceRestore()
        {
            GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
            int n = 0;
            foreach (Pawn p in SelectedColonists())
            {
                ShiftChangeRule rule = comp?.FindRule(p.thingIDNumber, ShiftChangeTriggerKind.Sleep);
                if (ShiftChangeUtility.TryStartRestoreJob(p, rule))
                {
                    n++;
                }
            }

            Messages.Message($"[Shift Change] Started restore on {n} pawn(s).",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Dump Shift Change state for selected",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DumpState()
        {
            GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
            foreach (Pawn p in SelectedColonists())
            {
                ShiftChangeRule rule = comp?.FindRule(p.thingIDNumber, ShiftChangeTriggerKind.Sleep);
                PawnShiftState state = comp?.GetState(p.thingIDNumber);
                Zone_Stockpile zone = ShiftChangeUtility.FindWardrobe(p, rule);
                Log.Message(
                    $"[Shift Change] {p.LabelShort}: enabled={rule?.enabled} policy={rule?.apparelPolicyName} zone={zone?.label} managed={state?.managed} trigger={state?.triggerActive} snap={state?.snapshotApparelIds?.Count ?? 0} sleepSched={ShiftChangeUtility.IsSleepSchedule(p)}");
            }
        }

        private static List<Pawn> SelectedColonists()
        {
            List<Pawn> list = new List<Pawn>();
            if (Find.Selector?.SelectedObjects == null)
            {
                return list;
            }

            foreach (object obj in Find.Selector.SelectedObjects)
            {
                if (obj is Pawn p && p.IsColonist && p.RaceProps.Humanlike && !p.Dead && !list.Contains(p))
                {
                    list.Add(p);
                }
            }

            return list;
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ShiftChange
{
    public class JobDriver_ShiftChangeApply : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => pawn.Downed || pawn.Drafted);

            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            Toil change = new Toil
            {
                initAction = () =>
                {
                    GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
                    if (comp == null)
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    PawnShiftState state = comp.GetState(pawn.thingIDNumber);
                    ShiftChangeRule rule = state != null
                        ? comp.FindRuleById(state.activeRuleId)
                        : null;
                    if (rule == null || state == null)
                    {
                        comp.NotifyApplyFinished(pawn, success: false);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    ApparelPolicy policy = rule.ResolvePolicy();
                    Zone_Stockpile zone = ShiftChangeUtility.FindWardrobe(pawn, rule);
                    if (policy == null || zone == null)
                    {
                        Messages.Message("ShiftChange_Msg_NoWardrobeOrPolicy".Translate(pawn.LabelShort),
                            pawn, MessageTypeDefOf.RejectInput, historical: false);
                        comp.NotifyApplyFinished(pawn, success: false);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    List<Apparel> available = ShiftChangeUtility.CollectApparelInZone(zone);
                    // Also allow already-inventoried pieces that match the policy.
                    ThingOwner inv = pawn.inventory?.innerContainer;
                    if (inv != null)
                    {
                        for (int i = 0; i < inv.Count; i++)
                        {
                            if (inv[i] is Apparel apparel
                                && !apparel.Destroyed
                                && ShiftChangeUtility.PolicyAllows(policy, apparel)
                                && !available.Contains(apparel))
                            {
                                available.Add(apparel);
                            }
                        }
                    }

                    int worn = 0;
                    int claimedDenied = 0;
                    for (int i = 0; i < available.Count; i++)
                    {
                        Apparel a = available[i];
                        if (a == null || a.Destroyed || (a.Wearer != null && a.Wearer != pawn))
                        {
                            continue;
                        }

                        if (!ShiftChangeUtility.PolicyAllows(policy, a))
                        {
                            continue;
                        }

                        if (comp.IsClaimedByOther(a.thingIDNumber, pawn.thingIDNumber))
                        {
                            claimedDenied++;
                            continue;
                        }

                        if (AlreadySatisfied(pawn, a, policy))
                        {
                            continue;
                        }

                        if (ShiftChangeUtility.TryWearFromZone(pawn, a, zone, rule.replaceMode))
                        {
                            worn++;
                        }
                    }

                    if (worn == 0 && available.Count == 0)
                    {
                        Messages.Message("ShiftChange_Msg_EmptyWardrobe".Translate(pawn.LabelShort),
                            pawn, MessageTypeDefOf.RejectInput, historical: false);
                    }
                    else if (worn == 0 && claimedDenied > 0)
                    {
                        Messages.Message("ShiftChange_Msg_ApparelClaimed".Translate(pawn.LabelShort),
                            pawn, MessageTypeDefOf.RejectInput, historical: false);
                    }

                    comp.NotifyApplyFinished(pawn, success: true);
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
            };
            yield return change;
        }

        private static bool AlreadySatisfied(Pawn pawn, Apparel candidate, ApparelPolicy policy)
        {
            List<Apparel> worn = pawn.apparel?.WornApparel;
            if (worn == null)
            {
                return false;
            }

            for (int i = 0; i < worn.Count; i++)
            {
                Apparel w = worn[i];
                if (w == null)
                {
                    continue;
                }

                if (!ApparelUtility.CanWearTogether(w.def, candidate.def, pawn.RaceProps.body)
                    && ShiftChangeUtility.PolicyAllows(policy, w))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class JobDriver_ShiftChangeRestore : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.Downed || pawn.Drafted);

            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            Toil restore = new Toil
            {
                initAction = () =>
                {
                    GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
                    if (comp == null)
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    PawnShiftState state = comp.GetState(pawn.thingIDNumber);
                    if (state == null)
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    ShiftChangeRule rule = comp.FindRuleById(state.activeRuleId)
                        ?? comp.FindAnyRuleForPawn(pawn.thingIDNumber);
                    Zone_Stockpile zone = ShiftChangeUtility.FindWardrobe(pawn, rule);
                    List<int> snapshot = new List<int>(state.snapshotApparelIds ?? new List<int>());

                    if (pawn.apparel?.WornApparel != null)
                    {
                        List<Apparel> wornCopy = new List<Apparel>(pawn.apparel.WornApparel);
                        for (int i = 0; i < wornCopy.Count; i++)
                        {
                            Apparel w = wornCopy[i];
                            if (w == null)
                            {
                                continue;
                            }

                            if (!snapshot.Contains(w.thingIDNumber))
                            {
                                ShiftChangeUtility.RemoveApparelFromPawn(pawn, w, zone);
                            }
                        }
                    }

                    for (int i = 0; i < snapshot.Count; i++)
                    {
                        Apparel a = ShiftChangeUtility.FindApparelById(pawn.Map, snapshot[i]);
                        if (a == null || a.Destroyed)
                        {
                            continue;
                        }

                        if (a.Wearer == pawn)
                        {
                            continue;
                        }

                        if (a.Wearer != null)
                        {
                            continue;
                        }

                        ShiftChangeUtility.TryWearFromZone(pawn, a, zone, replace: true);
                    }

                    comp.NotifyRestoreFinished(pawn, success: true);
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
            };
            yield return restore;
        }
    }
}

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

                    ShiftChangeRule rule = comp.FindRule(pawn.thingIDNumber, ShiftChangeTriggerKind.Sleep);
                    PawnShiftState state = comp.GetState(pawn.thingIDNumber);
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
                    int worn = 0;
                    for (int i = 0; i < available.Count; i++)
                    {
                        Apparel a = available[i];
                        if (a == null || a.Destroyed || a.Wearer != null)
                        {
                            continue;
                        }

                        if (!ShiftChangeUtility.PolicyAllows(policy, a))
                        {
                            continue;
                        }

                        // Skip if already wearing an allowed piece on the same layer set.
                        if (AlreadySatisfied(pawn, a))
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

                    comp.NotifyApplyFinished(pawn, success: true);
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
            };
            yield return change;
        }

        private static bool AlreadySatisfied(Pawn pawn, Apparel candidate)
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
                    && ShiftChangeUtility.PolicyAllows(
                        GameComponent_ShiftChange.Get?.FindRule(pawn.thingIDNumber, ShiftChangeTriggerKind.Sleep)
                            ?.ResolvePolicy(),
                        w))
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

                    ShiftChangeRule rule = comp.FindRule(pawn.thingIDNumber, ShiftChangeTriggerKind.Sleep);
                    PawnShiftState state = comp.GetState(pawn.thingIDNumber);
                    if (state == null)
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    Zone_Stockpile zone = ShiftChangeUtility.FindWardrobe(pawn, rule);
                    List<int> snapshot = new List<int>(state.snapshotApparelIds ?? new List<int>());

                    // Drop currently worn gear that is not in the snapshot into the wardrobe.
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
                                ShiftChangeUtility.DropApparelToZone(pawn, w, zone);
                            }
                        }
                    }

                    // Re-wear snapshotted pieces from the wardrobe / map.
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

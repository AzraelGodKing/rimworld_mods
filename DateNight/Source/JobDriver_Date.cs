using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace DateNight
{
    /// <summary>
    /// One driver for every date activity. The activity is resolved
    /// deterministically per couple, so both partners run the same one.
    /// TargetA = partner, TargetB = spot, TargetC = carried meal / gift.
    /// </summary>
    public class JobDriver_Date : JobDriver
    {
        private const TargetIndex PartnerInd = TargetIndex.A;
        private const TargetIndex SpotInd = TargetIndex.B;
        private const TargetIndex ItemInd = TargetIndex.C;

        private const int TicksBetweenHeartMotes = 180;
        private const int TicksBetweenChat = 320;
        private const int EatDurationTicks = 500;
        private const int WalkWaypointTicks = 300;
        private const int DanceTurnTicks = 35;
        private const float PassiveJoyPerTick = 0.36f / 2500f;

        private int ticksLeft;
        private int totalTicks;
        private int eatTicksLeft = -1;
        private int walkTicksLeft;
        private DateActivity activity = DateActivity.Unresolved;
        private bool giftDelivered;
        private bool inHangPhase;

        private Pawn Partner => job.GetTarget(PartnerInd).Pawn;

        public DateActivity Activity => activity;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
            Scribe_Values.Look(ref totalTicks, "totalTicks", 0);
            Scribe_Values.Look(ref eatTicksLeft, "eatTicksLeft", -1);
            Scribe_Values.Look(ref walkTicksLeft, "walkTicksLeft", 0);
            Scribe_Values.Look(ref activity, "activity", DateActivity.Unresolved);
            Scribe_Values.Look(ref giftDelivered, "giftDelivered", false);
            Scribe_Values.Look(ref inHangPhase, "inHangPhase", false);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Resolve here (not in MakeNewToils): runs once at job start, never on
            // load, so the rebuilt toil graph keeps its saved shape.
            ResolveActivity();
            return true;
        }

        public override string GetReport()
        {
            Pawn partner = Partner;
            string name = partner?.LabelShort ?? "?";
            switch (activity)
            {
                case DateActivity.Dinner:
                    return "DateNight_Report_Dinner".Translate(name);
                case DateActivity.Picnic:
                    return "DateNight_Report_Picnic".Translate(name);
                case DateActivity.Walk:
                    return "DateNight_Report_Walk".Translate(name);
                case DateActivity.Stargaze:
                    return "DateNight_Report_Stargaze".Translate(name);
                case DateActivity.Dance:
                    return "DateNight_Report_Dance".Translate(name);
                case DateActivity.Gift:
                    return "DateNight_Report_Gift".Translate(name);
                case DateActivity.Recreation:
                    return "DateNight_Report_Recreation".Translate(name);
                default:
                    return "DateNight_Report_Hangout".Translate(name);
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(PartnerInd);
            this.FailOn(() => pawn.Drafted);
            this.FailOn(() => !DateNightDateUtility.CanDate(pawn, Partner, force: true));

            bool fetchesItem = NeedsItemFetch();
            if (fetchesItem)
            {
                yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch)
                    .FailOnDespawnedNullOrForbidden(ItemInd);
                yield return Toils_Haul.StartCarryThing(ItemInd, false, false, false);
            }

            Toil go = ToilMaker.MakeToil("DateNightGoSpot");
            go.initAction = () =>
            {
                LocalTargetInfo spot = job.GetTarget(SpotInd);
                if (!spot.IsValid)
                {
                    spot = Partner;
                    job.SetTarget(SpotInd, spot);
                }
                pawn.pather.StartPath(spot, PathEndMode.OnCell);
            };
            go.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return go;

            Toil hang = ToilMaker.MakeToil("DateNightDateHang");
            hang.socialMode = RandomSocialMode.SuperActive;
            hang.defaultCompleteMode = ToilCompleteMode.Never;
            hang.handlingFacing = true;
            hang.initAction = () =>
            {
                inHangPhase = true;
                if (ticksLeft <= 0)
                {
                    ticksLeft = Rand.RangeInclusive(2000, 4000);
                    totalTicks = ticksLeft;
                }
                if (NeedsItemFetch() && activity != DateActivity.Gift && eatTicksLeft < 0)
                {
                    eatTicksLeft = EatDurationTicks;
                }
                pawn.pather?.StopDead();
            };
            hang.tickAction = () =>
            {
                Pawn partner = Partner;
                if (partner == null || partner.Dead || !partner.Spawned)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (activity == DateActivity.Walk)
                {
                    WalkTick(partner);
                }
                else if (pawn.Position.DistanceToSquared(partner.Position) > 64)
                {
                    pawn.pather.StartPath(partner, PathEndMode.Touch);
                    return;
                }

                if (activity == DateActivity.Stargaze)
                {
                    // Same trick as vanilla skygazing: lie face-up while parked.
                    pawn.jobs.posture = pawn.pather.MovingNow
                        ? PawnPosture.Standing
                        : PawnPosture.LayingOnGroundFaceUp;
                }

                if (!pawn.pather.MovingNow)
                {
                    if (activity == DateActivity.Dance)
                    {
                        DanceTick();
                    }
                    else if (activity != DateActivity.Stargaze)
                    {
                        pawn.rotationTracker.FaceCell(partner.Position);
                    }
                }

                if (activity == DateActivity.Gift && !giftDelivered)
                {
                    GiftTick(partner);
                }
                if (eatTicksLeft > 0 && !pawn.pather.MovingNow)
                {
                    EatTick();
                }

                PassiveJoyTick();

                ticksLeft--;
                if (ticksLeft <= 0)
                {
                    ReadyForNextToil();
                    return;
                }

                if (pawn.IsHashIntervalTick(TicksBetweenChat) && pawn.interactions != null
                    && pawn.Position.DistanceToSquared(partner.Position) <= 64)
                {
                    pawn.interactions.TryInteractWith(partner, InteractionDefOf.Chitchat);
                }
                if (pawn.IsHashIntervalTick(TicksBetweenHeartMotes) && pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                }
            };
            hang.AddFinishAction(() =>
            {
                inHangPhase = false;
                if (activity == DateActivity.Stargaze && pawn.jobs != null)
                {
                    pawn.jobs.posture = PawnPosture.Standing;
                }
                DropUneatenItem();
                if (ticksLeft <= 0 && totalTicks > 0)
                {
                    DateNightDateUtility.NotifyDateFinished(pawn, Partner, activity, job.GetTarget(SpotInd));
                }
                else if (totalTicks > 0 && totalTicks - ticksLeft > 600)
                {
                    DateNightDateUtility.NotifyDateInterrupted(pawn, Partner);
                }
            });
            yield return hang;
        }

        private void ResolveActivity()
        {
            if (activity != DateActivity.Unresolved)
            {
                return;
            }

            Pawn partner = Partner;
            activity = DateNightActivities.Resolve(pawn, partner);

            // Only the initiator hands over a gift; the partner just shows up.
            if (activity == DateActivity.Gift && !DateNightActivities.IsInitiator(pawn, partner))
            {
                activity = DateActivity.Hangout;
            }

            Thing item = null;
            if (activity == DateActivity.Dinner || activity == DateActivity.Picnic)
            {
                item = DateNightActivities.FindMealFor(pawn, partner);
                if (item == null)
                {
                    activity = DateActivity.Hangout;
                }
            }
            else if (activity == DateActivity.Gift)
            {
                item = DateNightActivities.FindGiftFor(pawn, partner);
                if (item == null)
                {
                    activity = DateActivity.Hangout;
                }
            }

            if (item != null)
            {
                if (pawn.Reserve(item, job, 10, 1, null, errorOnFailed: false))
                {
                    job.SetTarget(ItemInd, item);
                    job.count = 1;
                }
                else
                {
                    activity = DateActivity.Hangout;
                    item = null;
                }
            }

            LocalTargetInfo spot = DateNightActivities.FindSpotFor(activity, pawn, partner);
            if (spot.IsValid)
            {
                job.SetTarget(SpotInd, spot);
            }
        }

        /// <summary>
        /// Shape-stable across save/load: depends only on the resolved activity
        /// and the persisted item target, never on runtime progress flags.
        /// </summary>
        private bool NeedsItemFetch()
        {
            if (!job.GetTarget(ItemInd).IsValid)
            {
                return false;
            }
            return activity == DateActivity.Dinner
                || activity == DateActivity.Picnic
                || activity == DateActivity.Gift;
        }

        private void WalkTick(Pawn partner)
        {
            bool leader = DateNightActivities.IsInitiator(pawn, partner);
            if (leader)
            {
                walkTicksLeft--;
                if (walkTicksLeft <= 0 && !pawn.pather.MovingNow)
                {
                    walkTicksLeft = WalkWaypointTicks;
                    if (CellFinder.TryFindRandomCellNear(pawn.Position, pawn.Map, 12,
                            c => c.InBounds(pawn.Map) && c.Standable(pawn.Map)
                                && c.GetDangerFor(pawn, pawn.Map) == Danger.None
                                && pawn.CanReach(c, PathEndMode.OnCell, Danger.Some),
                            out IntVec3 next))
                    {
                        Job cur = pawn.CurJob;
                        cur.locomotionUrgency = LocomotionUrgency.Amble;
                        pawn.pather.StartPath(next, PathEndMode.OnCell);
                    }
                }
            }
            else if (pawn.Position.DistanceToSquared(partner.Position) > 9 && !pawn.pather.MovingNow)
            {
                Job cur = pawn.CurJob;
                cur.locomotionUrgency = LocomotionUrgency.Amble;
                pawn.pather.StartPath(partner, PathEndMode.Touch);
            }
        }

        private void DanceTick()
        {
            if (pawn.IsHashIntervalTick(DanceTurnTicks))
            {
                pawn.Rotation = Rot4.Random;
            }
            if (pawn.IsHashIntervalTick(90) && pawn.Map != null)
            {
                FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart, 0.6f);
            }
        }

        private void GiftTick(Pawn partner)
        {
            if (pawn.Position.DistanceToSquared(partner.Position) > 4)
            {
                // Close the gap ourselves: the generic follow logic only kicks in
                // past 8 cells, but a handover needs us adjacent.
                if (!pawn.pather.MovingNow)
                {
                    pawn.pather.StartPath(partner, PathEndMode.Touch);
                }
                return;
            }

            Thing carried = pawn.carryTracker?.CarriedThing;
            if (carried == null)
            {
                giftDelivered = true;
                return;
            }

            if (pawn.carryTracker.innerContainer.TryTransferToContainer(
                    carried, partner.inventory.innerContainer, 1) > 0)
            {
                giftDelivered = true;
                DateNightDateUtility.NotifyGiftGiven(pawn, partner);
                if (pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(partner.Position, pawn.Map, FleckDefOf.Heart, 1.2f);
                }
            }
            else
            {
                giftDelivered = true;
            }
        }

        private void EatTick()
        {
            Thing meal = pawn.carryTracker?.CarriedThing;
            if (meal == null)
            {
                eatTicksLeft = -1;
                return;
            }

            eatTicksLeft--;
            if (eatTicksLeft == EatDurationTicks / 2
                && meal.def.ingestible?.ingestSound != null && pawn.Map != null)
            {
                meal.def.ingestible.ingestSound.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }
            if (eatTicksLeft > 0)
            {
                return;
            }

            eatTicksLeft = -1;
            float wanted = pawn.needs?.food?.NutritionWanted ?? 1f;
            if (wanted < 0.3f)
            {
                wanted = 0.3f;
            }
            float nutrition = meal.Ingested(pawn, wanted);
            if (pawn.needs?.food != null)
            {
                pawn.needs.food.CurLevel += nutrition;
            }
        }

        private void PassiveJoyTick()
        {
            if (pawn.needs?.joy == null)
            {
                return;
            }
            JoyKindDef kind = activity == DateActivity.Stargaze
                ? JoyKindDefOf.Meditative
                : JoyKindDefOf.Social;
            float rate = activity == DateActivity.Recreation ? PassiveJoyPerTick * 2f : PassiveJoyPerTick;
            pawn.needs.joy.GainJoy(rate, kind);
        }

        private void DropUneatenItem()
        {
            Thing carried = pawn.carryTracker?.CarriedThing;
            if (carried != null)
            {
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
            }
        }
    }
}

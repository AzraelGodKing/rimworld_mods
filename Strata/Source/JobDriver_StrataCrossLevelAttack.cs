using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    public class JobDriver_StrataCrossLevelAttack : JobDriver
    {
        private Thing target;
        private bool warmedUp;
        private int warmupTicksLeft = -1;
        private int burstShotsLeft;
        private int ticksToNextShot;
        private int cooldownTicksLeft;
        private Verb_LaunchProjectile cachedVerb;
        private int verbStaleAt;
        private int lofCheckIn;

        public Thing AttackTarget => target;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public override string GetReport()
        {
            if (target != null && !target.Destroyed)
            {
                return "Strata_ReportAttackingAcross".Translate(target.LabelShort);
            }
            return base.GetReport();
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            if (target == null
                && StrataCrossLevelCombat.PendingTargets.TryGetValue(pawn.thingIDNumber, out Thing pending))
            {
                target = pending;
            }
            StrataCrossLevelCombat.PendingTargets.Remove(pawn.thingIDNumber);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref target, "Strata_xlTarget");
            Scribe_Values.Look(ref warmedUp, "Strata_xlWarmed");
            Scribe_Values.Look(ref warmupTicksLeft, "Strata_xlWarmup", -1);
            Scribe_Values.Look(ref burstShotsLeft, "Strata_xlBurst");
            Scribe_Values.Look(ref ticksToNextShot, "Strata_xlNextShot");
            Scribe_Values.Look(ref cooldownTicksLeft, "Strata_xlCooldown");
        }

        private Verb_LaunchProjectile ResolvedVerb()
        {
            int now = Find.TickManager.TicksGame;
            if (cachedVerb != null && now < verbStaleAt)
            {
                Thing eq = cachedVerb.EquipmentSource;
                if (eq == null || !eq.Destroyed) return cachedVerb;
            }
            cachedVerb = StrataCrossLevelCombat.GetRangedVerb(pawn);
            verbStaleAt = now + 15;
            return cachedVerb;
        }

        private bool Valid()
        {
            if (!StrataCrossLevelCombat.Enabled) return false;
            if (target == null || target.Destroyed || !target.Spawned) return false;
            if (pawn.Downed || pawn.Dead) return false;
            // Forced player orders still require draft; auto-engage may run undrafted.
            if (job.playerForced && pawn.IsColonistPlayerControlled && !pawn.Drafted) return false;
            return ResolvedVerb() != null;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !Valid());
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            Toil fire = ToilMaker.MakeToil("Strata_CrossFire");
            fire.defaultCompleteMode = ToilCompleteMode.Never;
            fire.handlingFacing = true;
            fire.tickAction = FireTick;
            yield return fire;
        }

        private void FireTick()
        {
            Verb_LaunchProjectile verb = ResolvedVerb();
            if (verb == null)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (--lofCheckIn <= 0)
            {
                if (!StrataCrossLevelCombat.CanFireFrom(pawn.Map, pawn.Position, target, verb, out _))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                lofCheckIn = 15;
            }

            pawn.rotationTracker.FaceCell(target.Position);

            if (cooldownTicksLeft > 0)
            {
                cooldownTicksLeft--;
                return;
            }

            if (!warmedUp)
            {
                if (warmupTicksLeft < 0)
                {
                    float warm = verb.verbProps.warmupTime;
                    try { warm *= pawn.GetStatValue(StatDefOf.AimingDelayFactor); }
                    catch { /* ignore */ }
                    warmupTicksLeft = SecondsToTicks(warm);
                }
                if (warmupTicksLeft > 0)
                {
                    warmupTicksLeft--;
                    return;
                }
                warmedUp = true;
            }

            if (burstShotsLeft <= 0)
            {
                burstShotsLeft = Mathf.Max(1, verb.verbProps.burstShotCount);
                ticksToNextShot = 0;
            }
            if (ticksToNextShot > 0)
            {
                ticksToNextShot--;
                return;
            }

            if (!StrataCrossLevelCombat.Fire(pawn, verb, target))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            burstShotsLeft--;
            if (burstShotsLeft > 0)
            {
                ticksToNextShot = Mathf.Max(0, verb.verbProps.ticksBetweenBurstShots);
                return;
            }

            cooldownTicksLeft = CooldownTicks(verb, pawn);
            warmedUp = false;
            warmupTicksLeft = -1;
        }

        private static int CooldownTicks(Verb verb, Pawn p)
        {
            try
            {
                return Mathf.Max(1, SecondsToTicks(verb.verbProps.AdjustedCooldown(verb, p)));
            }
            catch
            {
                return Mathf.Max(1, SecondsToTicks(verb.verbProps.defaultCooldownTime));
            }
        }

        private static int SecondsToTicks(float seconds) =>
            Mathf.Max(0, Mathf.RoundToInt(seconds * 60f));
    }
}

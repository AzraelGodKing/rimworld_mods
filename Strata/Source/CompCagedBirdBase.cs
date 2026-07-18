using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    public class CompProperties_CagedBird : CompProperties
    {
        public float assignRadius = 12f;

        public CompProperties_CagedBird()
        {
            compClass = typeof(CompCagedBirdBase);
        }
    }

    public abstract class CompCagedBirdBase : ThingComp
    {
        protected Pawn contained;

        protected CompProperties_CagedBird Props => (CompProperties_CagedBird)props;

        public Pawn ContainedBird => contained;

        public bool HasLiveBird => contained != null && contained.Spawned && !contained.Dead;

        public static bool IsContained(Pawn pawn) => StrataCagedBirdRegistry.IsContained(pawn);

        protected static bool UsesSustainMode => StrataCagedBirdUtility.UsesSustainMode;

        protected abstract bool CanAccept(Pawn pawn);

        protected abstract string OccupantNoun { get; }

        protected abstract IEnumerable<Pawn> EnumerateAssignCandidates();

        protected virtual void OnRareTick()
        {
        }

        protected virtual IEnumerable<Gizmo> ExtraEmptyGizmos()
        {
            yield break;
        }

        protected virtual IEnumerable<Gizmo> ExtraOccupiedGizmos()
        {
            yield break;
        }

        protected virtual string EmptyInspectExtra()
        {
            return $"Empty — assign a nearby {OccupantNoun}.";
        }

        protected virtual string OccupiedInspectExtra()
        {
            return StrataCagedBirdUtility.FeedingInspectHint(parent, contained);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (HasLiveBird)
            {
                StrataCagedBirdRegistry.Register(contained);
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            ReleaseContained();
            base.PostDeSpawn(map, mode);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref contained, "strataCagedBird");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (HasLiveBird)
                {
                    StrataCagedBirdRegistry.Register(contained);
                }
                else if (contained != null && contained.Dead)
                {
                    StrataCagedBirdRegistry.Unregister(contained);
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned)
            {
                return;
            }
            if (contained != null && contained.Dead)
            {
                StrataCagedBirdRegistry.Unregister(contained);
            }
            if (!HasLiveBird)
            {
                return;
            }
            if (contained.Map != parent.Map)
            {
                ReleaseContained();
                return;
            }
            StrataCagedBirdUtility.MaintainPosition(parent, contained);
            StrataCagedBirdUtility.FeedCagedBird(parent, contained);
            if ((Find.TickManager.TicksGame + parent.thingIDNumber) % 45 != 0)
            {
                return;
            }
            OnRareTick();
        }

        protected virtual string GetAssignRejectReason(Pawn pawn) => null;

        public void TryAssign(Pawn pawn)
        {
            if (!CanAccept(pawn))
            {
                string reason = GetAssignRejectReason(pawn);
                if (!reason.NullOrEmpty())
                {
                    Messages.Message(
                        reason,
                        pawn ?? (Thing)parent,
                        MessageTypeDefOf.RejectInput,
                        historical: false);
                }
                return;
            }
            if (HasLiveBird)
            {
                Messages.Message(
                    "Strata_Cage_AlreadyHolds".Translate(parent.LabelShort, OccupantNoun),
                    parent,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }
            if (!StrataCagedBirdUtility.CanAssignToColonyCage(
                    pawn,
                    parent.Map,
                    parent.Position,
                    Props.assignRadius,
                    out string rejectReason))
            {
                Messages.Message(
                    rejectReason,
                    pawn,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }
            contained = pawn;
            StrataCagedBirdRegistry.Register(pawn);
            pawn.jobs?.StopAll();
            pawn.pather?.StopDead();
            pawn.Position = parent.Position;
            StrataCagedBirdUtility.FeedCagedBird(parent, pawn);
            Messages.Message(
                "Strata_Cage_Assigned".Translate(pawn.LabelShort, parent.LabelShort),
                parent,
                MessageTypeDefOf.PositiveEvent);
        }

        public void ReleaseContained()
        {
            if (contained == null)
            {
                return;
            }
            StrataCagedBirdRegistry.Unregister(contained);
            if (HasLiveBird)
            {
                contained.jobs?.StopAll();
            }
            contained = null;
        }

        private IEnumerable<FloatMenuOption> AssignOptions()
        {
            bool any = false;
            foreach (Pawn pawn in EnumerateAssignCandidates())
            {
                any = true;
                Pawn local = pawn;
                yield return new FloatMenuOption(
                    "Strata_Cage_Assign".Translate(pawn.LabelShort),
                    () => TryAssign(local));
            }
            if (!any)
            {
                yield return new FloatMenuOption("Strata_Cage_NoNearby".Translate(OccupantNoun), null);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (HasLiveBird)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Strata_Cage_Release".Translate(OccupantNoun),
                    defaultDesc = "Strata_Cage_ReleaseDesc".Translate(OccupantNoun),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/ReleaseAnimalToWild", reportFailure: false),
                    action = () =>
                    {
                        Pawn released = contained;
                        ReleaseContained();
                        Messages.Message(
                            "Strata_Cage_Released".Translate(released.LabelShort, parent.LabelShort),
                            parent,
                            MessageTypeDefOf.NeutralEvent);
                    },
                };
                foreach (Gizmo gizmo in ExtraOccupiedGizmos())
                {
                    yield return gizmo;
                }
                yield break;
            }
            yield return new Command_Action
            {
                defaultLabel = "Strata_Cage_AssignLabel".Translate(OccupantNoun),
                defaultDesc = UsesSustainMode
                    ? "Strata_Cage_AssignDescSustain".Translate(OccupantNoun)
                    : "Strata_Cage_AssignDescFeed".Translate(OccupantNoun),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/ReleaseAnimalToWild", reportFailure: false),
                action = () => Find.WindowStack.Add(new FloatMenu(AssignOptions().ToList())),
            };
            foreach (Gizmo gizmo in ExtraEmptyGizmos())
            {
                yield return gizmo;
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!HasLiveBird)
            {
                if (contained != null && contained.Dead)
                {
                    return "Strata_Cage_DeadBody".Translate(OccupantNoun);
                }
                return EmptyInspectExtra();
            }
            return OccupiedInspectExtra();
        }
    }
}

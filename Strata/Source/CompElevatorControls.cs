using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    public class CompProperties_ElevatorControls : CompProperties
    {
        public CompProperties_ElevatorControls()
        {
            compClass = typeof(CompElevatorControls);
        }
    }

    // Call / hold / per-landing priority for powered elevators (AZR-64).
    public class CompElevatorControls : ThingComp
    {
        public const int MaxPriority = 5;

        private bool holdAtLevel;
        private int floorPriority = 3;

        public bool HoldAtLevel => holdAtLevel;

        public int FloorPriority => floorPriority;

        public static CompElevatorControls On(Thing thing)
        {
            return thing?.TryGetComp<CompElevatorControls>();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref holdAtLevel, "strataElevatorHold", defaultValue: false);
            Scribe_Values.Look(ref floorPriority, "strataElevatorPriority", defaultValue: 3);
        }

        public override string CompInspectStringExtra()
        {
            string pri = "Strata_ElevatorPriorityInspect".Translate(floorPriority);
            if (holdAtLevel)
            {
                return pri + "\n" + "Strata_ElevatorHeldInspect".Translate();
            }
            return pri;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent is not MapPortal portal)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "Strata_ElevatorCall".Translate(),
                defaultDesc = "Strata_ElevatorCallDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", reportFailure: false)
                    ?? BaseContent.BadTex,
                action = () => CallSelected(portal),
            };

            yield return new Command_Toggle
            {
                defaultLabel = "Strata_ElevatorHold".Translate(),
                defaultDesc = "Strata_ElevatorHoldDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", reportFailure: false)
                    ?? BaseContent.BadTex,
                isActive = () => holdAtLevel,
                toggleAction = () => holdAtLevel = !holdAtLevel,
            };

            yield return new Command_Action
            {
                defaultLabel = "Strata_ElevatorPriority".Translate(floorPriority),
                defaultDesc = "Strata_ElevatorPriorityDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", reportFailure: false)
                    ?? BaseContent.BadTex,
                action = () =>
                {
                    floorPriority++;
                    if (floorPriority > MaxPriority)
                    {
                        floorPriority = 1;
                    }
                },
            };
        }

        private static void CallSelected(MapPortal portal)
        {
            if (portal == null || !portal.Spawned)
            {
                return;
            }

            int ordered = 0;
            List<Pawn> selected = Find.Selector.SelectedPawns;
            for (int i = 0; i < selected.Count; i++)
            {
                Pawn pawn = selected[i];
                if (!StrataPawnUtility.CanUseLevelPortals(pawn)
                    || pawn.Map != portal.Map
                    || !portal.IsEnterable(out _)
                    || !pawn.CanReach(portal, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                Job job = JobMaker.MakeJob(JobDefOf.EnterPortal, portal);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                ordered++;
            }

            if (ordered == 0)
            {
                Messages.Message(
                    "Strata_ElevatorCallNone".Translate(),
                    portal,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
            }
        }
    }
}

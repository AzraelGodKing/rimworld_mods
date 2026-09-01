using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_ElevatorControls : CompProperties
    {
        public CompProperties_ElevatorControls()
        {
            compClass = typeof(CompElevatorControls);
        }
    }

    // Call / hold / per-floor priority on a powered elevator shaft.
    // Data lives on the player-built car (down or tower); landings forward gizmos.
    public class CompElevatorControls : ThingComp
    {
        public const int MinPriority = 1;

        public const int MaxPriority = 5;

        public const int DefaultPriority = 3;

        private const int CallPreferTicks = 7500;

        private int priority = DefaultPriority;

        private bool held;

        private int lastCallTick = -99999;

        private int lastCallMapId = -1;

        public int Priority
        {
            get => Master.priority;
            set => Master.priority = Mathf.Clamp(value, MinPriority, MaxPriority);
        }

        public bool Held
        {
            get => Master.held;
            set => Master.held = value;
        }

        public CompElevatorControls Master
        {
            get
            {
                if (IsMaster(parent))
                {
                    return this;
                }
                if (parent is PocketMapExit exit
                    && exit.entrance != null
                    && !exit.entrance.Destroyed)
                {
                    CompElevatorControls other = exit.entrance.TryGetComp<CompElevatorControls>();
                    if (other != null)
                    {
                        return other;
                    }
                }
                return this;
            }
        }

        public static bool IsMaster(Thing thing)
        {
            return thing is Building_ElevatorDown || thing is Building_ElevatorBuildUp;
        }

        public static bool BlocksPoweredEntry(MapPortal portal)
        {
            if (portal == null)
            {
                return false;
            }
            if (portal is Building_ElevatorUp || portal is Building_ElevatorBuildUpLanding)
            {
                return false;
            }
            CompElevatorControls controls = portal.TryGetComp<CompElevatorControls>();
            return controls != null && controls.Held;
        }

        public static int BestPriorityOn(Map map)
        {
            if (map == null)
            {
                return DefaultPriority;
            }
            int best = MinPriority - 1;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                CompElevatorControls controls = thing.TryGetComp<CompElevatorControls>();
                if (controls == null)
                {
                    continue;
                }
                int p = controls.Master.priority;
                if (p > best)
                {
                    best = p;
                }
            }
            return best < MinPriority ? DefaultPriority : best;
        }

        public static bool RecentlyCalledToward(MapPortal portal, Map hop, Map target)
        {
            CompElevatorControls controls = portal?.TryGetComp<CompElevatorControls>();
            if (controls == null)
            {
                return false;
            }
            CompElevatorControls master = controls.Master;
            if (Find.TickManager.TicksGame - master.lastCallTick > CallPreferTicks)
            {
                return false;
            }
            int called = master.lastCallMapId;
            return (hop != null && called == hop.uniqueID)
                || (target != null && called == target.uniqueID);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (!IsMaster(parent) && Scribe.mode == LoadSaveMode.Saving)
            {
                return;
            }
            Scribe_Values.Look(ref priority, "strataElevatorPriority", DefaultPriority);
            Scribe_Values.Look(ref held, "strataElevatorHeld", false);
            Scribe_Values.Look(ref lastCallTick, "strataElevatorLastCallTick", -99999);
            Scribe_Values.Look(ref lastCallMapId, "strataElevatorLastCallMapId", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                priority = Mathf.Clamp(priority, MinPriority, MaxPriority);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "Strata_ElevatorCall".Translate(),
                defaultDesc = "Strata_ElevatorCallDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", reportFailure: false)
                    ?? BaseContent.BadTex,
                action = CallToThisFloor,
            };
            yield return new Command_Action
            {
                defaultLabel = "Strata_ElevatorPriority".Translate(Priority),
                defaultDesc = "Strata_ElevatorPriorityDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Copy", reportFailure: false)
                    ?? BaseContent.BadTex,
                action = CyclePriority,
            };
            yield return new Command_Toggle
            {
                defaultLabel = "Strata_ElevatorHold".Translate(),
                defaultDesc = "Strata_ElevatorHoldDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire", reportFailure: false)
                    ?? ContentFinder<Texture2D>.Get("UI/Commands/Halt", reportFailure: false)
                    ?? BaseContent.BadTex,
                isActive = () => Held,
                toggleAction = () => Held = !Held,
            };
        }

        public override string CompInspectStringExtra()
        {
            string line = "Strata_ElevatorPriorityInspect".Translate(Priority);
            if (Held)
            {
                line += "\n" + "Strata_ElevatorHeldInspect".Translate();
            }
            return line;
        }

        private void CallToThisFloor()
        {
            CompElevatorControls master = Master;
            master.lastCallTick = Find.TickManager.TicksGame;
            master.lastCallMapId = parent.Map?.uniqueID ?? -1;
            Messages.Message(
                "Strata_ElevatorCalled".Translate(LevelName(parent.Map)),
                parent,
                MessageTypeDefOf.TaskCompletion);
        }

        private void CyclePriority()
        {
            int next = Priority + 1;
            if (next > MaxPriority)
            {
                next = MinPriority;
            }
            Priority = next;
            Messages.Message(
                "Strata_ElevatorPrioritySet".Translate(Priority),
                parent,
                MessageTypeDefOf.SilentInput,
                historical: false);
        }

        private static string LevelName(Map map)
        {
            if (map == null)
            {
                return "Level";
            }
            string custom = StrataLevelLabels.Get?.GetLabel(map);
            if (!custom.NullOrEmpty())
            {
                return custom;
            }
            int altitude = StrataDepth.Altitude(map);
            if (altitude == 0)
            {
                string name = map.Parent?.LabelCap;
                return name.NullOrEmpty() ? "Strata_LevelSurface".Translate() : name;
            }
            if (altitude > 0)
            {
                return "Strata_LevelAbove".Translate(altitude);
            }
            return "Strata_LevelBelow".Translate(altitude);
        }
    }
}

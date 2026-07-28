using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Carry a minified building through stairs to its Blueprint_Install on a
    // linked floor. Generic LevelDemand skips install blueprints (specific
    // minified reference); this workgiver is the dedicated path.
    public class WorkGiver_InstallAcrossLevels : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.MinifiedThing);

        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.haulAcrossLevelsEnabled)
            {
                return true;
            }
            return !LevelGraph.AnyLinkFrom(pawn.Map);
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Thing> things = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
            for (int i = 0; i < things.Count; i++)
            {
                yield return things[i];
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return TryFindInstallTarget(pawn, t, forced, out _, out _);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!TryFindInstallTarget(pawn, t, forced, out MapPortal portal, out Map destMap))
            {
                return null;
            }

            HaulToLevelTargets.Remember(pawn, destMap, pawn.Map);
            Job job = JobMaker.MakeJob(StrataDefOf.Strata_HaulToLevel, t, portal);
            job.count = 1;
            return job;
        }

        private static bool TryFindInstallTarget(
            Pawn pawn,
            Thing t,
            bool forced,
            out MapPortal portal,
            out Map destMap)
        {
            portal = null;
            destMap = null;
            if (t is not MinifiedThing mini || !mini.Spawned || mini.Map != pawn.Map)
            {
                return false;
            }
            if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, mini, forced))
            {
                return false;
            }

            // Prefer local install if one exists — vanilla handles same-map.
            if (FindInstallBlueprintFor(pawn.Map, mini) != null)
            {
                return false;
            }

            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(pawn.Map);
            for (int i = 0; i < links.Count; i++)
            {
                LevelGraph.LevelLink link = links[i];
                Blueprint_Install bp = FindInstallBlueprintFor(link.map, mini);
                if (bp == null)
                {
                    continue;
                }
                if (!link.arrivalCell.IsValid || !link.arrivalCell.InBounds(link.map)
                    || !link.map.reachability.CanReach(
                        link.arrivalCell, bp.Position, PathEndMode.Touch,
                        TraverseParms.For(TraverseMode.PassDoors)))
                {
                    continue;
                }

                MapPortal step = LevelGraph.BestFirstStep(pawn.Map, link.map, pawn.Position, pawn)
                    ?? link.firstStep;
                if (step == null || !step.Spawned || !step.IsEnterable(out _)
                    || !pawn.CanReach(step, PathEndMode.Touch, Danger.Some))
                {
                    continue;
                }

                portal = step;
                destMap = link.map;
                return true;
            }

            return false;
        }

        internal static Blueprint_Install FindInstallBlueprintFor(Map map, MinifiedThing mini)
        {
            if (map == null || mini == null)
            {
                return null;
            }
            List<Thing> blueprints = map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint);
            for (int i = 0; i < blueprints.Count; i++)
            {
                if (blueprints[i] is not Blueprint_Install bp || bp.Faction != Faction.OfPlayer)
                {
                    continue;
                }
                Thing target = bp.MiniToInstallOrBuildingToReinstall;
                if (target == mini || (target != null && target.thingIDNumber == mini.thingIDNumber))
                {
                    return bp;
                }
            }
            return null;
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    public sealed class CrossFloorIntentRecord : IExposable
    {
        public int pawnId;
        public int destMapId;
        public RelayPurpose purpose;
        public int preferredBedId = -1;
        public int returnMapId = -1;
        public int carriedPawnId = -1;
        public IntVec3 preferArrivalNear = IntVec3.Invalid;
        public string intendedWorkDefName;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref destMapId, "destMapId");
            Scribe_Values.Look(ref purpose, "purpose");
            Scribe_Values.Look(ref preferredBedId, "preferredBedId", -1);
            Scribe_Values.Look(ref returnMapId, "returnMapId", -1);
            Scribe_Values.Look(ref carriedPawnId, "carriedPawnId", -1);
            Scribe_Values.Look(ref preferArrivalNear, "preferArrivalNear", IntVec3.Invalid);
            Scribe_Values.Look(ref intendedWorkDefName, "intendedWorkDefName");
        }
    }

    public sealed class CrossFloorRouteRecord : IExposable
    {
        public int pawnId;
        public int destMapId;
        public IntVec3 destCell;
        public int returnPortalId;
        public int stage;
        public int finish;
        public int bedId = -1;
        public int destPortalId;
        public int bedMapId;
        public bool requireDrafted;
        public RelayPurpose handoff;
        public int handoffMapId = -1;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref destMapId, "destMapId");
            Scribe_Values.Look(ref destCell, "destCell");
            Scribe_Values.Look(ref returnPortalId, "returnPortalId");
            Scribe_Values.Look(ref stage, "stage");
            Scribe_Values.Look(ref finish, "finish");
            Scribe_Values.Look(ref bedId, "bedId", -1);
            Scribe_Values.Look(ref destPortalId, "destPortalId");
            Scribe_Values.Look(ref bedMapId, "bedMapId");
            Scribe_Values.Look(ref requireDrafted, "requireDrafted");
            Scribe_Values.Look(ref handoff, "handoff");
            Scribe_Values.Look(ref handoffMapId, "handoffMapId", -1);
        }
    }

    public sealed class CrossFloorOrderedJobRecord : IExposable
    {
        public int pawnId;
        public int destMapId;
        public Job job;
        public JobTag tag;
        public bool hasTag;
        public bool prioritized;
        public string giverDefName;
        public IntVec3 cell = IntVec3.Invalid;
        public int attempts;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref destMapId, "destMapId");
            Scribe_Deep.Look(ref job, "job");
            Scribe_Values.Look(ref tag, "tag");
            Scribe_Values.Look(ref hasTag, "hasTag");
            Scribe_Values.Look(ref prioritized, "prioritized");
            Scribe_Values.Look(ref giverDefName, "giverDefName");
            Scribe_Values.Look(ref cell, "cell", IntVec3.Invalid);
            Scribe_Values.Look(ref attempts, "attempts");
        }
    }

    public sealed class JobRelayAssignmentRecord : IExposable
    {
        public int pawnId;
        public int mapId;
        public string workDefName;
        public int stuckSince;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref mapId, "mapId");
            Scribe_Values.Look(ref workDefName, "workDefName");
            Scribe_Values.Look(ref stuckSince, "stuckSince");
        }
    }

    public sealed class CrossFloorArrivalWorkRecord : IExposable
    {
        public int pawnId;
        public string workDefName;
        public int untilTick;
        public int mapId;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref workDefName, "workDefName");
            Scribe_Values.Look(ref untilTick, "untilTick");
            Scribe_Values.Look(ref mapId, "mapId");
        }
    }

    // PortalRelayChain and DraftedPortalPathing keep trip state in static
    // dictionaries. FinalizeInit clears those on every load; this component
    // writes them into the save and puts back the ones that still make sense
    // from LoadedGame, which runs after that clear.
    public class GameComponent_StrataCrossFloorTrips : GameComponent
    {
        private List<CrossFloorIntentRecord> intents;
        private List<CrossFloorRouteRecord> routes;
        private List<CrossFloorOrderedJobRecord> orderedJobs;
        private List<CrossFloorArrivalWorkRecord> arrivalWork;
        private List<JobRelayAssignmentRecord> jobRelay;

        public GameComponent_StrataCrossFloorTrips(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                intents = PortalRelayChain.CaptureIntents();
                routes = DraftedPortalPathing.CaptureRoutes();
                orderedJobs = CrossLevelOrderedJobs.CaptureOrdered();
                arrivalWork = PortalRelayChain.CaptureArrivalWork();
                jobRelay = JobRelay.Capture();
            }
            Scribe_Collections.Look(ref intents, "strataRelayIntents", LookMode.Deep);
            Scribe_Collections.Look(ref routes, "strataDraftedRoutes", LookMode.Deep);
            Scribe_Collections.Look(ref orderedJobs, "strataOrderedJobs", LookMode.Deep);
            Scribe_Collections.Look(ref arrivalWork, "strataArrivalWork", LookMode.Deep);
            Scribe_Collections.Look(ref jobRelay, "strataJobRelay", LookMode.Deep);
        }

        public override void LoadedGame()
        {
            CrossLevelOrderedJobs.RestoreOrdered(orderedJobs);
            PortalRelayChain.RestoreIntents(intents);
            JobRelay.Restore(jobRelay);
            if (arrivalWork != null)
            {
                for (int i = 0; i < arrivalWork.Count; i++)
                {
                    CrossFloorArrivalWorkRecord record = arrivalWork[i];
                    if (record == null)
                    {
                        continue;
                    }
                    PortalRelayChain.RememberArrivalWork(record.pawnId, record.workDefName, record.untilTick, record.mapId);
                    JobRelay.Remember(record.pawnId, record.mapId, record.workDefName);
                }
            }
            DraftedPortalPathing.RestoreRoutes(routes);
            intents = null;
            routes = null;
            orderedJobs = null;
            arrivalWork = null;
            jobRelay = null;
        }
    }
}

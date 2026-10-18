using System;
using Verse;

namespace Strata
{
    public static class StrataSessionReset
    {
        static readonly Action[] ResetAll =
        {
            StrataLevelPerfUtility.ResetSession,
            StrataPawnUtility.ResetMiscRobotCaches,
            StrataBelowSelection.ResetSession,
            StrataCrossLevelCombat.ResetSession,
            StrataRoomUtility.ResetSession,
            VefPipeSystemBackend.ResetSession,
            VteAcPipeBackend.ResetSession,
            StrataGravshipPortalTravel.ResetSession,
            StrataGravshipDeckCargo.ResetSession,
            StrataCrossLevelAutoEngage.ResetSession,
            StrataResources.ClearCaches,
            WorkRelaySignals.ResetSession,
            WorkRelayBoardBuilder.ResetSession,
            SleepRelay.ResetSession,
            StrataConstructAcrossLevels.ResetSession,
            HaulToLevelTargets.ResetSession,
            RelayClaims.ResetSession,
            StrataMapUtility.ResetSession,
            StrataDeferredGenUtility.ResetSession,
            WorkRelayAntiLoop.ResetSession,
            StrataRobotDiagnostics.ResetSession,
            DraftedPortalPathing.ResetSession,
            StrataPortalUtility.ResetHaulDeliverSession,
            CrossLevelOrderedJobs.ResetSession,
            MapComponent_RaidPursuit.ResetSession,
            StrataCagedBirdRegistry.ResetSession,
            CaravanTravelUtility.ResetSession,
            RaidCoordinator.ResetSession,
            WorkRelayBoard.ResetSession,
            PortalRelayChain.ResetSession,
            JobRelay.ResetSession,
            Patch_EnterPortalKeepJobs.ResetSession,
            StrataFaultGuard.ResetSession,
            StrataPocketMapOpen.ResetSession,
            StrataCrossLevelThreatNotify.ResetSession,
            PawnRelay.ResetSession,
        };

        public static void Run()
        {
            for (int i = 0; i < ResetAll.Length; i++)
            {
                Action reset = ResetAll[i];
                try
                {
                    reset();
                }
                catch (Exception e)
                {
                    Log.Warning("[Strata] Session reset failed on "
                        + reset.Method.DeclaringType.FullName + "."
                        + reset.Method.Name + ": " + e);
                }
            }
        }
    }
}

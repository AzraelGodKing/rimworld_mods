using System;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Tick-stamped / ID-keyed statics from a previously loaded save are garbage
    // in the next one. They are swept before the new game's maps load or
    // generate: Map.FinalizeInit (map components) and spawn/PostLoadInit hooks
    // fill some of them, and all of that runs before Game.FinalizeInit.
    [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
    public static class Patch_SessionResetBeforeLoad
    {
        public static void Prefix()
        {
            StrataSessionReset.Run();
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.InitNewGame))]
    public static class Patch_SessionResetBeforeNewGame
    {
        public static void Prefix()
        {
            StrataSessionReset.Run();
        }
    }

    public static class StrataSessionReset
    {
        static readonly Action[] ResetAll =
        {
            StrataLevelPerfUtility.ResetSession,
            StrataPawnUtility.ResetMiscRobotCaches,
            StrataBelowSelection.ResetSession,
            StrataCrossLevelCombat.ResetSession,
            StrataRoomUtility.ResetSession,
            SealedPortalCells.ResetSession,
            GasPipeUtility.ResetSession,
            CompShaftFluidTie.ResetSession,
            StrataThingLookup.ResetSession,
            VefPipeSystemBackend.ResetSession,
            VteAcPipeBackend.ResetSession,
            StrataGravshipPortalTravel.ResetSession,
            StrataGravshipDeckCargo.ResetSession,
            StrataCrossLevelAutoEngage.ResetSession,
            StrataResources.ClearCaches,
            WorkRelaySignals.ResetSession,
            WorkRelayBoardBuilder.ResetSession,
            AtmosphereMapComponent.ResetSession,
            SleepRelay.ResetSession,
            StrataConstructAcrossLevels.ResetSession,
            BillIngredientUtility.ResetSession,
            HaulToLevelTargets.ResetSession,
            RelayClaims.ResetSession,
            StrataMapUtility.ResetSession,
            StrataLevelCoords.ResetSession,
            StrataDeferredGenUtility.ResetSession,
            StrataLevelKinds.ResetSession,
            LevelWalkCost.ResetSession,
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
            WorkGiver_HaulAcrossLevels.ResetSession,
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

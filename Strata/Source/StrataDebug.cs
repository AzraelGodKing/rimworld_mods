using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace Strata
{
    // Dev-mode helpers (visible only with Development Mode on) for exercising
    // Strata's systems without waiting on the storyteller.
    public static class StrataDebug
    {
        private const string Cat = "Strata";

        private static void Fire(IncidentDef def)
        {
            Map map = Find.CurrentMap;
            if (map == null || def == null)
            {
                return;
            }
            var parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            if (def.Worker.CanFireNow(parms))
            {
                def.Worker.TryExecute(parms);
            }
            else
            {
                Messages.Message($"[Strata] {def.defName} can't fire on this map right now.",
                    MessageTypeDefOf.RejectInput, historical: false);
            }
        }

        [DebugAction(Cat, "Fire: cave-in", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireCaveIn() => Fire(StrataIncidentDefOf.Strata_CaveIn);

        [DebugAction(Cat, "Fire: gas pocket", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireGasPocket() => Fire(StrataIncidentDefOf.Strata_GasPocket);

        [DebugAction(Cat, "Fire: deep vein", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireDeepVein() => Fire(StrataIncidentDefOf.Strata_DeepVein);

        [DebugAction(Cat, "Fire: deep raid", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireDeepRaid() => Fire(StrataIncidentDefOf.Strata_DeepRaid);

        [DebugAction(Cat, "Fire: prospector's tip", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireProspector() => Fire(StrataIncidentDefOf.Strata_ProspectorTip);

        [DebugAction(Cat, "Fire: ground tremor", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireTremor() => Fire(StrataIncidentDefOf.Strata_Tremor);

        [DebugAction(Cat, "Log level depths", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogDepths()
        {
            var sb = new System.Text.StringBuilder("[Strata] Levels:\n");
            foreach (Map map in Find.Maps)
            {
                string kind = StrataMapUtility.IsUnderground(map) ? "underground" : "surface";
                sb.AppendLine($"  {map} - {kind}, depth {StrataDepth.Of(map)}, "
                    + $"outdoorTemp {map.mapTemperature.OutdoorTemp:F1}, "
                    + $"pawns {map.mapPawns.AllPawnsSpawned.Count}");
            }
            Log.Message(sb.ToString());
        }
    }
}

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
            // Dev buttons bypass the storyteller's pacing gates (ThreatBig
            // mercy windows, refire cooldowns, difficulty settings) - only the
            // incident's own requirements still apply.
            parms.forced = true;
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

        [DebugAction(Cat, "List smoke emitters", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ListSmokeEmitters()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }
            SmokeMapComponent smoke = map.GetComponent<SmokeMapComponent>();
            var sb = new System.Text.StringBuilder($"[Strata] Smoke emitters on {map}:\n");
            int emitters = 0;
            int unpatched = 0;
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                CompExhaust exhaust = b.GetComp<CompExhaust>();
                bool combustion = b.GetComp<CompPowerPlant>() != null && b.GetComp<CompRefuelable>() != null;
                if (exhaust != null)
                {
                    emitters++;
                    bool registered = smoke != null && smoke.Emitters.Contains(exhaust);
                    float density = smoke != null ? smoke.DensityInRoom(b.GetRoom()) : 0f;
                    sb.AppendLine($"  {b.LabelCap}: exhaust=YES registered={registered} active={exhaust.Active} "
                        + $"emit/cyc={exhaust.Props.emissionPerCycle} roomDensity={density:F2}");
                }
                else if (combustion)
                {
                    unpatched++;
                    sb.AppendLine($"  {b.LabelCap}: fuel-burning generator with NO exhaust comp - not covered! "
                        + "(add its defName to Patches/Exhaust_Strata.xml)");
                }
            }
            sb.AppendLine($"Total: {emitters} smoke emitter(s), {unpatched} uncovered fuel generator(s).");
            Log.Message(sb.ToString());
        }

        [DebugAction(Cat, "Log room smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogRoomSmoke()
        {
            Map map = Find.CurrentMap;
            SmokeMapComponent smoke = map?.GetComponent<SmokeMapComponent>();
            if (smoke != null)
            {
                Log.Message($"[Strata] Active smoke on {map}:\n{smoke.DebugSummary()}");
            }
        }

        [DebugAction(Cat, "Clear all smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearSmoke()
        {
            Find.CurrentMap?.GetComponent<SmokeMapComponent>()?.ClearAll();
        }

        [DebugAction(Cat, "Saturate room with smoke", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        private static void SaturateSmoke()
        {
            Find.CurrentMap?.GetComponent<SmokeMapComponent>()?.DebugSaturate(UI.MouseCell());
        }

        // The honest version of unit tests for a mod whose types need a live
        // game: invariant checks over the running colony, run from dev mode.
        [DebugAction(Cat, "Run self-tests", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RunSelfTests()
        {
            int passed = 0;
            int failed = 0;
            var sb = new System.Text.StringBuilder("[Strata] Self-tests:\n");

            void Check(string name, bool ok)
            {
                if (ok) { passed++; } else { failed++; }
                sb.AppendLine($"  {(ok ? "PASS" : "FAIL")}  {name}");
            }

            foreach (Map map in Find.Maps)
            {
                Check($"smoke component on {map}", map.GetComponent<SmokeMapComponent>() != null);
                Check($"pursuit component on {map}", map.GetComponent<MapComponent_RaidPursuit>() != null);
                if (!StrataMapUtility.IsUnderground(map))
                {
                    Check($"depth of surface {map} is 0", StrataDepth.Of(map) == 0);
                }

                var seen = new HashSet<Map>();
                bool noDupes = true, stepsValid = true, depthsPositive = true;
                foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
                {
                    if (link.map == map || !seen.Add(link.map)) { noDupes = false; }
                    if (link.firstStep == null || !link.firstStep.Spawned || link.firstStep.Map != map) { stepsValid = false; }
                    if (link.depth < 1) { depthsPositive = false; }
                }
                if (seen.Count > 0)
                {
                    Check($"level graph from {map}: no self/duplicate links", noDupes);
                    Check($"level graph from {map}: first steps spawned on source map", stepsValid);
                    Check($"level graph from {map}: depths start at 1", depthsPositive);
                    foreach (Map target in seen)
                    {
                        Check($"BestFirstStep {map} -> {target} resolves",
                            LevelGraph.BestFirstStep(map, target, map.Center) != null);
                    }
                }
            }
            Check("world ritual-travel component exists", StrataRitualTravel.Get != null);
            Check("settings loaded", StrataMod.Settings != null);

            sb.AppendLine($"Total: {passed} passed, {failed} failed.");
            if (failed > 0) { Log.Warning(sb.ToString()); } else { Log.Message(sb.ToString()); }
        }

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

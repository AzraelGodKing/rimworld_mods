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
            AtmosphereMapComponent smoke = map.GetComponent<AtmosphereMapComponent>();
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

        [DebugAction(Cat, "Log room gases", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogRoomGases()
        {
            Map map = Find.CurrentMap;
            AtmosphereMapComponent atmosphere = map?.GetComponent<AtmosphereMapComponent>();
            if (atmosphere != null)
            {
                Log.Message($"[Strata] Active gas on {map}:\n{atmosphere.DebugSummary()}");
            }
        }

        [DebugAction(Cat, "Clear all gas", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearGas()
        {
            Find.CurrentMap?.GetComponent<AtmosphereMapComponent>()?.ClearAll();
        }

        [DebugAction(Cat, "Saturate room with smoke", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        private static void SaturateSmoke()
        {
            Find.CurrentMap?.GetComponent<AtmosphereMapComponent>()?.DebugSaturate(UI.MouseCell());
        }

        [DebugAction(Cat, "Saturate room with deep gas", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        private static void SaturateDeepGas()
        {
            Find.CurrentMap?.GetComponent<AtmosphereMapComponent>()
                ?.DebugSaturate(UI.MouseCell(), StrataGasDefOf.Strata_DeepGas);
        }

        [DebugAction(Cat, "List hidden chambers", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ListHiddenChambers()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }
            var sb = new System.Text.StringBuilder($"[Strata] Hidden features on {map}:\n");
            int found = 0;
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing.def == ThingDefOf.SteamGeyser || thing.def == StrataThingDefOf.Strata_DeepGasVent)
                {
                    found++;
                    sb.AppendLine($"  {thing.LabelCap} @ {thing.Position}"
                        + (thing.Position.Fogged(map) ? " (still hidden in the rock)" : " (discovered)"));
                }
            }
            sb.AppendLine(found == 0 ? "  (none on this level)" : $"Total: {found}.");
            Log.Message(sb.ToString());
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
                Check($"atmosphere component on {map}", map.GetComponent<AtmosphereMapComponent>() != null);
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

            // Pillar 1: the living deep.
            Check("gas defs loaded", StrataGasDefOf.Strata_Smoke != null && StrataGasDefOf.Strata_DeepGas != null);
            Check("smoke rises, deep gas pools",
                StrataGasDefOf.Strata_Smoke.buoyant && !StrataGasDefOf.Strata_DeepGas.buoyant);
            Check("deep gas is persistent, flammable, extractable",
                StrataGasDefOf.Strata_DeepGas.passiveLeak <= 0f
                && StrataGasDefOf.Strata_DeepGas.flammable
                && StrataGasDefOf.Strata_DeepGas.extractable);
            Check("gas defs carry harm hediffs",
                StrataGasDefOf.Strata_Smoke.harmHediff != null && StrataGasDefOf.Strata_DeepGas.harmHediff != null);
            Check("gas economy defs loaded",
                StrataThingDefOf.Strata_DeepGasVent != null
                && StrataThingDefOf.Strata_GasWell != null
                && StrataThingDefOf.Strata_DeepGasCanister != null);
            Check("hidden chamber gensteps registered",
                DefDatabase<GenStepDef>.GetNamedSilentFail("Strata_HiddenChambers") != null
                && DefDatabase<GenStepDef>.GetNamedSilentFail("Strata_Fog") != null);
            sb.AppendLine($"  INFO  gas pipe adapter: {GasNetAdapter.Status}");

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

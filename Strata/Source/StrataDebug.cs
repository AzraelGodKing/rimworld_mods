using System.Collections.Generic;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Dev-mode helpers (visible only with Development Mode on) for exercising
    // Strata's systems without waiting on the storyteller.
    public static class StrataDebug
    {
        private const string Cat = "Strata";
        private const string IncidentsCat = "Strata/Incidents";
        private const string GasCat = "Strata/Gas";

        // Cross-map click relink: pick shaft, switch floors, click landing.
        private static MapPortal pendingRelinkShaft;

        private static IIncidentTarget ResolveIncidentTarget(IncidentDef def)
        {
            if (def?.targetTags != null)
            {
                foreach (IncidentTargetTagDef tag in def.targetTags)
                {
                    if (tag == IncidentTargetTagDefOf.World)
                    {
                        return Find.World;
                    }
                }
            }
            return Find.CurrentMap;
        }

        private static void Fire(IncidentDef def)
        {
            if (def == null)
            {
                Messages.Message("[Strata] Incident def not loaded.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            IIncidentTarget target = ResolveIncidentTarget(def);
            if (target == null)
            {
                Messages.Message("[Strata] No valid target for this incident (open a colony map or world view).",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, target);
            // Dev buttons bypass the storyteller's pacing gates (ThreatBig
            // mercy windows, refire cooldowns, difficulty settings).
            parms.forced = true;
            if (def.Worker.TryExecute(parms))
            {
                return;
            }
            // Settings gates and refire cooldowns can still block TryExecute; in
            // dev mode fall back to the worker body so every Strata incident is
            // exercisable from the menu.
            if (Prefs.DevMode && DevTryExecuteWorker(def.Worker, parms))
            {
                return;
            }
            Messages.Message($"[Strata] {def.defName} can't fire right now — check map type, research, factions, and mod settings.",
                MessageTypeDefOf.RejectInput, historical: false);
        }

        private static bool DevTryExecuteWorker(IncidentWorker worker, IncidentParms parms)
        {
            try
            {
                return (bool)AccessTools.Method(worker.GetType(), "TryExecuteWorker")
                    .Invoke(worker, new object[] { parms });
            }
            catch
            {
                return false;
            }
        }

        [DebugAction(IncidentsCat, "Cave-in", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireCaveIn() => Fire(StrataIncidentDefOf.Strata_CaveIn);

        [DebugAction(IncidentsCat, "Gas pocket", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireGasPocket() => Fire(StrataIncidentDefOf.Strata_GasPocket);

        [DebugAction(IncidentsCat, "Deep vein", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireDeepVein() => Fire(StrataIncidentDefOf.Strata_DeepVein);

        [DebugAction(IncidentsCat, "Deep raid", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireDeepRaid() => Fire(StrataIncidentDefOf.Strata_DeepRaid);

        [DebugAction(IncidentsCat, "Prospector tip", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireProspector() => Fire(StrataIncidentDefOf.Strata_ProspectorTip);

        [DebugAction(IncidentsCat, "Ground tremor", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireTremor() => Fire(StrataIncidentDefOf.Strata_Tremor);

        [DebugAction(IncidentsCat, "Gas firestorm", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireGasFirestorm() => Fire(StrataIncidentDefOf.Strata_GasFirestorm);

        [DebugAction(IncidentsCat, "Deep siege", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireDeepSiege() => Fire(StrataIncidentDefOf.Strata_DeepSiege);

        [DebugAction(IncidentsCat, "Cave breakthrough", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireCaveBreakthrough() => Fire(StrataIncidentDefOf.Strata_CaveBreakthrough);

        [DebugAction(IncidentsCat, "Prospector dig", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireProspectorDig() => Fire(StrataIncidentDefOf.Strata_ProspectorDig);

        [DebugAction(IncidentsCat, "Lost miners", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireLostMiners() => Fire(StrataIncidentDefOf.Strata_LostMiners);

        [DebugAction(IncidentsCat, "Flood seep", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireFloodSeep() => Fire(StrataIncidentDefOf.Strata_FloodSeep);

        [DebugAction(IncidentsCat, "Lost miners Anomaly", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireLostMinersAnomaly() =>
            Fire(DefDatabase<IncidentDef>.GetNamedSilentFail("Strata_LostMinersAnomaly"));

        [DebugAction(IncidentsCat, "Sunken ruin", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireSunkenRuin() => Fire(StrataIncidentDefOf.Strata_SunkenRuinDiscovered);

        [DebugAction(IncidentsCat, "Collapsed mine", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireCollapsedMine() => Fire(StrataIncidentDefOf.Strata_CollapsedMineDiscovered);

        [DebugAction(IncidentsCat, "Sealed vault", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireSealedVault() => Fire(StrataIncidentDefOf.Strata_SealedVaultDiscovered);

        [DebugAction(IncidentsCat, "Geothermal vent", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireGeothermalVent() => Fire(StrataIncidentDefOf.Strata_GeothermalVentDiscovered);

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

        [DebugAction(GasCat, "Log room gases", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogRoomGases()
        {
            Map map = Find.CurrentMap;
            AtmosphereMapComponent atmosphere = map?.GetComponent<AtmosphereMapComponent>();
            if (atmosphere != null)
            {
                Log.Message($"[Strata] Active gas on {map}:\n{atmosphere.DebugSummary()}");
            }
        }

        [DebugAction(GasCat, "Clear all gas", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearGas()
        {
            Find.CurrentMap?.GetComponent<AtmosphereMapComponent>()?.ClearAll();
        }

        [DebugAction(GasCat, "Saturate room with smoke", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        private static void SaturateSmoke()
        {
            Find.CurrentMap?.GetComponent<AtmosphereMapComponent>()?.DebugSaturate(UI.MouseCell());
        }

        [DebugAction(GasCat, "Saturate room with deep gas", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        private static void SaturateDeepGas()
        {
            Find.CurrentMap?.GetComponent<AtmosphereMapComponent>()
                ?.DebugSaturate(UI.MouseCell(), StrataGasDefOf.Strata_DeepGas);
        }

        [DebugAction(GasCat, "Saturate room with oxygen", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        private static void SaturateOxygen()
        {
            Find.CurrentMap?.GetComponent<AtmosphereMapComponent>()
                ?.DebugSaturate(UI.MouseCell(), StrataGasDefOf.Strata_Oxygen, AtmosphereMapComponent.AmbientOxygen);
        }

        [DebugAction(GasCat, "Deplete oxygen in room", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        private static void DepleteOxygen()
        {
            Find.CurrentMap?.GetComponent<AtmosphereMapComponent>()
                ?.DebugSetGas(UI.MouseCell(), StrataGasDefOf.Strata_Oxygen, 0f);
        }

        [DebugAction(GasCat, "Saturate room with CO2", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        private static void SaturateCO2()
        {
            Find.CurrentMap?.GetComponent<AtmosphereMapComponent>()
                ?.DebugSaturate(UI.MouseCell(), StrataGasDefOf.Strata_CarbonDioxide, 0.5f);
        }

        [DebugAction(Cat, "Place rich ore nodes", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void PlaceRichOreNodes()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }
            new GenStep_RichOreNodes().Generate(map, default);
            Messages.Message("Strata: ran rich ore node placement on this map.", MessageTypeDefOf.TaskCompletion, false);
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
                Check($"relay tick component on {map}", map.GetComponent<MapComponent_StrataRelayTick>() != null);
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
            Check("world caravan-travel component exists", StrataCaravanTravel.Get != null);
            Check("settings loaded", StrataMod.Settings != null);

            // Pillar 1: the living deep.
            Check("gas defs loaded",
                StrataGasDefOf.Strata_Smoke != null && StrataGasDefOf.Strata_DeepGas != null
                && StrataGasDefOf.Strata_Oxygen != null && StrataGasDefOf.Strata_CarbonDioxide != null
                && StrataGasDefOf.Strata_Methane != null && StrataGasDefOf.Strata_BlackDamp != null
                && StrataGasDefOf.Strata_Spores != null && StrataGasDefOf.Strata_Steam != null);
            Check("smoke rises, deep gas pools",
                StrataGasDefOf.Strata_Smoke.buoyant && !StrataGasDefOf.Strata_DeepGas.buoyant);
            Check("O₂ rises, CO₂ sinks",
                StrataGasDefOf.Strata_Oxygen.buoyant && !StrataGasDefOf.Strata_CarbonDioxide.buoyant);
            Check("methane rises, black damp pools",
                StrataGasDefOf.Strata_Methane.buoyant && !StrataGasDefOf.Strata_BlackDamp.buoyant
                && StrataGasDefOf.Strata_Steam.buoyant && !StrataGasDefOf.Strata_Spores.buoyant);
            Check("black damp displaces oxygen",
                StrataGasDefOf.Strata_BlackDamp.displacesOxygen > 0f);
            Check("O₂ hypoxia and CO₂ harm defs",
                StrataGasDefOf.Strata_Oxygen.harmWhenBelow
                && StrataGasDefOf.Strata_Oxygen.harmHediff != null
                && StrataGasDefOf.Strata_CarbonDioxide.harmHediff != null);
            Check("life support defs loaded",
                StrataThingDefOf.Strata_OxygenPump != null
                && StrataThingDefOf.Strata_CO2Pump != null
                && StrataThingDefOf.Strata_GasExchanger != null);
            Check("ancient colony stairwell defs",
                StrataThingDefOf.Strata_AncientColonyStairsDown != null
                && StrataThingDefOf.Strata_AncientColonyStairsUp != null
                && DefDatabase<GenStepDef>.GetNamedSilentFail("Strata_AncientColonyStairwell") != null);
            Check("mine atmosphere counters loaded",
                StrataThingDefOf.Strata_MethaneFlare != null
                && StrataThingDefOf.Strata_BlackDampScrubber != null
                && StrataThingDefOf.Strata_SporeFilter != null
                && StrataThingDefOf.Strata_SteamCondenser != null);
            Check("canary cage loaded", StrataThingDefOf.Strata_CanaryCage != null);
            Check("bird cage loaded", StrataThingDefOf.Strata_BirdCage != null);
            Check("mine canary loaded", StrataPawnKindDefOf.Strata_Canary != null);
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
            Check("rich ore node genstep registered",
                DefDatabase<GenStepDef>.GetNamedSilentFail("Strata_RichOreNodes") != null);
            Check("building-up tower stairwell loaded",
                StrataThingDefOf.Strata_StairsBuildUp != null
                && StrataThingDefOf.Strata_BuildUpLanding != null
                && DefDatabase<MapGeneratorDef>.GetNamedSilentFail("Strata_UpperLevel") != null
                && DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Strata_BuildingUp") != null);
            Check("tower elevator loaded",
                StrataThingDefOf.Strata_ElevatorBuildUp != null
                && StrataThingDefOf.Strata_ElevatorBuildUpLanding != null);
            Check("upper roof-deck terrains loaded",
                DefDatabase<TerrainDef>.GetNamedSilentFail(UpperDeckUtility.RoofDeckDefName) != null
                && DefDatabase<TerrainDef>.GetNamedSilentFail(UpperDeckUtility.OpenSkyDefName) != null);
            Check("gravship APIs present for stack follow",
                typeof(GravshipUtility) != null && typeof(Building_GravEngine) != null);
            if (StrataGravshipUtility.OdysseyActive)
            {
                Check("gravship stairs loaded (Odyssey)",
                    StrataThingDefOf.Strata_GravshipStairsDown != null
                    && StrataThingDefOf.Strata_GravshipStairsUp != null
                    && StrataThingDefOf.Strata_GravshipStairsBuildUp != null
                    && StrataThingDefOf.Strata_GravshipBuildUpLanding != null);
                Check("gravship elevators loaded (Odyssey)",
                    StrataThingDefOf.Strata_GravshipElevatorDown != null
                    && StrataThingDefOf.Strata_GravshipElevatorUp != null
                    && StrataThingDefOf.Strata_GravshipElevatorBuildUp != null
                    && StrataThingDefOf.Strata_GravshipElevatorBuildUpLanding != null);
            }
            Check("sunken ruin site loaded", SunkenRuinDefOf.Strata_SunkenRuin != null);
            Check("sunken ruin map generator loaded",
                DefDatabase<MapGeneratorDef>.GetNamedSilentFail("Strata_SunkenRuinLevel") != null);
            Check("collapsed mine quest site loaded", QuestSiteDefOf.Strata_CollapsedMine != null);
            Check("sealed vault quest site loaded", QuestSiteDefOf.Strata_SealedVault != null);
            Check("geothermal vent quest site loaded", QuestSiteDefOf.Strata_GeothermalVent != null);
            Check("quest site stairheads loaded",
                QuestSiteDefOf.Strata_MineStairsDown != null
                && QuestSiteDefOf.Strata_VaultStairsDown != null
                && QuestSiteDefOf.Strata_VentStairsDown != null);
            Check("quest site map generators loaded",
                DefDatabase<MapGeneratorDef>.GetNamedSilentFail("Strata_CollapsedMineLevel") != null
                && DefDatabase<MapGeneratorDef>.GetNamedSilentFail("Strata_SealedVaultLevel") != null
                && DefDatabase<MapGeneratorDef>.GetNamedSilentFail("Strata_GeothermalVentLevel") != null);
            Check("warren theme gensteps registered",
                DefDatabase<GenStepDef>.GetNamedSilentFail("Strata_WarrenTheme_Fungal") != null
                && DefDatabase<GenStepDef>.GetNamedSilentFail("Strata_WarrenTheme_Frozen") != null
                && DefDatabase<GenStepDef>.GetNamedSilentFail("Strata_WarrenTheme_Volcanic") != null);
            Check("rimefeller shaft junctions loaded",
                DefDatabase<ThingDef>.GetNamedSilentFail("Strata_ShaftFluid_RimefellerCrude") != null
                && DefDatabase<ThingDef>.GetNamedSilentFail("Strata_ShaftFluid_RimefellerFuel") != null);
            sb.AppendLine($"  INFO  gas pipe adapter: {GasNetAdapter.Status}");

            sb.AppendLine($"Total: {passed} passed, {failed} failed.");
            if (failed > 0) { Log.Warning(sb.ToString()); } else { Log.Message(sb.ToString()); }
        }

        [DebugAction(Cat, "Relink stairs by pair ID", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RelinkStairsByPairId()
        {
            StrataPortalUtility.RepairMissingLandings();
            int stamped = StrataPortalUtility.StampLinkedPortalPairIds();
            int relinked = StrataPortalUtility.RelinkPortalsByPairId();
            Messages.Message(
                "[Strata] Pair-ID relink: stamped " + stamped + ", rewired " + relinked + ".",
                MessageTypeDefOf.TaskCompletion, historical: false);
        }

        [DebugAction(Cat, "Relink stairs (click shaft, then landing)",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        private static void RelinkStairsClickTool()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }
            IntVec3 cell = UI.MouseCell();
            if (!cell.InBounds(map))
            {
                return;
            }

            MapPortal hit = FindPortalAt(map, cell);
            if (pendingRelinkShaft == null)
            {
                if (hit != null && StrataPortalUtility.IsStrataPortalShaft(hit))
                {
                    pendingRelinkShaft = hit;
                    Messages.Message(
                        "[Strata] Relink: shaft selected ("
                        + hit.LabelCap + ", pair "
                        + (CompStrataShaftLink.CompOf(hit)?.ShortId ?? "?")
                        + "). Switch floors if needed, then click its landing.",
                        MessageTypeDefOf.NeutralEvent, historical: false);
                    return;
                }
                Messages.Message("[Strata] Relink: click a Strata shaft first.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (hit != null && StrataPortalUtility.IsStrataPortalLanding(hit))
            {
                if (StrataPortalUtility.ConnectPortalPair(pendingRelinkShaft, hit))
                {
                    Messages.Message(
                        "[Strata] Relinked " + pendingRelinkShaft.LabelCap
                        + " <-> " + hit.LabelCap + ".",
                        MessageTypeDefOf.TaskCompletion, historical: false);
                }
                pendingRelinkShaft = null;
                return;
            }

            if (hit != null && StrataPortalUtility.IsStrataPortalShaft(hit))
            {
                pendingRelinkShaft = hit;
                Messages.Message(
                    "[Strata] Relink: shaft changed to " + hit.LabelCap + ".",
                    MessageTypeDefOf.NeutralEvent, historical: false);
                return;
            }

            Messages.Message("[Strata] Relink: click a landing (or another shaft).",
                MessageTypeDefOf.RejectInput, historical: false);
        }

        [DebugAction(Cat, "Relink selected shaft ↔ landing",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RelinkSelectedShaftLanding()
        {
            MapPortal shaft = null;
            MapPortal landing = null;
            foreach (object obj in Find.Selector.SelectedObjectsListForReading)
            {
                if (obj is not Thing thing)
                {
                    continue;
                }
                if (shaft == null && StrataPortalUtility.IsStrataPortalShaft(thing))
                {
                    shaft = (MapPortal)thing;
                }
                else if (landing == null && StrataPortalUtility.IsStrataPortalLanding(thing))
                {
                    landing = (MapPortal)thing;
                }
            }

            if (shaft == null || landing == null)
            {
                Messages.Message(
                    "[Strata] Select one Strata shaft and one landing (use multi-select / shift).",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (StrataPortalUtility.ConnectPortalPair(shaft, landing))
            {
                Messages.Message(
                    "[Strata] Relinked " + shaft.LabelCap + " <-> " + landing.LabelCap + ".",
                    MessageTypeDefOf.TaskCompletion, historical: false);
            }
        }

        [DebugAction(Cat, "Cancel stair relink", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CancelStairRelink()
        {
            if (pendingRelinkShaft == null)
            {
                Messages.Message("[Strata] No stair relink in progress.",
                    MessageTypeDefOf.NeutralEvent, historical: false);
                return;
            }
            pendingRelinkShaft = null;
            Messages.Message("[Strata] Stair relink cancelled.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Repair missing landings", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RepairMissingLandingsDev()
        {
            StrataPortalUtility.RepairMissingLandings();
            Messages.Message("[Strata] Repair missing landings finished (see log).",
                MessageTypeDefOf.TaskCompletion, historical: false);
        }

        [DebugAction(Cat, "Log portal topology (include pair id)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogPortalTopology()
        {
            Log.Message(StrataPortalUtility.DebugPortalTopology());
        }

        private static MapPortal FindPortalAt(Map map, IntVec3 cell)
        {
            List<Thing> things = cell.GetThingList(map);
            MapPortal landing = null;
            MapPortal shaft = null;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (StrataPortalUtility.IsStrataPortalLanding(thing))
                {
                    landing = (MapPortal)thing;
                }
                else if (StrataPortalUtility.IsStrataPortalShaft(thing))
                {
                    shaft = (MapPortal)thing;
                }
            }
            // Prefer landing when finishing a click-relink.
            return landing ?? shaft;
        }

        [DebugAction(Cat, "Force hibernate all empty levels", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceHibernateEmptyLevels()
        {
            StrataLevelPerfUtility.ForceHibernateAllEmptyLevels();
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

        [DebugAction(Cat, "Teleport selected to surface", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TeleportSelectedToSurface()
        {
            List<Pawn> pawns = SelectedPawns();
            if (pawns.Count == 0)
            {
                Messages.Message("[Strata] Select one or more pawns first.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            Map from = Find.CurrentMap ?? pawns[0].MapHeld;
            Map surface = ColonyBedUtility.FindSurfacePlayerHome(from);
            if (surface == null)
            {
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    if (StrataMapUtility.IsSurfacePlayerHome(maps[i]))
                    {
                        surface = maps[i];
                        break;
                    }
                }
            }
            if (surface == null)
            {
                Messages.Message("[Strata] No surface player-home map found.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            IntVec3 dest = FindSurfaceTeleportCell(surface, from);
            int moved = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (TeleportPawnTo(pawns[i], surface, dest))
                {
                    moved++;
                }
            }

            CameraJumper.TryJump(dest, surface);
            Messages.Message($"[Strata] Teleported {moved} pawn(s) to surface near {dest}.",
                MessageTypeDefOf.TaskCompletion, historical: false);
        }

        [DebugAction(Cat, "Teleport selected to current map center", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TeleportSelectedToCurrentMap()
        {
            Map map = Find.CurrentMap;
            List<Pawn> pawns = SelectedPawns();
            if (map == null || pawns.Count == 0)
            {
                Messages.Message("[Strata] Open a map and select one or more pawns.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            IntVec3 dest = CellFinder.RandomClosewalkCellNear(map.Center, map, 12);
            if (!dest.IsValid)
            {
                dest = map.Center;
            }
            int moved = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (TeleportPawnTo(pawns[i], map, dest))
                {
                    moved++;
                }
            }
            Messages.Message($"[Strata] Teleported {moved} pawn(s) to {map}.",
                MessageTypeDefOf.TaskCompletion, historical: false);
        }

        private static List<Pawn> SelectedPawns()
        {
            var result = new List<Pawn>();
            foreach (object obj in Find.Selector.SelectedObjectsListForReading)
            {
                if (obj is Pawn pawn && !pawn.Destroyed)
                {
                    result.Add(pawn);
                }
            }
            return result;
        }

        private static IntVec3 FindSurfaceTeleportCell(Map surface, Map from)
        {
            // Prefer a surface stair that links back toward the pawn's floor.
            if (from != null && from != surface)
            {
                MapPortal best = LevelGraph.BestFirstStep(surface, from, surface.Center);
                if (best != null && best.Spawned)
                {
                    IntVec3 nearStair = CellFinder.RandomClosewalkCellNear(best.Position, surface, 6);
                    if (nearStair.IsValid)
                    {
                        return nearStair;
                    }
                    return best.Position;
                }
            }

            foreach (Thing thing in surface.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is MapPortal portal && portal.Spawned
                    && LevelGraph.OtherMapSafe(portal) != null)
                {
                    IntVec3 near = CellFinder.RandomClosewalkCellNear(portal.Position, surface, 6);
                    if (near.IsValid)
                    {
                        return near;
                    }
                }
            }

            IntVec3 fallback = CellFinder.RandomClosewalkCellNear(surface.Center, surface, 12);
            return fallback.IsValid ? fallback : surface.Center;
        }

        private static bool TeleportPawnTo(Pawn pawn, Map map, IntVec3 cell)
        {
            if (pawn == null || map == null || !cell.IsValid)
            {
                return false;
            }

            IntVec3 stand = cell;
            if (!stand.Standable(map) || stand.GetFirstPawn(map) != null)
            {
                stand = CellFinder.RandomClosewalkCellNear(cell, map, 8);
            }
            if (!stand.IsValid)
            {
                stand = cell;
            }

            if (pawn.Spawned)
            {
                if (pawn.Map == map)
                {
                    pawn.Position = stand;
                    pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true);
                    return true;
                }
                pawn.DeSpawn(DestroyMode.Vanish);
            }
            else if (pawn.holdingOwner != null)
            {
                pawn.holdingOwner.Remove(pawn);
            }

            GenSpawn.Spawn(pawn, stand, map, WipeMode.Vanish);
            pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true);
            return true;
        }
    }
}

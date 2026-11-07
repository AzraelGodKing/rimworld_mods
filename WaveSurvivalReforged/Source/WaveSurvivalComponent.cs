using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    // The wave timer, daily supplies and the run's saved state. Waves are in
    // WaveSurvivalComponent.Waves.cs and supplies in WaveSurvivalComponent.Supplies.cs.
    public partial class WaveSurvivalComponent : GameComponent
    {
        private const int CheckInterval = 250;
        private const int BeaconRescanTicks = GenDate.TicksPerHour;
        // 1: Wave Survival Reforged 1.0. 2: 2.0 (run settings, pre-rolled wave, wave tracker).
        private const int CurrentSaveVersion = 2;

        private int saveVersion = CurrentSaveVersion;
        private bool started;
        // The tuning this run plays with, copied from the mod settings at its start.
        private RunSettings run;
        private int waveNum = 1;
        private int ticksNextWave = -1;
        private int ticksNextDay = -1;
        private bool dropPending;
        // The next wave, rolled when it is scheduled so the alert can name it.
        private WavePlan nextPlan;
        private WaveTracker tracker = new WaveTracker();

        // Not saved: only stops the postponed message repeating for the same wave.
        private int postponedWave;

        // Not saved: the beacon found by the last scan, and what the scan saw.
        private Building beacon;
        private int beaconScanTick = -BeaconRescanTicks;
        private int beaconScanSignature = -1;

        public WaveSurvivalComponent(Game game)
        {
        }

        public static WaveSurvivalComponent Instance => Current.Game?.GetComponent<WaveSurvivalComponent>();

        public static bool ModeActive => Find.Storyteller?.def == WSR_DefOf.WSR_WaveSurvival;

        public bool Started => started;

        public int WaveNum => waveNum;

        public int TicksUntilNextWave => ticksNextWave - Find.TickManager.TicksGame;

        public RunSettings Run => run;

        public WavePlan NextPlan => nextPlan;

        public WavePool NextWavePool => nextPlan?.type?.pool ?? PoolFor(waveNum);

        public WaveTracker Tracker => tracker;

        private static WaveSurvivalConfigDef Config => WSR_DefOf.WSR_Config;

        private static float ThreatScale => Find.Storyteller.difficulty.threatScale;

        public override void GameComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now % CheckInterval != 0 || !ModeActive)
                return;
            if (!started)
                Start(now);
            tracker.Update(now);
            if (now >= ticksNextWave)
            {
                DeepProfiler.Start("WaveSurvivalReforged.RunWave");
                try
                {
                    RunWave(now);
                }
                finally
                {
                    DeepProfiler.End();
                }
            }
            else if (nextPlan == null)
            {
                RollNextPlan();
            }
            DeepProfiler.Start("WaveSurvivalReforged.Supplies");
            try
            {
                if (now >= ticksNextDay)
                    RunDay(now);
                else if (dropPending && TryDropSupplies(fallback: false))
                    dropPending = false;
            }
            finally
            {
                DeepProfiler.End();
            }
        }

        public override void GameComponentOnGUI()
        {
            WaveHud.Draw(this);
        }

        private void Start(int now)
        {
            WaveSurvivalConfigDef cfg = Config;
            run = WaveSurvivalMod.Settings.ForNewRun();
            int hourOffset = cfg.raidHourOffset * GenDate.TicksPerHour;
            ticksNextWave = now + Mathf.RoundToInt(run.daysBeforeFirstWave * GenDate.TicksPerDay) + hourOffset;
            ticksNextDay = now + GenDate.TicksPerHour;
            waveNum = 1;
            started = true;
            RollNextPlan();
        }

        // The home map with a trade beacon, else any home map.
        public Map TargetMap()
        {
            Building found = Beacon();
            if (found != null)
                return found.Map;
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                    return map;
            }
            return null;
        }

        public bool HasBeacon => Beacon() != null;

        // A spawned colony trade beacon on a home map. Rescans only when the colony's building
        // count changes, the cached beacon is gone, or an hour has passed.
        public Building Beacon()
        {
            int now = Find.TickManager.TicksGame;
            int signature = BuildingSignature();
            bool cachedOk = beacon != null && beacon.Spawned && beacon.Map.IsPlayerHome
                && beacon.Faction == Faction.OfPlayer;
            if (cachedOk && signature == beaconScanSignature)
                return beacon;
            if (!cachedOk && signature == beaconScanSignature && now - beaconScanTick < BeaconRescanTicks)
                return null;
            beaconScanTick = now;
            beaconScanSignature = signature;
            beacon = null;
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome)
                    continue;
                foreach (Building_OrbitalTradeBeacon b in map.listerBuildings.AllBuildingsColonistOfClass<Building_OrbitalTradeBeacon>())
                {
                    if (b.Spawned)
                    {
                        beacon = b;
                        return beacon;
                    }
                }
            }
            return null;
        }

        private static int BuildingSignature()
        {
            int signature = 17;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
                signature = signature * 31 + maps[i].uniqueID * 7 + maps[i].listerBuildings.allBuildingsColonist.Count;
            return signature;
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
                saveVersion = CurrentSaveVersion;
            // A save without this field is from 1.0.
            Scribe_Values.Look(ref saveVersion, "saveVersion", 1);
            Scribe_Values.Look(ref started, "started");
            Scribe_Values.Look(ref waveNum, "waveNum", 1);
            Scribe_Values.Look(ref ticksNextWave, "ticksNextWave", -1);
            Scribe_Values.Look(ref ticksNextDay, "ticksNextDay", -1);
            Scribe_Values.Look(ref dropPending, "dropPending");
            Scribe_Values.Look(ref missedDropDays, "missedDropDays");
            Scribe_Deep.Look(ref nextPlan, "nextPlan");
            Scribe_Deep.Look(ref tracker, "tracker");
            Scribe_Deep.Look(ref run, "run");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                tracker ??= new WaveTracker();
                // A 1.0 run (or one started before presets) plays on as Standard, which is the
                // XML config it already used. Its wave number and countdown are kept, and the
                // next wave is pre-rolled on the first check without moving the countdown.
                if (started && run == null)
                    run = RunSettings.For(WavePreset.Standard);
                if (nextPlan != null && !nextPlan.IsUsable)
                    nextPlan = null;
            }
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WaveSurvivalReforged
{
    // What this run has done so far. Saved with the game.
    public class RunStats : IExposable
    {
        public int startTick;
        public int highestWave;
        public int wavesCleared;
        public List<int> killsPerWave = new List<int>();
        public int dropsReceived;
        // statsRecord.colonistsKilled when the run started; the difference is colonists lost.
        public int colonistsKilledAtStart;
        // Set once the end of the run has been recorded, so it is never recorded twice.
        public bool recorded;

        public static RunStats StartingNow()
        {
            return new RunStats
            {
                startTick = Find.TickManager.TicksGame,
                colonistsKilledAtStart = Find.StoryWatcher.statsRecord.colonistsKilled,
            };
        }

        // A run from a save made before run stats: what can be told from its wave number.
        public static RunStats FromOldSave(int waveNum)
        {
            return new RunStats
            {
                highestWave = waveNum - 1,
                wavesCleared = waveNum - 1,
            };
        }

        public int TotalKills
        {
            get
            {
                int total = 0;
                foreach (int k in killsPerWave)
                    total += k;
                return total;
            }
        }

        public int ColonistsLost => Find.StoryWatcher.statsRecord.colonistsKilled - colonistsKilledAtStart;

        public int TicksSurvived => Find.TickManager.TicksGame - startTick;

        public void Notify_WaveStarted(int wave)
        {
            if (wave > highestWave)
                highestWave = wave;
        }

        public void Notify_WaveFinished(int wave, int killed, bool cleared)
        {
            while (killsPerWave.Count < wave)
                killsPerWave.Add(0);
            killsPerWave[wave - 1] = killed;
            if (cleared)
                wavesCleared++;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Values.Look(ref highestWave, "highestWave");
            Scribe_Values.Look(ref wavesCleared, "wavesCleared");
            Scribe_Collections.Look(ref killsPerWave, "killsPerWave", LookMode.Value);
            Scribe_Values.Look(ref dropsReceived, "dropsReceived");
            Scribe_Values.Look(ref colonistsKilledAtStart, "colonistsKilledAtStart");
            Scribe_Values.Look(ref recorded, "recorded");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                killsPerWave ??= new List<int>();
        }
    }

    // One finished run on the local best-runs list. Client-side only, kept in the mod settings.
    public class RunRecord : IExposable
    {
        public int waves;
        public int kills;
        public int colonistsLost;
        public float days;
        public WavePreset preset;
        public string difficulty;
        public string colony;
        public string date;

        public void ExposeData()
        {
            Scribe_Values.Look(ref waves, "waves");
            Scribe_Values.Look(ref kills, "kills");
            Scribe_Values.Look(ref colonistsLost, "colonistsLost");
            Scribe_Values.Look(ref days, "days");
            Scribe_Values.Look(ref preset, "preset");
            Scribe_Values.Look(ref difficulty, "difficulty");
            Scribe_Values.Look(ref colony, "colony");
            Scribe_Values.Look(ref date, "date");
        }
    }
}

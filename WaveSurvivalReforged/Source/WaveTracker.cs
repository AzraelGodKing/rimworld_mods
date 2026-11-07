using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WaveSurvivalReforged
{
    /// <summary>
    /// Follows the enemies of the latest wave: pawns that turned up hostile on the wave's map
    /// in the first hours after it started. When none is left standing, or the next wave
    /// starts, it sends a summary letter and keeps the result for the HUD.
    /// Only live pawns are held, so the save never references a discarded pawn.
    /// </summary>
    public class WaveTracker : IExposable
    {
        private const int GatherTicks = 2 * GenDate.TicksPerHour;

        private Map map;
        private int wave;
        private string label;
        private int gatherUntil = -1;
        private List<int> hostileBefore = new List<int>();
        private List<Pawn> pawns = new List<Pawn>();
        private int total;
        private int killed;
        private int fled;

        // The last finished wave, for the HUD. 0: none yet.
        private int lastWave;
        private int lastKilled;
        private int lastDowned;
        private int lastFled;
        private int lastLeft;

        // Not saved: fast lookup for hostileBefore and pawns while gathering.
        private HashSet<int> beforeSet;
        private HashSet<Pawn> pawnSet;

        public bool Active => wave > 0;

        public int LastWave => lastWave;

        public int LastKilled => lastKilled;

        public int LastDowned => lastDowned;

        public int LastFled => lastFled;

        public int LastLeft => lastLeft;

        public int ActiveWave => wave;

        public int Standing
        {
            get
            {
                int n = 0;
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (pawns[i] != null && !pawns[i].Downed)
                        n++;
                }
                return n;
            }
        }

        // Before the wave's incident runs: remember who was already hostile on the map.
        public void BeginGather(Map waveMap, int waveNum)
        {
            map = waveMap;
            wave = waveNum;
            label = null;
            gatherUntil = -1;
            pawns.Clear();
            total = killed = fled = 0;
            hostileBefore.Clear();
            foreach (Pawn p in waveMap.mapPawns.AllPawnsSpawned)
            {
                if (p.HostileTo(Faction.OfPlayer))
                    hostileBefore.Add(p.thingIDNumber);
            }
            beforeSet = new HashSet<int>(hostileBefore);
            pawnSet = new HashSet<Pawn>();
        }

        // The wave spawned: collect its pawns now and for the next two hours (drop pods, clusters).
        public void Started(WavePlan plan, int now)
        {
            label = plan.Describe();
            gatherUntil = now + GatherTicks;
            Gather();
        }

        // The wave failed to spawn.
        public void Cancel()
        {
            wave = 0;
            map = null;
            pawns.Clear();
            hostileBefore.Clear();
        }

        public void Update(int now, RunStats stats)
        {
            if (!Active)
                return;
            if (map == null || !Find.Maps.Contains(map))
            {
                Finish(null, stats);
                return;
            }
            if (now < gatherUntil)
                Gather();
            for (int i = pawns.Count - 1; i >= 0; i--)
            {
                Pawn p = pawns[i];
                if (p == null)
                {
                    pawns.RemoveAt(i);
                }
                else if (p.Dead)
                {
                    killed++;
                    Forget(i);
                }
                else if (!p.Spawned || p.Map != map || !p.HostileTo(Faction.OfPlayer))
                {
                    // Left the map, or captured, recruited or calmed down.
                    if (!p.Spawned || p.Map != map)
                        fled++;
                    Forget(i);
                }
            }
            if (now >= gatherUntil && Standing == 0)
                Finish(map, stats);
        }

        // Ends the wave being followed (if any) and sends its summary. Called when the next
        // wave starts with whatever is still standing.
        public void Finish(Map current, RunStats stats)
        {
            if (!Active)
                return;
            int downed = 0;
            int left = 0;
            foreach (Pawn p in pawns)
            {
                if (p == null)
                    continue;
                if (p.Downed)
                    downed++;
                else
                    left++;
            }
            lastWave = wave;
            lastKilled = killed;
            lastDowned = downed;
            lastFled = fled;
            lastLeft = left;
            stats?.Notify_WaveFinished(wave, killed, left == 0);
            if (total > 0 && label != null)
            {
                TaggedString text = "WSR_WaveSummaryText".Translate(wave, label, killed, downed, fled);
                if (left > 0)
                    text += "\n\n" + "WSR_WaveSummaryLeft".Translate(left);
                Find.LetterStack.ReceiveLetter("WSR_WaveSummaryLabel".Translate(wave), text,
                    left > 0 ? LetterDefOf.NeutralEvent : LetterDefOf.PositiveEvent,
                    current != null ? new LookTargets(current.Parent) : LookTargets.Invalid);
            }
            Cancel();
        }

        private void Gather()
        {
            beforeSet ??= new HashSet<int>(hostileBefore);
            pawnSet ??= new HashSet<Pawn>(pawns);
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (beforeSet.Contains(p.thingIDNumber) || pawnSet.Contains(p) || !p.HostileTo(Faction.OfPlayer))
                    continue;
                pawns.Add(p);
                pawnSet.Add(p);
                total++;
            }
        }

        private void Forget(int index)
        {
            pawnSet?.Remove(pawns[index]);
            pawns.RemoveAt(index);
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref map, "map");
            Scribe_Values.Look(ref wave, "wave");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref gatherUntil, "gatherUntil", -1);
            Scribe_Collections.Look(ref hostileBefore, "hostileBefore", LookMode.Value);
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_Values.Look(ref total, "total");
            Scribe_Values.Look(ref killed, "killed");
            Scribe_Values.Look(ref fled, "fled");
            Scribe_Values.Look(ref lastWave, "lastWave");
            Scribe_Values.Look(ref lastKilled, "lastKilled");
            Scribe_Values.Look(ref lastDowned, "lastDowned");
            Scribe_Values.Look(ref lastFled, "lastFled");
            Scribe_Values.Look(ref lastLeft, "lastLeft");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                hostileBefore ??= new List<int>();
                pawns ??= new List<Pawn>();
                pawns.RemoveAll(p => p == null);
                beforeSet = null;
                pawnSet = null;
            }
        }
    }
}

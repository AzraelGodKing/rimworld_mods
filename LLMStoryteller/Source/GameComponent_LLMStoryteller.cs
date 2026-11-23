using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace LLMStoryteller
{
    /// <summary>
    /// Per-save storyteller state: when each home map decides next, which maps are in a fallback
    /// window, and the model's last few choices (fed back to it as memory). The request in flight is
    /// not saved: after a load the next interval simply asks again.
    /// </summary>
    public class GameComponent_LLMStoryteller : GameComponent
    {
        const int MaxRememberedDecisions = 6;
        public const int MaxAttempts = 2;

        Dictionary<int, int> nextDecisionTick = new Dictionary<int, int>();
        Dictionary<int, int> fallbackUntilTick = new Dictionary<int, int>();
        List<string> recentDecisions = new List<string>();

        /// <summary>The one request in flight. Local servers run one prompt at a time, so maps take turns.</summary>
        public PendingDecision Pending;

        bool offlineAnnounced;

        public GameComponent_LLMStoryteller(Game game)
        {
        }

        public static GameComponent_LLMStoryteller Get => Current.Game?.GetComponent<GameComponent_LLMStoryteller>();

        public IList<string> RecentDecisions => recentDecisions;

        public bool DecisionDue(Map map) =>
            !nextDecisionTick.TryGetValue(map.uniqueID, out int t) || Find.TickManager.TicksGame >= t;

        /// <summary>True when the Cassandra comps should run for this target instead of the model.</summary>
        public bool FallbackActive(IIncidentTarget target)
        {
            if (!(target is Map map) || !map.IsPlayerHome) return true;
            if (!LLMStorytellerMod.Settings.useModel) return true;
            return fallbackUntilTick.TryGetValue(map.uniqueID, out int t) && Find.TickManager.TicksGame < t;
        }

        public void ScheduleNext(Map map)
        {
            nextDecisionTick[map.uniqueID] = Find.TickManager.TicksGame + IntervalTicks;
        }

        /// <summary>The model could not decide: Cassandra's comps cover this map until the next decision.</summary>
        public void EnterFallback(Map map, string why)
        {
            ScheduleNext(map);
            fallbackUntilTick[map.uniqueID] = nextDecisionTick[map.uniqueID];
            Log.Warning("[LLMStoryteller] Falling back to Cassandra until the next decision: " + why);
            if (!offlineAnnounced)
            {
                offlineAnnounced = true;
                Messages.Message("LLMStoryteller_Offline".Translate(why), MessageTypeDefOf.CautionInput, historical: false);
            }
        }

        public void RecordDecision(Map map, string summary)
        {
            ScheduleNext(map);
            fallbackUntilTick.Remove(map.uniqueID);
            offlineAnnounced = false;
            recentDecisions.Add("Day " + (GenDate.DaysPassed + 1) + ": " + summary);
            while (recentDecisions.Count > MaxRememberedDecisions) recentDecisions.RemoveAt(0);
        }

        static int IntervalTicks => (int)(LLMStorytellerMod.Settings.hoursBetweenDecisions * GenDate.TicksPerHour);

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving) PruneGoneMaps();
            Scribe_Collections.Look(ref nextDecisionTick, "nextDecisionTick", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref fallbackUntilTick, "fallbackUntilTick", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref recentDecisions, "recentDecisions", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (nextDecisionTick == null) nextDecisionTick = new Dictionary<int, int>();
                if (fallbackUntilTick == null) fallbackUntilTick = new Dictionary<int, int>();
                if (recentDecisions == null) recentDecisions = new List<string>();
            }
        }

        void PruneGoneMaps()
        {
            var live = new HashSet<int>(Find.Maps.Select(m => m.uniqueID));
            foreach (int id in nextDecisionTick.Keys.Where(k => !live.Contains(k)).ToList()) nextDecisionTick.Remove(id);
            foreach (int id in fallbackUntilTick.Keys.Where(k => !live.Contains(k)).ToList()) fallbackUntilTick.Remove(id);
        }
    }

    /// <summary>A request in flight, with what is needed to check and retry its answer.</summary>
    public sealed class PendingDecision
    {
        public int MapId;
        public List<MenuOption> Menu;
        public string System;
        public string User;
        public int Attempt;
        public Task<LlmResult> Task;
    }
}

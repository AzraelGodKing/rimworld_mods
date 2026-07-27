using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    public class GameComp_DeepColony : GameComponent
    {
        public static GameComp_DeepColony Instance =>
            Verse.Current.Game?.GetComponent<GameComp_DeepColony>();

        private HashSet<int> inheritanceProcessed = new HashSet<int>();
        private HashSet<int> formerPlayerColonists = new HashSet<int>();
        private Dictionary<int, float> factionDriftBuffer = new Dictionary<int, float>();
        private int driftTickCounter;

        private List<int> recentColonistDeathTimestamps = new List<int>();
        private bool massacreTriggeredThisWindow;

        private const int DriftIntervalTicks = 2500;
        private const int MassacreWindowTicks = 60000;
        private const int MassacreDeathThreshold = 3;

        public GameComp_DeepColony(Game game) { }

        public bool HasProcessedInheritance(Pawn pawn) =>
            inheritanceProcessed.Contains(pawn.thingIDNumber);

        public void MarkInheritanceProcessed(Pawn pawn) =>
            inheritanceProcessed.Add(pawn.thingIDNumber);

        public bool WasEverPlayerColonist(Pawn pawn) =>
            pawn != null && formerPlayerColonists.Contains(pawn.thingIDNumber);

        public void MarkFormerPlayerColonist(Pawn pawn)
        {
            if (pawn != null) formerPlayerColonists.Add(pawn.thingIDNumber);
        }

        public void AddFactionDrift(Faction faction, float amount)
        {
            if (faction == null || faction.IsPlayer) return;
            if (!factionDriftBuffer.ContainsKey(faction.loadID))
                factionDriftBuffer[faction.loadID] = 0f;
            factionDriftBuffer[faction.loadID] += amount;
        }

        public void NotifyColonistDied(Pawn victim)
        {
            if (victim == null || !victim.RaceProps.Humanlike) return;

            int now = Find.TickManager.TicksGame;
            recentColonistDeathTimestamps.Add(now);
            PruneDeathWindow(now);

            if (massacreTriggeredThisWindow) return;
            if (recentColonistDeathTimestamps.Count < MassacreDeathThreshold) return;

            massacreTriggeredThisWindow = true;
            TraumaDef massacre = DC_DefOf.DC_Trauma_Massacre;
            if (massacre == null) return;

            Map map = victim.MapHeld;
            if (map == null) return;

            foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Dead || colonist == victim) continue;
                TraumaUtility.ApplyTrauma(colonist, massacre);
            }
        }

        private void PruneDeathWindow(int now)
        {
            recentColonistDeathTimestamps.RemoveAll(t => now - t > MassacreWindowTicks);
            // Allow another massacre once the dense cluster has aged out of the window
            // (not only when the list is completely empty — trickle deaths used to latch forever).
            if (recentColonistDeathTimestamps.Count < MassacreDeathThreshold)
                massacreTriggeredThisWindow = false;
        }

        public override void GameComponentTick()
        {
            driftTickCounter++;
            if (driftTickCounter < DriftIntervalTicks) return;
            driftTickCounter = 0;

            ProcessFactionDrift();
            PruneDeathWindow(Find.TickManager.TicksGame);
        }

        private void ProcessFactionDrift()
        {
            var keys = new List<int>(factionDriftBuffer.Keys);
            foreach (int id in keys)
            {
                float amount = factionDriftBuffer[id];
                Faction faction = Find.FactionManager.AllFactionsListForReading
                    .Find(f => f.loadID == id);
                if (faction == null)
                {
                    factionDriftBuffer.Remove(id);
                    continue;
                }

                int whole = (int)amount; // truncates toward zero; keep fractional remainder
                if (whole != 0)
                {
                    faction.TryAffectGoodwillWith(Faction.OfPlayer, whole, canSendMessage: false);
                    amount -= whole;
                }

                if (System.Math.Abs(amount) < 0.01f)
                    factionDriftBuffer.Remove(id);
                else
                    factionDriftBuffer[id] = amount;
            }

            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (faction.IsPlayer || faction.defeated) continue;

                int goodwill = faction.GoodwillWith(Faction.OfPlayer);
                if (goodwill > 40)
                {
                    if (Rand.MTBEventOccurs(5f, 60000f, DriftIntervalTicks))
                    {
                        faction.TryAffectGoodwillWith(Faction.OfPlayer, -1,
                            canSendMessage: false, canSendHostilityLetter: false);
                    }
                }
                else if (goodwill < -40)
                {
                    if (Rand.MTBEventOccurs(4f, 60000f, DriftIntervalTicks))
                    {
                        faction.TryAffectGoodwillWith(Faction.OfPlayer, 1,
                            canSendMessage: false, canSendHostilityLetter: false);
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref inheritanceProcessed, "inheritanceProcessed", LookMode.Value);
            Scribe_Collections.Look(ref formerPlayerColonists, "formerPlayerColonists", LookMode.Value);
            Scribe_Collections.Look(ref factionDriftBuffer, "factionDriftBuffer",
                LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref driftTickCounter, "driftTickCounter", 0);
            Scribe_Collections.Look(ref recentColonistDeathTimestamps, "recentColonistDeaths",
                LookMode.Value);
            Scribe_Values.Look(ref massacreTriggeredThisWindow, "massacreTriggered", false);

            if (inheritanceProcessed == null) inheritanceProcessed = new HashSet<int>();
            if (formerPlayerColonists == null) formerPlayerColonists = new HashSet<int>();
            if (factionDriftBuffer == null) factionDriftBuffer = new Dictionary<int, float>();
            if (recentColonistDeathTimestamps == null)
                recentColonistDeathTimestamps = new List<int>();
        }

        public override void FinalizeInit()
        {
            if (inheritanceProcessed == null) inheritanceProcessed = new HashSet<int>();
            if (formerPlayerColonists == null) formerPlayerColonists = new HashSet<int>();
            if (factionDriftBuffer == null) factionDriftBuffer = new Dictionary<int, float>();
            if (recentColonistDeathTimestamps == null)
                recentColonistDeathTimestamps = new List<int>();
            ActiveMentoringSession.ResetSession();
        }
    }
}

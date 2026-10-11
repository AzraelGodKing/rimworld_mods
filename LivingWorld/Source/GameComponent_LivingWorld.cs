using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    public class GameComponent_LivingWorld : GameComponent
    {
        private const int ChronicleCapacity = 96;
        private const int EvictedCorrectionCapacity = 64;

        private List<WorldEvent> chronicle = new List<WorldEvent>();
        private List<WorldEvent> evictedPendingCorrections = new List<WorldEvent>();
        private List<SettlementMood> moods = new List<SettlementMood>();
        // Not scribed; rebuilt from moods on first lookup after load.
        private Dictionary<int, SettlementMood> moodIndex;
        private List<FactionPairState> pairs = new List<FactionPairState>();
        private List<PendingFallout> pendingFallout = new List<PendingFallout>();

        private int lettersThisQuadrum;
        private int morphsThisYear;
        private bool budgetsInitialized;
        private Quadrum lastQuadrum;
        private int lastYear = -1;
        private int pulseIndex;
        private string lastNewsVersion;
        private List<int> collapsedFactionIds = new List<int>();

        // AZR-328 — cache Get across Label/inspect hot paths for the current Game.
        private static Game cachedGame;
        private static GameComponent_LivingWorld cachedComp;

        public GameComponent_LivingWorld(Game game)
        {
        }

        public override void FinalizeInit()
        {
            UpdateNewsLetter.TrySend(ref lastNewsVersion);
        }

        public static GameComponent_LivingWorld Get
        {
            get
            {
                Game game = Current.Game;
                if (game == null)
                {
                    cachedGame = null;
                    cachedComp = null;
                    return null;
                }
                if (game != cachedGame)
                {
                    cachedGame = game;
                    cachedComp = game.GetComponent<GameComponent_LivingWorld>();
                }
                return cachedComp;
            }
        }

        public IReadOnlyList<WorldEvent> Chronicle => chronicle;

        /// <summary>Distorted events evicted from the chronicle before their correction was due.</summary>
        internal List<WorldEvent> EvictedPendingCorrections => evictedPendingCorrections;

        public IReadOnlyList<FactionPairState> Pairs => pairs;

        internal List<FactionPairState> PairsMutable => pairs;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref chronicle, "chronicle", LookMode.Deep);
            Scribe_Collections.Look(ref evictedPendingCorrections, "evictedPendingCorrections", LookMode.Deep);
            Scribe_Collections.Look(ref moods, "moods", LookMode.Deep);
            Scribe_Collections.Look(ref pairs, "pairs", LookMode.Deep);
            Scribe_Collections.Look(ref pendingFallout, "pendingFallout", LookMode.Deep);
            Scribe_Collections.Look(ref collapsedFactionIds, "collapsedFactionIds", LookMode.Value);
            Scribe_Values.Look(ref lettersThisQuadrum, "lettersThisQuadrum");
            Scribe_Values.Look(ref morphsThisYear, "morphsThisYear");
            Scribe_Values.Look(ref budgetsInitialized, "budgetsInitialized");
            Scribe_Values.Look(ref lastQuadrum, "lastQuadrum");
            Scribe_Values.Look(ref lastYear, "lastYear", -1);
            Scribe_Values.Look(ref pulseIndex, "pulseIndex");
            Scribe_Values.Look(ref lastNewsVersion, "lastNewsVersion");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                chronicle ??= new List<WorldEvent>();
                evictedPendingCorrections ??= new List<WorldEvent>();
                moods ??= new List<SettlementMood>();
                pairs ??= new List<FactionPairState>();
                pendingFallout ??= new List<PendingFallout>();
                collapsedFactionIds ??= new List<int>();
                moodIndex = null;
            }
        }

        public override void GameComponentTick()
        {
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (settings == null || !settings.enabled)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            LivingWorldDebugCommand.Drain();
            RefreshBudgets(now);
            LivingWorldWarSites.TickExpire();
            LivingWorldRumour.TickCorrections(this);

            int interval = settings.tickInterval <= 0 ? 10000 : settings.tickInterval;
            if (now % interval != 0)
            {
                return;
            }

            int n = settings.resolutionsPerPulse <= 0 ? 1 : settings.resolutionsPerPulse;
            for (int i = 0; i < n; i++)
            {
                bool did = false;
                // Alternate morph / diplomacy so neither starves the pulse.
                if ((pulseIndex % 2 == 0 || !settings.diplomacyEnabled) && settings.morphEnabled)
                {
                    did = LivingWorldMorph.TryResolveRandom(this);
                }
                if (!did && settings.diplomacyEnabled)
                {
                    did = LivingWorldDiplomacy.TryResolveRandom(this);
                }
                if (!did && settings.morphEnabled && pulseIndex % 2 != 0)
                {
                    LivingWorldMorph.TryResolveRandom(this);
                }
                pulseIndex++;
            }

            LivingWorldTraffic.TryResolvePulse(this);

            // Delayed fallout: give letters a beat, then try to fire.
            TryFirePendingFallout();
        }

        private void RefreshBudgets(int now)
        {
            Quadrum quadrum = GenDate.Quadrum(now, 0);
            int year = GenDate.Year(now, 0);
            if (!budgetsInitialized)
            {
                budgetsInitialized = true;
                lastQuadrum = quadrum;
                lastYear = year;
                return;
            }
            if (quadrum != lastQuadrum)
            {
                lastQuadrum = quadrum;
                lettersThisQuadrum = 0;
            }
            if (year != lastYear)
            {
                lastYear = year;
                morphsThisYear = 0;
            }
        }

        public bool TryConsumeLetterBudget()
        {
            LivingWorldSettings s = LivingWorldMod.Settings;
            int max = s?.maxLettersPerQuadrum ?? 8;
            if (lettersThisQuadrum >= max)
            {
                return false;
            }
            lettersThisQuadrum++;
            return true;
        }

        public bool HasMorphBudget()
        {
            LivingWorldSettings s = LivingWorldMod.Settings;
            int max = s?.maxMorphsPerYear ?? 6;
            return morphsThisYear < max;
        }

        public void ConsumeMorphBudget()
        {
            morphsThisYear++;
        }

        /// <summary>Legacy helper — prefer HasMorphBudget + ConsumeMorphBudget (AZR-326).</summary>
        public bool TryConsumeMorphBudget()
        {
            if (!HasMorphBudget())
            {
                return false;
            }
            ConsumeMorphBudget();
            return true;
        }

        /// <summary>AZR-332 — one Collapse letter per faction lost to the rim.</summary>
        public bool TryMarkFactionCollapsed(Faction faction)
        {
            if (faction == null)
            {
                return false;
            }
            int id = faction.loadID;
            if (collapsedFactionIds.Contains(id))
            {
                return false;
            }
            collapsedFactionIds.Add(id);
            return true;
        }

        /// <param name="sendLetter">False for routine chronicle-only entries (no letter, no rumour distortion).</param>
        public void RecordAndPublish(WorldEvent ev, bool sendLetter = true)
        {
            if (ev == null)
            {
                return;
            }

            LivingWorldRumour.StampOnPublish(ev, allowDistortion: sendLetter);

            chronicle.Add(ev);
            while (chronicle.Count > ChronicleCapacity)
            {
                WorldEvent evicted = chronicle[0];
                chronicle.RemoveAt(0);
                if (evicted != null && evicted.distorted && !evicted.corrected && evicted.correctionTick >= 0)
                {
                    evictedPendingCorrections.Add(evicted);
                    if (evictedPendingCorrections.Count > EvictedCorrectionCapacity)
                    {
                        evictedPendingCorrections.RemoveAt(0);
                    }
                }
            }

            if (!sendLetter || LivingWorldMod.Settings == null || !LivingWorldMod.Settings.chronicleEnabled)
            {
                LivingWorldSignals.Raise(ev);
                return;
            }

            LivingWorldLetters.TrySend(ev);
            LivingWorldSignals.Raise(ev);
        }

        public FactionPairState GetOrCreatePair(Faction a, Faction b)
        {
            if (a == null || b == null || a == b)
            {
                return null;
            }
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].Matches(a, b))
                {
                    return pairs[i];
                }
            }
            FactionPairState created = FactionPairState.Create(a, b);
            pairs.Add(created);
            return created;
        }

        public void EnqueueFallout(PendingFallout fallout)
        {
            if (fallout == null)
            {
                return;
            }
            pendingFallout.Add(fallout);
        }

        private const int FalloutDelayTicks = 30000;

        public PendingFallout TryDequeueFallout(FalloutKind kind)
        {
            int i = IndexOfReadyFallout(kind);
            if (i < 0)
            {
                return null;
            }
            PendingFallout hit = pendingFallout[i];
            pendingFallout.RemoveAt(i);
            return hit;
        }

        /// <summary>Only entries past the delay; matches what TryDequeueFallout will return.</summary>
        public PendingFallout PeekFallout(FalloutKind kind)
        {
            int i = IndexOfReadyFallout(kind);
            return i < 0 ? null : pendingFallout[i];
        }

        private int IndexOfReadyFallout(FalloutKind kind)
        {
            int now = Find.TickManager.TicksGame;
            for (int i = 0; i < pendingFallout.Count; i++)
            {
                // Wait at least ~half a day so the war letter can land first.
                if (pendingFallout[i].kind == kind && now - pendingFallout[i].enqueueTick >= FalloutDelayTicks)
                {
                    return i;
                }
            }
            return -1;
        }

        private void TryFirePendingFallout()
        {
            if (LivingWorldMod.Settings == null || !LivingWorldMod.Settings.falloutEnabled)
            {
                return;
            }
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                return;
            }

            if (PeekFallout(FalloutKind.Refugees) != null)
            {
                IncidentDef refugees = DefDatabase<IncidentDef>.GetNamedSilentFail("LivingWorld_Refugees");
                if (refugees != null)
                {
                    IncidentParms parms = StorytellerUtility.DefaultParmsNow(refugees.category, map);
                    parms.forced = true;
                    if (refugees.Worker.CanFireNow(parms) && refugees.Worker.TryExecute(parms))
                    {
                        return;
                    }
                }
            }

            if (LivingWorldMod.Settings.warbandFalloutEnabled
                && PeekFallout(FalloutKind.Warband) != null)
            {
                IncidentDef warband = DefDatabase<IncidentDef>.GetNamedSilentFail("LivingWorld_Warband");
                if (warband != null)
                {
                    IncidentParms parms = StorytellerUtility.DefaultParmsNow(warband.category, map);
                    parms.forced = true;
                    if (warband.Worker.CanFireNow(parms))
                    {
                        warband.Worker.TryExecute(parms);
                    }
                }
            }
        }

        public SettlementMood GetOrCreateMood(Settlement settlement)
        {
            if (settlement == null)
            {
                return null;
            }
            if (MoodIndex.TryGetValue(settlement.ID, out SettlementMood existing))
            {
                existing.tile = settlement.Tile;
                return existing;
            }
            var mood = new SettlementMood
            {
                settlementId = settlement.ID,
                tile = settlement.Tile,
            };
            moods.Add(mood);
            moodIndex[mood.settlementId] = mood;
            return mood;
        }

        public SettlementMood TryGetMood(Settlement settlement)
        {
            if (settlement == null)
            {
                return null;
            }
            return MoodIndex.TryGetValue(settlement.ID, out SettlementMood mood) ? mood : null;
        }

        public void RemoveMood(Settlement settlement)
        {
            if (settlement == null)
            {
                return;
            }
            moods.RemoveAll(m => m.settlementId == settlement.ID);
            moodIndex?.Remove(settlement.ID);
        }

        private Dictionary<int, SettlementMood> MoodIndex
        {
            get
            {
                if (moodIndex == null)
                {
                    moodIndex = new Dictionary<int, SettlementMood>(moods.Count);
                    for (int i = 0; i < moods.Count; i++)
                    {
                        SettlementMood m = moods[i];
                        if (m != null && !moodIndex.ContainsKey(m.settlementId))
                        {
                            moodIndex[m.settlementId] = m;
                        }
                    }
                }
                return moodIndex;
            }
        }

        public string DumpChronicle()
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                $"[Living World] Chronicle ({chronicle.Count}) lettersQ={lettersThisQuadrum} "
                + $"morphsY={morphsThisYear} wars={LivingWorldDiplomacy.CountWars(this)} "
                + $"pendingFallout={pendingFallout.Count}");
            for (int i = chronicle.Count - 1; i >= 0 && i >= chronicle.Count - 20; i--)
            {
                WorldEvent ev = chronicle[i];
                sb.AppendLine(
                    $"  t={ev.tick} {ev.kind} sev={ev.severity} seen={ev.seenByPlayer} "
                    + $"{ev.factionAName}/{ev.factionBName} @ {ev.settlementLabel} tile={ev.tile}");
            }
            return sb.ToString();
        }

        public string DumpPairs()
        {
            LivingWorldDiplomacy.EnsurePairs(this);
            var sb = new StringBuilder();
            sb.AppendLine($"[Living World] Faction pairs ({pairs.Count})");
            for (int i = 0; i < pairs.Count; i++)
            {
                sb.AppendLine("  " + pairs[i].DumpLine());
            }
            return sb.ToString();
        }
    }
}

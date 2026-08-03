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

        private List<WorldEvent> chronicle = new List<WorldEvent>();
        private List<SettlementMood> moods = new List<SettlementMood>();
        private List<FactionPairState> pairs = new List<FactionPairState>();
        private List<PendingFallout> pendingFallout = new List<PendingFallout>();

        private int lettersThisQuadrum;
        private int morphsThisYear;
        private bool budgetsInitialized;
        private Quadrum lastQuadrum;
        private int lastYear = -1;
        private int pulseIndex;

        public GameComponent_LivingWorld(Game game)
        {
        }

        public static GameComponent_LivingWorld Get =>
            Current.Game?.GetComponent<GameComponent_LivingWorld>();

        public IReadOnlyList<WorldEvent> Chronicle => chronicle;

        public IReadOnlyList<FactionPairState> Pairs => pairs;

        internal List<FactionPairState> PairsMutable => pairs;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref chronicle, "chronicle", LookMode.Deep);
            Scribe_Collections.Look(ref moods, "moods", LookMode.Deep);
            Scribe_Collections.Look(ref pairs, "pairs", LookMode.Deep);
            Scribe_Collections.Look(ref pendingFallout, "pendingFallout", LookMode.Deep);
            Scribe_Values.Look(ref lettersThisQuadrum, "lettersThisQuadrum");
            Scribe_Values.Look(ref morphsThisYear, "morphsThisYear");
            Scribe_Values.Look(ref budgetsInitialized, "budgetsInitialized");
            Scribe_Values.Look(ref lastQuadrum, "lastQuadrum");
            Scribe_Values.Look(ref lastYear, "lastYear", -1);
            Scribe_Values.Look(ref pulseIndex, "pulseIndex");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                chronicle ??= new List<WorldEvent>();
                moods ??= new List<SettlementMood>();
                pairs ??= new List<FactionPairState>();
                pendingFallout ??= new List<PendingFallout>();
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
            RefreshBudgets(now);
            LivingWorldWarSites.TickExpire();

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

        public bool TryConsumeMorphBudget()
        {
            LivingWorldSettings s = LivingWorldMod.Settings;
            int max = s?.maxMorphsPerYear ?? 6;
            if (morphsThisYear >= max)
            {
                return false;
            }
            morphsThisYear++;
            return true;
        }

        public void RecordAndPublish(WorldEvent ev)
        {
            if (ev == null)
            {
                return;
            }

            chronicle.Add(ev);
            while (chronicle.Count > ChronicleCapacity)
            {
                chronicle.RemoveAt(0);
            }

            if (LivingWorldMod.Settings == null || !LivingWorldMod.Settings.chronicleEnabled)
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

        public PendingFallout TryDequeueFallout(FalloutKind kind)
        {
            for (int i = 0; i < pendingFallout.Count; i++)
            {
                if (pendingFallout[i].kind != kind)
                {
                    continue;
                }
                // Wait at least ~half a day so the war letter can land first.
                if (Find.TickManager.TicksGame - pendingFallout[i].enqueueTick < 30000)
                {
                    continue;
                }
                PendingFallout hit = pendingFallout[i];
                pendingFallout.RemoveAt(i);
                return hit;
            }
            return null;
        }

        public PendingFallout PeekFallout(FalloutKind kind)
        {
            for (int i = 0; i < pendingFallout.Count; i++)
            {
                if (pendingFallout[i].kind == kind)
                {
                    return pendingFallout[i];
                }
            }
            return null;
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
                    warband.Worker.CanFireNow(parms);
                    warband.Worker.TryExecute(parms);
                }
            }
        }

        public SettlementMood GetOrCreateMood(Settlement settlement)
        {
            if (settlement == null)
            {
                return null;
            }
            for (int i = 0; i < moods.Count; i++)
            {
                if (moods[i].settlementId == settlement.ID)
                {
                    moods[i].tile = settlement.Tile;
                    return moods[i];
                }
            }
            var mood = new SettlementMood
            {
                settlementId = settlement.ID,
                tile = settlement.Tile,
            };
            moods.Add(mood);
            return mood;
        }

        public SettlementMood TryGetMood(Settlement settlement)
        {
            if (settlement == null)
            {
                return null;
            }
            for (int i = 0; i < moods.Count; i++)
            {
                if (moods[i].settlementId == settlement.ID)
                {
                    return moods[i];
                }
            }
            return null;
        }

        public void RemoveMood(Settlement settlement)
        {
            if (settlement == null)
            {
                return;
            }
            moods.RemoveAll(m => m.settlementId == settlement.ID);
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

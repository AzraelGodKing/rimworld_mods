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

        private int lettersThisQuadrum;
        private int morphsThisYear;
        private bool budgetsInitialized;
        private Quadrum lastQuadrum;
        private int lastYear = -1;

        public GameComponent_LivingWorld(Game game)
        {
        }

        public static GameComponent_LivingWorld Get =>
            Current.Game?.GetComponent<GameComponent_LivingWorld>();

        public IReadOnlyList<WorldEvent> Chronicle => chronicle;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref chronicle, "chronicle", LookMode.Deep);
            Scribe_Collections.Look(ref moods, "moods", LookMode.Deep);
            Scribe_Values.Look(ref lettersThisQuadrum, "lettersThisQuadrum");
            Scribe_Values.Look(ref morphsThisYear, "morphsThisYear");
            Scribe_Values.Look(ref budgetsInitialized, "budgetsInitialized");
            Scribe_Values.Look(ref lastQuadrum, "lastQuadrum");
            Scribe_Values.Look(ref lastYear, "lastYear", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                chronicle ??= new List<WorldEvent>();
                moods ??= new List<SettlementMood>();
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

            int interval = settings.tickInterval <= 0 ? 10000 : settings.tickInterval;
            if (now % interval != 0)
            {
                return;
            }

            int n = settings.resolutionsPerPulse <= 0 ? 1 : settings.resolutionsPerPulse;
            for (int i = 0; i < n; i++)
            {
                // Phase 1: morph resolutions only (diplomacy is Phase 2).
                if (settings.morphEnabled)
                {
                    LivingWorldMorph.TryResolveRandom(this);
                }
            }
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
            sb.AppendLine($"[Living World] Chronicle ({chronicle.Count}) lettersQ={lettersThisQuadrum} morphsY={morphsThisYear}");
            for (int i = chronicle.Count - 1; i >= 0 && i >= chronicle.Count - 20; i--)
            {
                WorldEvent ev = chronicle[i];
                sb.AppendLine(
                    $"  t={ev.tick} {ev.kind} sev={ev.severity} seen={ev.seenByPlayer} "
                    + $"{ev.factionAName}/{ev.factionBName} @ {ev.settlementLabel} tile={ev.tile}");
            }
            return sb.ToString();
        }
    }
}

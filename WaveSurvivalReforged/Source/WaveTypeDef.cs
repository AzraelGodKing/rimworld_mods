using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WaveSurvivalReforged
{
    public enum WaveKind
    {
        // A raid by a hostile faction, through IncidentDefOf.RaidEnemy.
        Raid,
        // Any other incident, such as a manhunter pack.
        Incident,
    }

    public enum WavePool
    {
        Normal,
        // Every specialWaveInterval-th wave.
        Special,
        // Every bossWaveInterval-th wave. Wins over Special.
        Boss,
    }

    /// <summary>
    /// One kind of wave. Add more by patching or adding defs; DLC waves use MayRequire.
    /// Game defs are named by string and resolved without errors, so a wave whose
    /// incident, strategy, arrival mode or factions are missing is skipped instead.
    /// </summary>
    public class WaveTypeDef : Def
    {
        public WaveKind kind = WaveKind.Raid;
        public WavePool pool = WavePool.Normal;
        public string incident;
        public string raidStrategy = "ImmediateAttack";
        public string arrivalMode;
        // FactionDef names. Empty: any hostile, non-hidden faction that can send a combat group.
        public List<string> factions = new List<string>();
        public int minWave = 1;
        public float weight = 1f;
        public float pointsFactor = 1f;
        // Added after pointsFactor, and scaled by the difficulty's threat scale.
        public float bonusPoints;
        // Only these waves can split into two half-size raids.
        public bool canSplit;

        [Unsaved] private bool resolved;
        [Unsaved] private IncidentDef incidentDef;
        [Unsaved] private RaidStrategyDef strategyDef;
        [Unsaved] private PawnsArrivalModeDef arrivalDef;
        [Unsaved] private List<FactionDef> factionDefs;
        [Unsaved] private bool available;

        public IncidentDef Incident
        {
            get { Resolve(); return incidentDef; }
        }

        public RaidStrategyDef Strategy
        {
            get { Resolve(); return strategyDef; }
        }

        public PawnsArrivalModeDef ArrivalMode
        {
            get { Resolve(); return arrivalDef; }
        }

        // Null: no faction filter.
        public List<FactionDef> FactionDefs
        {
            get { Resolve(); return factionDefs; }
        }

        // False when a named game def is missing (old game version, DLC off, typo).
        public bool Available
        {
            get { Resolve(); return available; }
        }

        public static IEnumerable<WaveTypeDef> InPool(WavePool pool, int wave)
        {
            foreach (WaveTypeDef def in DefDatabase<WaveTypeDef>.AllDefsListForReading)
            {
                if (def.pool == pool && wave >= def.minWave && def.Available)
                    yield return def;
            }
        }

        private void Resolve()
        {
            if (resolved)
                return;
            resolved = true;
            available = true;
            if (kind == WaveKind.Raid)
            {
                incidentDef = IncidentDefOf.RaidEnemy;
                strategyDef = DefDatabase<RaidStrategyDef>.GetNamedSilentFail(raidStrategy ?? "ImmediateAttack");
                available &= strategyDef != null;
                if (!arrivalMode.NullOrEmpty())
                {
                    arrivalDef = DefDatabase<PawnsArrivalModeDef>.GetNamedSilentFail(arrivalMode);
                    available &= arrivalDef != null;
                }
                if (!factions.NullOrEmpty())
                {
                    factionDefs = new List<FactionDef>();
                    foreach (string name in factions)
                    {
                        FactionDef f = DefDatabase<FactionDef>.GetNamedSilentFail(name);
                        if (f != null)
                            factionDefs.Add(f);
                    }
                    available &= factionDefs.Count > 0;
                }
            }
            else
            {
                incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(incident ?? "");
                available &= incidentDef != null;
            }
            if (!available && Prefs.DevMode)
                Log.Message("[WaveSurvivalReforged] Wave type " + defName + " is off: a game def it names is not loaded.");
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
                yield return error;
            if (kind == WaveKind.Incident && incident.NullOrEmpty())
                yield return "kind Incident needs an incident";
            if (weight <= 0f)
                yield return "weight must be above 0";
            if (pointsFactor <= 0f)
                yield return "pointsFactor must be above 0";
            if (bonusPoints < 0f)
                yield return "bonusPoints must be 0 or more";
            if (minWave < 1)
                yield return "minWave must be at least 1";
        }
    }
}

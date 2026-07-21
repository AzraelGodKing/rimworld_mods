using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    /// <summary>
    /// Soft compat for Vanilla Gravship Expanded - Chapter 1 (<c>vanillaexpanded.gravship</c>).
    /// VGE keeps <see cref="Building_GravEngine"/> but adds jumper/hulk engines, scaffold /
    /// damaged substructure terrains, and alternate launch precepts. Fail-open when absent.
    /// </summary>
    public static class StrataVgeCompat
    {
        public const string PackageId = "vanillaexpanded.gravship";

        private static bool _checked;
        private static bool _active;

        // Cached optional defs — null when VGE is not loaded.
        private static ThingDef _gravjumperEngine;
        private static ThingDef _gravhulkEngine;
        private static ThingDef _mechanoidEngine;
        private static TerrainDef _subscaffold;
        private static TerrainDef _damagedSubstructure;
        private static TerrainDef _mechanoidSubstructure;
        private static bool _defsResolved;

        public static bool Active
        {
            get
            {
                if (!_checked)
                {
                    _checked = true;
                    _active = ModLister.GetActiveModWithIdentifier(PackageId) != null
                        || ModsConfig.IsActive(PackageId);
                }
                return _active;
            }
        }

        public static void ResetCaches()
        {
            _checked = false;
            _active = false;
            _defsResolved = false;
            _gravjumperEngine = _gravhulkEngine = _mechanoidEngine = null;
            _subscaffold = _damagedSubstructure = _mechanoidSubstructure = null;
        }

        private static void EnsureDefs()
        {
            if (_defsResolved || !Active) return;
            _defsResolved = true;
            _gravjumperEngine = DefDatabase<ThingDef>.GetNamedSilentFail("VGE_GravjumperEngine");
            _gravhulkEngine = DefDatabase<ThingDef>.GetNamedSilentFail("VGE_GravhulkEngine");
            _mechanoidEngine = DefDatabase<ThingDef>.GetNamedSilentFail("VGE_MechanoidGravEngine");
            _subscaffold = DefDatabase<TerrainDef>.GetNamedSilentFail("VGE_GravshipSubscaffold");
            _damagedSubstructure = DefDatabase<TerrainDef>.GetNamedSilentFail("VGE_DamagedSubstructure");
            _mechanoidSubstructure = DefDatabase<TerrainDef>.GetNamedSilentFail("VGE_MechanoidSubstructure");
        }

        /// <summary>True when this engine def is a known VGE (or vanilla) grav engine.</summary>
        public static bool IsGravEngineDef(ThingDef def)
        {
            if (def == null) return false;
            if (def == ThingDefOf.GravEngine) return true;
            if (!Active) return def.thingClass != null
                && typeof(Building_GravEngine).IsAssignableFrom(def.thingClass);
            EnsureDefs();
            return def == _gravjumperEngine || def == _gravhulkEngine || def == _mechanoidEngine
                || (def.thingClass != null && typeof(Building_GravEngine).IsAssignableFrom(def.thingClass));
        }

        /// <summary>
        /// VGE scaffold / damaged / mechanoid substructure terrains (and any terrain tagged Substructure).
        /// Used as a placement/footprint fallback when engine cell sets are momentarily empty.
        /// </summary>
        public static bool IsShipFoundationTerrain(TerrainDef terrain)
        {
            if (terrain == null) return false;
            if (terrain.HasTag("Substructure")) return true;
            if (!Active) return false;
            EnsureDefs();
            return terrain == _subscaffold
                || terrain == _damagedSubstructure
                || terrain == _mechanoidSubstructure;
        }

        public static bool CellHasShipFoundation(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map)) return false;
            TerrainDef foundation = map.terrainGrid?.FoundationAt(cell);
            if (IsShipFoundationTerrain(foundation)) return true;
            TerrainDef top = map.terrainGrid?.TerrainAt(cell);
            return IsShipFoundationTerrain(top);
        }

        /// <summary>
        /// When several engines exist (VGE warns against this), prefer the one with live substructure,
        /// then the largest connected footprint, then first found.
        /// </summary>
        public static Building_GravEngine PreferBestEngine(IEnumerable<Building_GravEngine> engines)
        {
            Building_GravEngine best = null;
            int bestScore = -1;
            if (engines == null) return null;
            foreach (Building_GravEngine engine in engines)
            {
                if (engine == null || engine.Destroyed || !engine.Spawned) continue;
                int score = ScoreEngine(engine);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = engine;
                }
            }
            return best;
        }

        private static int ScoreEngine(Building_GravEngine engine)
        {
            // Never touch ValidSubstructure / AllConnectedSubstructure here.
            // Those getters call UpdateSubstructureIfNeeded → MapDrawer.RegenerateLayerNow
            // (GravshipMask). FinalizeInit / InitNewGame run on a LongEvent worker
            // thread; regenerating layers off the main thread hard-crashes Unity
            // ("Graphics device is null" / resource load from another thread).
            int score = 1;
            EnsureDefs();
            if (engine.def == ThingDefOf.GravEngine || engine.def == _gravhulkEngine)
            {
                score += 100;
            }
            else if (engine.def == _gravjumperEngine)
            {
                score += 50;
            }
            else if (engine.def == _mechanoidEngine)
            {
                score += 40;
            }

            // Cheap footprint proxy from terrain only (safe off-thread).
            Map map = engine.Map;
            if (map?.terrainGrid != null)
            {
                int foundation = 0;
                CellRect around = CellRect.CenteredOn(engine.Position, 12).ClipInsideMap(map);
                foreach (IntVec3 cell in around)
                {
                    if (CellHasShipFoundation(map, cell))
                    {
                        foundation++;
                    }
                }
                score += foundation;
            }
            return score;
        }

        /// <summary>VGE jumper/hulk launch precepts — treat like vanilla GravshipLaunch for Strata follow.</summary>
        public static bool IsGravshipLaunchPrecept(PreceptDef precept)
        {
            if (precept == null) return false;
            if (precept == PreceptDefOf.GravshipLaunch) return true;
            if (!Active) return false;
            string name = precept.defName ?? "";
            return name == "VGE_GravjumperLaunch" || name == "VGE_GravhulkLaunch";
        }
    }
}

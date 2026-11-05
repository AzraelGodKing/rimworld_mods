using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // What sort of level an excavated floor is. Standard levels are the
    // ordinary rock warren; the rest are special floors picked when the
    // level is first dug.
    public enum StrataLevelKind : byte
    {
        Standard,
        Magma,
    }

    public static class StrataLevelKinds
    {
        // Picked by GenStep_StrataLevelKind and claimed by the map component
        // on FinalizeInit (the component does not exist yet while gen steps run).
        private static readonly Dictionary<int, StrataLevelKind> pendingByMapId =
            new Dictionary<int, StrataLevelKind>();

        // OutdoorTemp asks for the kind constantly, so keep a flat lookup
        // instead of walking map.components each time.
        private static readonly Dictionary<int, StrataLevelKind> kindByMapId =
            new Dictionary<int, StrataLevelKind>();

        public static StrataLevelKind KindOf(Map map)
        {
            if (map == null)
            {
                return StrataLevelKind.Standard;
            }
            if (kindByMapId.TryGetValue(map.uniqueID, out StrataLevelKind kind))
            {
                return kind;
            }
            // During generation the component is not attached yet.
            return pendingByMapId.TryGetValue(map.uniqueID, out kind) ? kind : StrataLevelKind.Standard;
        }

        public static bool IsMagma(Map map) => KindOf(map) == StrataLevelKind.Magma;

        internal static void SetPending(Map map, StrataLevelKind kind)
        {
            pendingByMapId[map.uniqueID] = kind;
        }

        internal static bool TryClaimPending(Map map, out StrataLevelKind kind)
        {
            if (pendingByMapId.TryGetValue(map.uniqueID, out kind))
            {
                pendingByMapId.Remove(map.uniqueID);
                return true;
            }
            return false;
        }

        internal static bool HasPending(Map map) => map != null && pendingByMapId.ContainsKey(map.uniqueID);

        internal static void Register(Map map, StrataLevelKind kind)
        {
            if (kind == StrataLevelKind.Standard)
            {
                kindByMapId.Remove(map.uniqueID);
            }
            else
            {
                kindByMapId[map.uniqueID] = kind;
            }
        }

        internal static void Forget(Map map)
        {
            if (map != null)
            {
                kindByMapId.Remove(map.uniqueID);
                pendingByMapId.Remove(map.uniqueID);
            }
        }

        public static void ResetSession()
        {
            pendingByMapId.Clear();
            kindByMapId.Clear();
        }

        // Only fresh dig-down levels get a special kind: not gravship decks,
        // quest sites or ancient-stair levels that use their own generators.
        internal static StrataLevelKind PickFor(Map map)
        {
            if (map?.generatorDef?.defName != StrataMapUtility.UndergroundGenerator
                || PocketMapUtility.currentlyGeneratingPortal is IStrataGravshipPortal)
            {
                return StrataLevelKind.Standard;
            }
            int depth = StrataDepth.CountLevelsBelowSurface(map);
            if (MagmaLayer.IsMagmaDepth(depth))
            {
                return StrataLevelKind.Magma;
            }
            return StrataLevelKind.Standard;
        }
    }

    // Runs before the rock is laid so later steps (and the outdoor
    // temperature patch) already know which kind of level this is.
    public class GenStep_StrataLevelKind : GenStep
    {
        public override int SeedPart => 604117283;

        public override void Generate(Map map, GenStepParams parms)
        {
            StrataLevelKind kind = StrataLevelKinds.PickFor(map);
            if (kind != StrataLevelKind.Standard)
            {
                StrataLevelKinds.SetPending(map, kind);
            }
        }
    }

    // Saves the level kind and places the kind's features once the rock fill
    // is done. Rock is spawned in a long event after generation, so features
    // that sit in the rock (seams, pools) wait for the first tick.
    public class MapComponent_StrataLevelKind : MapComponent
    {
        private StrataLevelKind kind;
        private bool featuresPlaced;
        private List<IntVec3> poolCenters = new List<IntVec3>();
        private List<float> poolRadii = new List<float>();

        public MapComponent_StrataLevelKind(Map map) : base(map)
        {
        }

        public StrataLevelKind Kind => kind;

        public IReadOnlyList<IntVec3> PoolCenters => poolCenters;

        public IReadOnlyList<float> PoolRadii => poolRadii;

        internal void AddPool(IntVec3 center, float radius)
        {
            poolCenters.Add(center);
            poolRadii.Add(radius);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (StrataLevelKinds.TryClaimPending(map, out StrataLevelKind picked))
            {
                kind = picked;
            }
            StrataLevelKinds.Register(map, kind);
        }

        public override void MapComponentTick()
        {
            if (kind == StrataLevelKind.Standard)
            {
                return;
            }
            if (!featuresPlaced)
            {
                featuresPlaced = true;
                if (kind == StrataLevelKind.Magma)
                {
                    StrataFaultGuard.Run(StrataFaultGuard.System.LevelGeneration, "placing the magma layer",
                        () => MagmaLayer.PlaceFeatures(map, this));
                }
                return;
            }
            if (kind == StrataLevelKind.Magma && map.IsHashIntervalTick(MagmaLayer.HazardIntervalTicks))
            {
                MagmaLayer.TickHazards(map, this);
            }
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            StrataLevelKinds.Forget(map);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref kind, "strataLevelKind", StrataLevelKind.Standard);
            Scribe_Values.Look(ref featuresPlaced, "strataLevelFeaturesPlaced");
            Scribe_Collections.Look(ref poolCenters, "strataMagmaPoolCenters", LookMode.Value);
            Scribe_Collections.Look(ref poolRadii, "strataMagmaPoolRadii", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                poolCenters ??= new List<IntVec3>();
                poolRadii ??= new List<float>();
                if (poolRadii.Count != poolCenters.Count)
                {
                    poolCenters.Clear();
                    poolRadii.Clear();
                }
            }
        }
    }
}

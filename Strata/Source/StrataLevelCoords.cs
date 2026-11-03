using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Where a level's cells sit relative to the map it hangs off. Only levels
    // opened smaller than their source map carry a frame; everything else is
    // either the same size (1:1) or a legacy size mismatch (proportional).
    public class MapComponent_StrataLevelFrame : MapComponent
    {
        // Source-map cell that this level's (0,0) sits on.
        public IntVec3 offset = IntVec3.Zero;
        public bool hasFrame;

        public MapComponent_StrataLevelFrame(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hasFrame, "hasFrame", defaultValue: false);
            Scribe_Values.Look(ref offset, "offset", IntVec3.Zero);
        }
    }

    // The one place cross-map cells are converted. Rules, in order:
    //   same map                      -> unchanged
    //   gravship stack                -> raw 1:1 (decks paint on the hull cells)
    //   both maps share a root frame  -> cell + originFrom - originTo (may land
    //                                    outside the target; callers check InBounds)
    //   same size                     -> unchanged
    //   otherwise (pre-frame levels)  -> proportional, clamped
    public static class StrataLevelCoords
    {
        private struct RootInfo
        {
            public Map root;
            public IntVec3 origin;
            public bool valid;
        }

        private static readonly Dictionary<int, RootInfo> rootCache = new Dictionary<int, RootInfo>();
        private static IntVec3? pendingOffset;

        public static void ResetSession()
        {
            rootCache.Clear();
            pendingOffset = null;
        }

        public static void Invalidate()
        {
            rootCache.Clear();
        }

        // Wrap PocketMapUtility.GeneratePocketMap with Begin/End so gen steps
        // (landing placement, deck paint) already see the new level's frame.
        public static void BeginPendingFrame(IntVec3 offset)
        {
            pendingOffset = offset;
            rootCache.Clear();
        }

        public static void EndPendingFrame(Map generated)
        {
            if (pendingOffset.HasValue && generated != null)
            {
                RegisterFrame(generated, pendingOffset.Value);
            }
            pendingOffset = null;
        }

        public static void RegisterFrame(Map map, IntVec3 offset)
        {
            if (map == null)
            {
                return;
            }
            MapComponent_StrataLevelFrame frame = map.GetComponent<MapComponent_StrataLevelFrame>();
            if (frame == null)
            {
                frame = new MapComponent_StrataLevelFrame(map);
                map.components.Add(frame);
            }
            frame.offset = offset;
            frame.hasFrame = true;
            rootCache.Clear();
        }

        public static bool HasFrame(Map map)
        {
            return FrameOf(map)?.hasFrame == true;
        }

        public static IntVec3 OffsetInSource(Map map)
        {
            MapComponent_StrataLevelFrame frame = FrameOf(map);
            return frame != null && frame.hasFrame ? frame.offset : IntVec3.Zero;
        }

        private static MapComponent_StrataLevelFrame FrameOf(Map map)
        {
            if (map == null)
            {
                return null;
            }
            MapComponent_StrataLevelFrame frame = map.GetComponent<MapComponent_StrataLevelFrame>();
            if (frame == null && pendingOffset.HasValue && map == MapGenerator.mapBeingGenerated)
            {
                RegisterFrame(map, pendingOffset.Value);
                frame = map.GetComponent<MapComponent_StrataLevelFrame>();
            }
            return frame;
        }

        public static IntVec3 Translate(IntVec3 pos, Map from, Map to)
        {
            if (from == null || to == null || from == to || !pos.IsValid)
            {
                return pos;
            }
            if (UsesRawOneToOne(from, to))
            {
                return pos;
            }
            RootInfo a = RootOf(from);
            RootInfo b = RootOf(to);
            if (a.valid && b.valid && a.root == b.root)
            {
                return pos + a.origin - b.origin;
            }
            if (from.Size == to.Size)
            {
                return pos;
            }
            return LegacyProportional(pos, from, to);
        }

        public static IntVec3 TranslateClamped(IntVec3 pos, Map from, Map to)
        {
            IntVec3 cell = Translate(pos, from, to);
            if (to == null || !cell.IsValid)
            {
                return to?.Center ?? IntVec3.Invalid;
            }
            cell.x = Mathf.Clamp(cell.x, 0, to.Size.x - 1);
            cell.z = Mathf.Clamp(cell.z, 0, to.Size.z - 1);
            cell.y = 0;
            return cell;
        }

        // Pre-frame behaviour: scale by map size. Kept for levels opened before
        // frames existed whose size differs from the map above.
        public static IntVec3 LegacyProportional(IntVec3 pos, Map from, Map to)
        {
            if (from == null || to == null || from.Size.x <= 0 || from.Size.z <= 0)
            {
                return to?.Center ?? IntVec3.Invalid;
            }
            int x = Mathf.Clamp(Mathf.RoundToInt((float)pos.x / from.Size.x * to.Size.x), 0, to.Size.x - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt((float)pos.z / from.Size.z * to.Size.z), 0, to.Size.z - 1);
            return new IntVec3(x, 0, z);
        }

        public static bool UsesRawOneToOne(Map a, Map b)
        {
            if (!StrataGravshipUtility.OdysseyActive)
            {
                return false;
            }
            return StrataGravshipUtility.IsInGravshipStack(a)
                || StrataGravshipUtility.IsInGravshipStack(b);
        }

        public const int MinLevelSize = 50;
        public const int DeckMargin = 15;

        public static Map RootMap(Map map)
        {
            if (map == null)
            {
                return null;
            }
            RootInfo info = RootOf(map);
            return info.valid ? info.root : null;
        }

        // A B level covering `fraction` of the home map, centred on the stairwell
        // and kept inside the home map. False when the full source size should
        // be used (fraction 1, or the source sits on a legacy-sized chain).
        public static bool TryPlanBelowLevel(Map source, IntVec3 anchor, float fraction,
            out IntVec3 size, out IntVec3 offsetInSource)
        {
            size = source != null ? new IntVec3(source.Size.x, 1, source.Size.z) : IntVec3.Zero;
            offsetInSource = IntVec3.Zero;
            if (source == null || fraction >= 0.999f)
            {
                return false;
            }
            RootInfo root = RootOf(source);
            if (!root.valid || root.root == null)
            {
                return false;
            }
            IntVec3 rootSize = root.root.Size;
            int sx = Mathf.Clamp(Mathf.RoundToInt(rootSize.x * fraction), Mathf.Min(MinLevelSize, rootSize.x), rootSize.x);
            int sz = Mathf.Clamp(Mathf.RoundToInt(rootSize.z * fraction), Mathf.Min(MinLevelSize, rootSize.z), rootSize.z);
            IntVec3 anchorRoot = anchor + root.origin;
            int ox = Mathf.Clamp(anchorRoot.x - sx / 2, 0, rootSize.x - sx);
            int oz = Mathf.Clamp(anchorRoot.z - sz / 2, 0, rootSize.z - sz);
            size = new IntVec3(sx, 1, sz);
            offsetInSource = new IntVec3(ox, 0, oz) - root.origin;
            return sx != source.Size.x || sz != source.Size.z || offsetInSource != IntVec3.Zero;
        }

        // A+ deck sized to the constructed-roof bounding box below (plus the
        // stairwell and a margin), kept inside the source map.
        public static bool TryPlanRoofDeck(Map source, IntVec3 anchor, out IntVec3 size, out IntVec3 offsetInSource)
        {
            size = source != null ? new IntVec3(source.Size.x, 1, source.Size.z) : IntVec3.Zero;
            offsetInSource = IntVec3.Zero;
            if (source == null)
            {
                return false;
            }
            int minX = anchor.x, maxX = anchor.x, minZ = anchor.z, maxZ = anchor.z;
            RoofGrid roofs = source.roofGrid;
            foreach (IntVec3 cell in source.AllCells)
            {
                RoofDef roof = roofs.RoofAt(cell);
                if (roof == null || roof.isThickRoof)
                {
                    continue;
                }
                if (cell.x < minX) minX = cell.x;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.z < minZ) minZ = cell.z;
                if (cell.z > maxZ) maxZ = cell.z;
            }
            CellRect rect = CellRect.FromLimits(minX, minZ, maxX, maxZ).ExpandedBy(DeckMargin);
            int min = Mathf.Min(MinLevelSize, Mathf.Min(source.Size.x, source.Size.z));
            if (rect.Width < min)
            {
                rect = rect.ExpandedBy((min - rect.Width + 1) / 2);
            }
            if (rect.Height < min)
            {
                rect = rect.ExpandedBy((min - rect.Height + 1) / 2);
            }
            rect = rect.ClipInsideMap(source);
            size = new IntVec3(rect.Width, 1, rect.Height);
            offsetInSource = new IntVec3(rect.minX, 0, rect.minZ);
            return rect.Width != source.Size.x || rect.Height != source.Size.z;
        }

        public static string CellCostLabel(IntVec3 size)
        {
            return size.x + "x" + size.z + " (" + (size.x * size.z).ToStringCached() + " cells)";
        }

        // The level's footprint in its source map's cells (whole map when unframed).
        public static CellRect FootprintInSource(Map level)
        {
            if (level == null)
            {
                return CellRect.Empty;
            }
            IntVec3 o = OffsetInSource(level);
            return new CellRect(o.x, o.z, level.Size.x, level.Size.z);
        }

        // Walk sourceMap links summing frame offsets. A same-size unframed link
        // is 1:1; a different-size unframed link breaks the chain (legacy).
        private static RootInfo RootOf(Map map)
        {
            if (rootCache.TryGetValue(map.uniqueID, out RootInfo cached)
                && (cached.root == null || !cached.root.Disposed))
            {
                return cached;
            }
            RootInfo info = new RootInfo { root = map, origin = IntVec3.Zero, valid = true };
            Map current = map;
            int guard = 0;
            while (guard++ < 64)
            {
                Map source = SourceMap(current);
                if (source == null || source == current || source.Disposed)
                {
                    break;
                }
                MapComponent_StrataLevelFrame frame = FrameOf(current);
                if (frame != null && frame.hasFrame)
                {
                    info.origin += frame.offset;
                }
                else if (current.Size != source.Size)
                {
                    info.valid = false;
                    break;
                }
                current = source;
            }
            info.root = current;
            // Never cache mid-generation: the frame may not be registered yet.
            if (MapGenerator.mapBeingGenerated == null)
            {
                rootCache[map.uniqueID] = info;
            }
            return info;
        }

        private static Map SourceMap(Map level)
        {
            if (level?.Parent is PocketMapParent pocket && pocket.sourceMap != null)
            {
                return pocket.sourceMap;
            }
            if (level != null && level == MapGenerator.mapBeingGenerated)
            {
                return PocketMapUtility.currentlyGeneratingPortal?.Map;
            }
            return null;
        }
    }
}

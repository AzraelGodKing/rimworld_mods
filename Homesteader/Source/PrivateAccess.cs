#nullable enable
using System;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#else
using HarmonyLib;
#endif
using Verse;

namespace Homesteader
{
    /// <summary>
    /// The two private game fields Homesteader reads (AZR-543, AZR-695). Each is
    /// probed once at startup: a renamed field logs one clear message and the
    /// feature that needs it falls back, instead of silently doing nothing.
    /// RimWorld 1.7 (.NET 10) binds with [UnsafeAccessor]; 1.6 uses Harmony
    /// FieldRefs. Neither boxes or reflects per call.
    /// </summary>
    internal static class PrivateAccess
    {
#if NET8_0_OR_GREATER
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "map")]
        private static extern ref Map? UpdaterMap(RegionAndRoomUpdater updater);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "cachedGraphic")]
        private static extern ref Graphic? CachedGraphic(GraphicData data);
#else
        // Declared before the probes below: static fields initialise in order.
        private static readonly AccessTools.FieldRef<RegionAndRoomUpdater, Map?>? UpdaterMapRef =
            TryFieldRef<RegionAndRoomUpdater, Map?>("map");

        private static readonly AccessTools.FieldRef<GraphicData, Graphic?>? CachedGraphicRef =
            TryFieldRef<GraphicData, Graphic?>("cachedGraphic");

        // A missing field leaves the ref null; the probe's call then throws and is caught.
        private static ref Map? UpdaterMap(RegionAndRoomUpdater updater) => ref UpdaterMapRef!(updater);

        private static ref Graphic? CachedGraphic(GraphicData data) => ref CachedGraphicRef!(data);

        private static AccessTools.FieldRef<T, F>? TryFieldRef<T, F>(string field)
        {
            try
            {
                return AccessTools.Field(typeof(T), field) != null ? AccessTools.FieldRefAccess<T, F>(field) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
#endif

        /// <summary>RegionAndRoomUpdater.map, read on every room rebuild.</summary>
        internal static readonly bool UpdaterMapAvailable = Probe(
            "RegionAndRoomUpdater.map",
#if NET8_0_OR_GREATER
            () => _ = UpdaterMap((RegionAndRoomUpdater)RuntimeHelpers.GetUninitializedObject(typeof(RegionAndRoomUpdater))));
#else
            () => _ = UpdaterMapRef ?? throw new MissingFieldException("RegionAndRoomUpdater", "map"));
#endif

        /// <summary>GraphicData.cachedGraphic, cleared when the refresh texture pack swaps art at startup.</summary>
        internal static readonly bool CachedGraphicAvailable = Probe(
            "GraphicData.cachedGraphic",
            () => _ = CachedGraphic(new GraphicData()));

        internal static Map? MapOf(RegionAndRoomUpdater? updater)
        {
            return updater != null && UpdaterMapAvailable ? UpdaterMap(updater) : null;
        }

        internal static bool TryClearCachedGraphic(GraphicData? data)
        {
            if (data == null || !CachedGraphicAvailable)
            {
                return false;
            }

            CachedGraphic(data) = null;
            return true;
        }

        private static bool Probe(string name, Action read)
        {
            try
            {
                read();
                return true;
            }
            catch (Exception e)
            {
                Log.Warning("[Homesteader] Private field " + name + " not found on this RimWorld build ("
                    + e.GetType().Name + "). The feature that needs it falls back.");
                return false;
            }
        }

    }
}

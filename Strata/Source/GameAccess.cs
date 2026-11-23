using System.Collections.Generic;
using Verse;
using Verse.AI;
#if RIMWORLD_1_7
using System.Runtime.CompilerServices;
#else
using HarmonyLib;
#endif

namespace Strata
{
    // AZR-462: private game fields read on hot paths (every CanReserve, every
    // see-below frame, every cross-floor job probe). On 1.7 (.NET 10) these
    // are [UnsafeAccessor] externs the JIT binds to a direct field access;
    // on 1.6 (Mono, net472) they stay Harmony FieldRefs. Same call shape on
    // both, so callers need no #if. ModChecks UnsafeAccessorNames checks the
    // 1.7 names against the 1.7 refs; the 1.6 FieldRefs throw at load if wrong.
    internal static class GameAccess
    {
#if RIMWORLD_1_7
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "map")]
        internal static extern ref Map ReservationMap(ReservationManager manager);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "reservations")]
        internal static extern ref List<ReservationManager.Reservation> Reservations(ReservationManager manager);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "mapIndexOrState")]
        internal static extern ref sbyte MapIndexOrState(Thing thing);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "positionInt")]
        internal static extern ref IntVec3 PositionInt(Thing thing);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "layers")]
        internal static extern ref List<SectionLayer> SectionLayers(Section section);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "sections")]
        internal static extern ref Section[,] Sections(MapDrawer drawer);
#else
        private static readonly AccessTools.FieldRef<ReservationManager, Map> reservationMap =
            AccessTools.FieldRefAccess<ReservationManager, Map>("map");

        private static readonly AccessTools.FieldRef<ReservationManager, List<ReservationManager.Reservation>> reservations =
            AccessTools.FieldRefAccess<ReservationManager, List<ReservationManager.Reservation>>("reservations");

        private static readonly AccessTools.FieldRef<Thing, sbyte> mapIndexOrState =
            AccessTools.FieldRefAccess<Thing, sbyte>("mapIndexOrState");

        private static readonly AccessTools.FieldRef<Thing, IntVec3> positionInt =
            AccessTools.FieldRefAccess<Thing, IntVec3>("positionInt");

        private static readonly AccessTools.FieldRef<Section, List<SectionLayer>> sectionLayers =
            AccessTools.FieldRefAccess<Section, List<SectionLayer>>("layers");

        private static readonly AccessTools.FieldRef<MapDrawer, Section[,]> sections =
            AccessTools.FieldRefAccess<MapDrawer, Section[,]>("sections");

        internal static ref Map ReservationMap(ReservationManager manager) => ref reservationMap(manager);

        internal static ref List<ReservationManager.Reservation> Reservations(ReservationManager manager) => ref reservations(manager);

        internal static ref sbyte MapIndexOrState(Thing thing) => ref mapIndexOrState(thing);

        internal static ref IntVec3 PositionInt(Thing thing) => ref positionInt(thing);

        internal static ref List<SectionLayer> SectionLayers(Section section) => ref sectionLayers(section);

        internal static ref Section[,] Sections(MapDrawer drawer) => ref sections(drawer);
#endif
    }
}

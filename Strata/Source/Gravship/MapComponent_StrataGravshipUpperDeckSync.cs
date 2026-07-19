using RimWorld;
using Verse;

namespace Strata
{
    // Gravship A+ decks follow substructure downstairs, not colony roofs. When the
    // ship footprint grows or shrinks, refresh the linked upper pocket map.
    public class MapComponent_StrataGravshipUpperDeckSync : MapComponent
    {
        private int lastSubstructureFingerprint;
        private int lastSubstructureCount = -1;
        private bool forceSyncNext;

        public MapComponent_StrataGravshipUpperDeckSync(Map map) : base(map)
        {
        }

        // Land path: Odyssey may still be wiring ValidSubstructure — force a
        // refresh on the next tick once the footprint exists.
        public static void RequestSync(Map host)
        {
            if (host == null)
            {
                return;
            }
            var comp = host.GetComponent<MapComponent_StrataGravshipUpperDeckSync>();
            if (comp == null)
            {
                comp = new MapComponent_StrataGravshipUpperDeckSync(host);
                host.components.Add(comp);
            }
            comp.forceSyncNext = true;
            comp.lastSubstructureCount = -1;
            comp.lastSubstructureFingerprint = 0;
        }

        public override void MapComponentTick()
        {
            if (!StrataGravshipUtility.OdysseyActive || !StrataGravshipUtility.IsGravshipHostMap(map))
            {
                return;
            }
            bool forced = forceSyncNext;
            if (!forced && !map.IsHashIntervalTick(60))
            {
                return;
            }
            // No gravship shafts / elevators on this host → nothing to sync.
            if (!forced && !AnyGravshipPortal())
            {
                return;
            }
            Building_GravEngine engine = StrataGravshipUtility.FindGravEngine(map);
            if (engine == null)
            {
                return;
            }
            System.Collections.Generic.HashSet<IntVec3> sub = engine.ValidSubstructure;
            if (sub == null || sub.Count == 0)
            {
                sub = engine.AllConnectedSubstructure;
            }
            int count = sub?.Count ?? 0;
            // Do not paint/sync while empty — that wipes travelling underdeck decks.
            if (count == 0)
            {
                return;
            }
            forceSyncNext = false;
            if (!forced && count == lastSubstructureCount && !map.IsHashIntervalTick(300))
            {
                return;
            }
            int fingerprint = SubstructureFingerprint(sub, count);
            if (!forced && fingerprint == lastSubstructureFingerprint && count == lastSubstructureCount)
            {
                return;
            }
            lastSubstructureCount = count;
            lastSubstructureFingerprint = fingerprint;
            if (forced)
            {
                // Land deferred sync: snap landings under shafts again, then
                // grow-only deck paint (keeps travelling rooms off impassable hull).
                var pockets = new System.Collections.Generic.List<Map>();
                foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (thing is IStrataGravshipPortal && thing is MapPortal portal
                        && portal.PocketMapExists)
                    {
                        pockets.Add(portal.PocketMap);
                    }
                }
                Map under = StrataGravshipOrphanLevels.FindAdoptableUnderdeck(map);
                if (under != null && !pockets.Contains(under))
                {
                    pockets.Add(under);
                }
                Map upper = StrataGravshipOrphanLevels.FindAdoptableUpperDeck(map);
                if (upper != null && !pockets.Contains(upper))
                {
                    pockets.Add(upper);
                }
                if (pockets.Count > 0)
                {
                    StrataGravshipPocketAlign.AlignPocketsToLandedShip(
                        pockets, map, IntVec3.Invalid);
                }
                StrataGravshipFootprintSnapshot.RebuildLinkedFloors(map, pockets);
                return;
            }
            UpperDeckUtility.SyncGravshipUpperDecksFromSource(map);
            GravshipDeckUtility.SyncUnderdecksFromHost(map);
            StrataGravshipSubstructureSync.SyncAllLinkedFromHost(map);
            // Snap contents onto footprint + rebuild projected substructure / clear orphans.
            StrataGravshipFootprintSnapshot.RebuildLinkedFloors(map, null);
        }

        private bool AnyGravshipPortal()
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is IStrataGravshipPortal)
                {
                    return true;
                }
            }
            return false;
        }

        private static int SubstructureFingerprint(System.Collections.Generic.HashSet<IntVec3> sub, int count)
        {
            if (sub == null || count == 0)
            {
                return 0;
            }
            int hash = count;
            foreach (IntVec3 cell in sub)
            {
                hash = unchecked(hash * 31 + cell.GetHashCode());
            }
            return hash;
        }
    }
}

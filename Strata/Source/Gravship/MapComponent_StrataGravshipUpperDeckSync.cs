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

        public MapComponent_StrataGravshipUpperDeckSync(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (!StrataGravshipUtility.OdysseyActive || !StrataGravshipUtility.IsGravshipHostMap(map))
            {
                return;
            }
            if (!map.IsHashIntervalTick(60))
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
            if (count == lastSubstructureCount && !map.IsHashIntervalTick(300))
            {
                return;
            }
            int fingerprint = SubstructureFingerprint(sub, count);
            if (fingerprint == lastSubstructureFingerprint && count == lastSubstructureCount)
            {
                return;
            }
            lastSubstructureCount = count;
            lastSubstructureFingerprint = fingerprint;
            UpperDeckUtility.SyncGravshipUpperDecksFromSource(map);
            GravshipDeckUtility.SyncUnderdecksFromHost(map);
            StrataGravshipSubstructureSync.SyncAllLinkedFromHost(map);
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

using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // G3: defer CompleteLanding footprint work until host ValidSubstructure (or VGE
    // foundation proxy) is non-empty — single deferred path via UpperDeckSync.
    public static class StrataGravshipLandGate
    {
        public class PendingLand : IExposable
        {
            public List<int> mapIds = new List<int>();
            public IntVec3 takeoffEnginePos = IntVec3.Invalid;
            public int engineThingId = -1;
            public int gravshipLoadId = -1;

            public void ExposeData()
            {
                Scribe_Collections.Look(ref mapIds, "mapIds", LookMode.Value);
                Scribe_Values.Look(ref takeoffEnginePos, "takeoffEnginePos", IntVec3.Invalid);
                Scribe_Values.Look(ref engineThingId, "engineThingId", -1);
                Scribe_Values.Look(ref gravshipLoadId, "gravshipLoadId", -1);
                mapIds ??= new List<int>();
            }
        }

        public static bool HostDeckReady(Building_GravEngine engine, Map host)
        {
            if (StrataGravshipUtility.EngineHasSubstructure(engine))
            {
                return true;
            }
            if (host == null || engine == null || !StrataVgeCompat.Active)
            {
                return false;
            }
            // VGE foundation while Odyssey cell sets are still empty mid-land.
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(engine.Position, 12f, true))
            {
                if (cell.InBounds(host) && StrataVgeCompat.CellHasShipFoundation(host, cell))
                {
                    return true;
                }
            }
            return false;
        }

        public static void ArmPending(
            Map host,
            List<Map> maps,
            IntVec3 takeoffPos,
            int engineThingId,
            Gravship ship)
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
            var pending = new PendingLand
            {
                takeoffEnginePos = takeoffPos,
                engineThingId = engineThingId,
                gravshipLoadId = ship?.ID ?? -1,
                mapIds = new List<int>(),
            };
            if (maps != null)
            {
                for (int i = 0; i < maps.Count; i++)
                {
                    if (maps[i] != null)
                    {
                        pending.mapIds.Add(maps[i].uniqueID);
                    }
                }
            }
            comp.ArmPendingLand(pending);
            MapComponent_StrataGravshipUpperDeckSync.RequestSync(host);
            Log.Message("[Strata] G3 land gate: deferred CompleteLanding until host substructure is ready ("
                + pending.mapIds.Count + " level(s)).");
        }
    }
}

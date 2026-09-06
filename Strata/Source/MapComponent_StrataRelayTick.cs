using Verse;

namespace Strata
{
    /// <summary>
    /// Stair-arrival job finish must run on map async time. World DoSingleTick
    /// in Multiplayer is a different clock; MakeJob there desyncs UniqueIDs.
    /// </summary>
    public class MapComponent_StrataRelayTick : MapComponent
    {
        public MapComponent_StrataRelayTick(Map map)
            : base(map)
        {
        }

        public override void MapComponentTick()
        {
            StrataPortalUtility.TickHaulDeliveries(map);
            PortalRelayChain.TickMap(map);
            DraftedPortalPathing.TickMap(map);
            StrataCaravanUtility.TickCaravanPull(map);
        }
    }
}

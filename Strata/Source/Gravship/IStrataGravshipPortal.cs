namespace Strata
{
    // Marker for Odyssey gravship stair/elevator portals. Stack-follow and
    // IsGravshipMap detection key off this instead of guessing from substructure.
    public interface IStrataGravshipPortal
    {
        bool IsGravshipPortal { get; }

        // True when this portal currently sits on gravship substructure.
        bool IsOnGravship { get; }
    }
}

using Verse;

namespace Strata
{
    // DEPRECATED. The cargo lift was removed - the pawn haul relay plus a
    // stockpile by the stairs does the job. This empty stub and its non-
    // buildable def exist only so saves made while the lift still existed load
    // cleanly; deconstruct any leftover lifts and they're gone for good. Remove
    // once no save references it.
    public class Building_CargoLift : Building
    {
    }
}

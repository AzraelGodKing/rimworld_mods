using Verse;
using Verse.AI;

namespace RimWorld;

public class WorkGiver_ConstructDeliverResourcesToBlueprints : WorkGiver_ConstructDeliverResources
{
	public override extern ThingRequest PotentialWorkThingRequest { get; }

	public override extern bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false);

	public override extern Job JobOnThing(Pawn pawn, Thing t, bool forced = false);

	private static extern bool CanDoRemoveExistingFloorWork(Pawn pawn, Blueprint blue);

	private extern Job NoCostFrameMakeJobFor(IConstructible c);

	public extern WorkGiver_ConstructDeliverResourcesToBlueprints();
}

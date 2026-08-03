using Verse;
using Verse.AI;

namespace RimWorld;

public class WorkGiver_ConstructDeliverResourcesToFrames : WorkGiver_ConstructDeliverResources
{
	public override extern ThingRequest PotentialWorkThingRequest { get; }

	public override extern bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false);

	public override extern Job JobOnThing(Pawn pawn, Thing t, bool forced = false);

	public extern WorkGiver_ConstructDeliverResourcesToFrames();
}

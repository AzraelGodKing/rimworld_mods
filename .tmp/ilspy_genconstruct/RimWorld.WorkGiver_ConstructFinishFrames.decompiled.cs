using Verse;
using Verse.AI;

namespace RimWorld;

public class WorkGiver_ConstructFinishFrames : WorkGiver_Scanner
{
	public override extern PathEndMode PathEndMode { get; }

	public override extern ThingRequest PotentialWorkThingRequest { get; }

	public override extern Danger MaxPathDanger(Pawn pawn);

	public override extern Job JobOnThing(Pawn pawn, Thing t, bool forced = false);

	public extern WorkGiver_ConstructFinishFrames();
}

using Verse;
using Verse.AI;

namespace RimWorld;

public class WorkGiver_ConstructDeliverResourcesToBlueprints : WorkGiver_ConstructDeliverResources
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Blueprint);

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t.Faction != pawn.Faction)
		{
			return false;
		}
		if (!(t is Blueprint blueprint))
		{
			return false;
		}
		if (blueprint.def.entityDefToBuild is ThingDef { plant: not null })
		{
			return false;
		}
		if (!GenConstruct.CanTouchTargetFromValidCell(blueprint, pawn))
		{
			return false;
		}
		if (GenConstruct.FirstBlockingThing(blueprint, pawn) != null)
		{
			return GenConstruct.HandleBlockingThingJob(blueprint, pawn, forced) != null;
		}
		if (!GenConstruct.CanConstruct(blueprint, pawn, def.workType, forced, JobDefOf.HaulToContainer))
		{
			return false;
		}
		if (def.workType != WorkTypeDefOf.Construction && WorkGiver_ConstructDeliverResources.ShouldRemoveExistingFloorFirst(pawn, blueprint))
		{
			return false;
		}
		if (CanDoRemoveExistingFloorWork(pawn, blueprint))
		{
			return true;
		}
		if (blueprint is Blueprint_Install)
		{
			return ResourceDeliverJobFor(pawn, blueprint, canRemoveExistingFloorUnderNearbyNeeders: true, forced) != null;
		}
		if (blueprint.TotalMaterialCost().Count == 0)
		{
			return def.workType != WorkTypeDefOf.Hauling;
		}
		if (!GenConstruct.CanGetResources_NewTemp(blueprint, pawn, forced))
		{
			return false;
		}
		return ResourceDeliverJobFor(pawn, blueprint, canRemoveExistingFloorUnderNearbyNeeders: true, forced) != null;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t.Faction != pawn.Faction)
		{
			return null;
		}
		if (!(t is Blueprint blueprint))
		{
			return null;
		}
		if (blueprint.def.entityDefToBuild is ThingDef { plant: not null })
		{
			return null;
		}
		if (!GenConstruct.CanTouchTargetFromValidCell(blueprint, pawn))
		{
			return null;
		}
		if (GenConstruct.FirstBlockingThing(blueprint, pawn) != null)
		{
			return GenConstruct.HandleBlockingThingJob(blueprint, pawn, forced);
		}
		if (!GenConstruct.CanConstruct(blueprint, pawn, def.workType, forced, JobDefOf.HaulToContainer))
		{
			return null;
		}
		if (def.workType != WorkTypeDefOf.Construction && WorkGiver_ConstructDeliverResources.ShouldRemoveExistingFloorFirst(pawn, blueprint))
		{
			return null;
		}
		Job job = RemoveExistingFloorJob(pawn, blueprint);
		if (job != null)
		{
			return job;
		}
		Job job2 = ResourceDeliverJobFor(pawn, blueprint, canRemoveExistingFloorUnderNearbyNeeders: true, forced);
		if (job2 != null)
		{
			return job2;
		}
		if (def.workType != WorkTypeDefOf.Hauling)
		{
			Job job3 = NoCostFrameMakeJobFor(blueprint);
			if (job3 != null)
			{
				return job3;
			}
		}
		return null;
	}

	private static bool CanDoRemoveExistingFloorWork(Pawn pawn, Blueprint blue)
	{
		if (!WorkGiver_ConstructDeliverResources.ShouldRemoveExistingFloorFirst(pawn, blue))
		{
			return false;
		}
		if (!pawn.CanReserve(blue.Position, 1, -1, ReservationLayerDefOf.Floor))
		{
			return false;
		}
		if (pawn.WorkTypeIsDisabled(WorkGiverDefOf.ConstructRemoveFloors.workType))
		{
			return false;
		}
		return true;
	}

	private Job NoCostFrameMakeJobFor(IConstructible c)
	{
		if (c is Blueprint_Install)
		{
			return null;
		}
		if (c is Blueprint && c.TotalMaterialCost().Count == 0)
		{
			Job job = JobMaker.MakeJob(JobDefOf.PlaceNoCostFrame);
			job.targetA = (Thing)c;
			return job;
		}
		return null;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '9.1.0.7988')

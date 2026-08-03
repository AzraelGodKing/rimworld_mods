using RimWorld;
using Verse;

namespace MultiFloors.Gravships;

[HotSwappable]
public class PlaceWorker_InGroundSubstructureFootprint : PlaceWorker
{
	public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (map.Level() <= 0)
		{
			return AcceptanceReport.op_Implicit(true);
		}
		Building_GravEngine val = GenCollection.FirstOrFallback<Building_GravEngine>(map.GroundMap().listerBuildings.AllBuildingsColonistOfClass<Building_GravEngine>(), (Building_GravEngine)null);
		if (val != null && val.ValidSubstructureAt(loc))
		{
			return AcceptanceReport.op_Implicit(true);
		}
		return AcceptanceReport.op_Implicit(Translator.Translate("MF_MustBeBuiltWithinGroundValidSubstructure"));
	}
}

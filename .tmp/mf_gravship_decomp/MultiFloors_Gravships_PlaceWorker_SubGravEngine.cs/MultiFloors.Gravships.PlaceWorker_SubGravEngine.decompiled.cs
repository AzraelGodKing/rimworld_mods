using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MultiFloors.Gravships;

[HotSwappable]
public class PlaceWorker_SubGravEngine : PlaceWorker
{
	private readonly HashSet<IntVec3> allowedCells = new HashSet<IntVec3>();

	public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		if (map.Level() <= 0 || map.LowerMap() == null)
		{
			return AcceptanceReport.op_Implicit(Translator.Translate("MF_OnlyBuildableOnUpperLevels"));
		}
		Map val = map.GroundMap();
		if (val == null)
		{
			return AcceptanceReport.op_Implicit(Translator.Translate("MF_OnlyBuildableOnUpperLevels"));
		}
		allowedCells.Clear();
		foreach (Building_GravEngine item in val.listerBuildings.AllBuildingsColonistOfClass<Building_GravEngine>())
		{
			allowedCells.Add(((Thing)item).Position);
		}
		if (!allowedCells.Contains(loc))
		{
			return AcceptanceReport.op_Implicit(Translator.Translate("MF_MustBeBuiltOnGroundGravEngineCell"));
		}
		return AcceptanceReport.op_Implicit(true);
	}

	public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		foreach (IntVec3 allowedCell in allowedCells)
		{
			IntVec3 current = allowedCell;
			GenDraw.DrawLineBetween(((IntVec3)(ref center)).ToVector3Shifted(), ((IntVec3)(ref current)).ToVector3(), (SimpleColor)0, 0.2f);
			GenDraw.DrawArrowPointingAt(((IntVec3)(ref current)).ToVector3(), false);
		}
	}
}

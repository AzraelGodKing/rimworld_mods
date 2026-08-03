using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MultiFloors.Gravships;

[HotSwappable]
public class Building_SubGravEngine : Building
{
	public Building_GravEngine Parent;

	public Building_GravEngine ParentEngine => Parent;

	public HashSet<IntVec3> ValidSubstructure => Parent.ValidSubstructure;

	public bool ValidSubstructureAt(IntVec3 cell)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return ValidSubstructure.Contains(cell);
	}

	public unsafe bool OnValidSubstructure(Thing thing)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if (!thing.Spawned)
		{
			return false;
		}
		BuildingProperties building = thing.def.building;
		if (building != null && building.isAttachment)
		{
			Thing wallAttachedTo = GenConstruct.GetWallAttachedTo(thing);
			return ValidSubstructureAt(wallAttachedTo.Position);
		}
		CellRect val = GenAdj.OccupiedRect(thing);
		Enumerator enumerator = ((CellRect)(ref val)).GetEnumerator();
		try
		{
			while (((Enumerator)(ref enumerator)).MoveNext())
			{
				IntVec3 current = ((Enumerator)(ref enumerator)).Current;
				if (!ValidSubstructureAt(current))
				{
					return false;
				}
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
		return true;
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		((Building)this).SpawnSetup(map, respawningAfterLoad);
		if (Parent != null || map.GroundMap() == null)
		{
			return;
		}
		Building_GravEngine val = null;
		foreach (Building_GravEngine item in map.GroundMap().listerBuildings.AllBuildingsColonistOfClass<Building_GravEngine>())
		{
			if (val == null)
			{
				val = item;
			}
			if (((Thing)item).Position == ((Thing)this).Position)
			{
				Parent = item;
				return;
			}
		}
		Parent = val;
	}

	public override void ExposeData()
	{
		((Building)this).ExposeData();
		Scribe_References.Look<Building_GravEngine>(ref Parent, "mf_baseGravshipEngine", false);
	}
}

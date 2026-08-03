using System;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

public class PlaceWorker_ABStairs : PlaceWorker
{
	public unsafe override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Invalid comparison between Unknown and I4
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		BuildableDef obj = ((checkingDef is ThingDef) ? checkingDef : null);
		ABStairsExtension aBStairsExtension = ((obj != null) ? ((Def)obj).GetModExtension<ABStairsExtension>() : null);
		if (aBStairsExtension == null)
		{
			return AcceptanceReport.op_Implicit(true);
		}
		int num = map.Level() + aBStairsExtension.deltaLevel;
		if (num < -1 || num > 1)
		{
			return new AcceptanceReport(TaggedString.op_Implicit(Translator.Translate("AB_LevelCap")));
		}
		if (num == 0 && map.Level() != 0)
		{
			Map val = map.GroundMap();
			if (val == null || val.Disposed)
			{
				return new AcceptanceReport(TaggedString.op_Implicit(Translator.Translate("AB_LevelCap")));
			}
			CellRect val2 = GenAdj.OccupiedRect(loc, rot, ((BuildableDef)(ThingDef)checkingDef).Size);
			Enumerator enumerator = ((CellRect)(ref val2)).GetEnumerator();
			try
			{
				while (((Enumerator)(ref enumerator)).MoveNext())
				{
					IntVec3 current = ((Enumerator)(ref enumerator)).Current;
					if (!GenGrid.InBounds(current, val))
					{
						return new AcceptanceReport(TaggedString.op_Implicit(Translator.Translate("AB_BlockedOnTarget")));
					}
					Building edifice = GridsUtility.GetEdifice(current, val);
					if (edifice != null && (int)((BuildableDef)((Thing)edifice).def).passability == 2)
					{
						return new AcceptanceReport(TaggedString.op_Implicit(Translator.Translate("AB_BlockedOnTarget")));
					}
					TerrainDef val3 = val.terrainGrid.TerrainAt(current);
					if (val3?.affordances == null || !val3.affordances.Contains(TerrainAffordanceDefOf.Medium))
					{
						return new AcceptanceReport(TaggedString.op_Implicit(Translator.Translate("AB_NoSupportOnTarget")));
					}
				}
			}
			finally
			{
				((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
			}
		}
		return AcceptanceReport.op_Implicit(true);
	}
}

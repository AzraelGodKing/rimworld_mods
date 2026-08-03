using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class Building_ABStairs : Building
{
	public const int BaseClimbTicks = 90;

	protected Building_ABStairs counterpart;

	protected Building_ABStairs counterpartSecond;

	private ABStairsExtension extCached;

	private bool extResolved;

	private static Texture removeIcon;

	public Building_ABStairs Counterpart => FilterLink(ref counterpart);

	public Building_ABStairs SecondCounterpart => FilterLink(ref counterpartSecond);

	public bool HasAnyLink => Counterpart != null || SecondCounterpart != null;

	public virtual bool AutoLinkOnSpawn => true;

	public ABStairsExtension Ext
	{
		get
		{
			if (!extResolved)
			{
				extResolved = true;
				extCached = ((Def)((Thing)this).def).GetModExtension<ABStairsExtension>();
			}
			return extCached;
		}
	}

	public int DeltaLevel => Ext?.deltaLevel ?? 0;

	private static Texture RemoveIcon
	{
		get
		{
			if ((Object)(object)removeIcon == (Object)null)
			{
				removeIcon = (Texture)(((object)((Command)(DesignatorUtility.FindAllowedDesignator<Designator_Deconstruct>()?)).icon) ?? ((object)BaseContent.BadTex));
			}
			return removeIcon;
		}
	}

	private static Building_ABStairs FilterLink(ref Building_ABStairs link)
	{
		if (link == null)
		{
			return null;
		}
		if (((Thing)link).Destroyed)
		{
			link = null;
			return null;
		}
		if (!((Thing)link).Spawned)
		{
			return null;
		}
		return link;
	}

	public Building_ABStairs CounterpartTowards(Map target)
	{
		Building_ABStairs building_ABStairs = Counterpart;
		if (building_ABStairs != null && ((Thing)building_ABStairs).Map == target)
		{
			return building_ABStairs;
		}
		building_ABStairs = SecondCounterpart;
		if (building_ABStairs != null && ((Thing)building_ABStairs).Map == target)
		{
			return building_ABStairs;
		}
		return null;
	}

	protected virtual void SetLink(int delta, Building_ABStairs other)
	{
		counterpart = other;
	}

	protected virtual Building_ABStairs GetLink(int delta)
	{
		return Counterpart;
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		((Building)this).SpawnSetup(map, respawningAfterLoad);
		map.Levels()?.RegisterStairs(this);
		if (!respawningAfterLoad && counterpart == null && AutoLinkOnSpawn)
		{
			LongEventHandler.ExecuteWhenFinished((Action)TryEstablishLink);
		}
	}

	public override void DeSpawn(DestroyMode mode = (DestroyMode)0)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		Building_ABStairs building_ABStairs = counterpart;
		Building_ABStairs building_ABStairs2 = counterpartSecond;
		SeverLink();
		Map map = ((Thing)this).Map;
		((Building)this).DeSpawn(mode);
		map?.Levels()?.DeregisterStairs(this);
		if (building_ABStairs != null && !((Thing)building_ABStairs).Destroyed && ((Thing)building_ABStairs).Spawned)
		{
			((Thing)building_ABStairs).Destroy((DestroyMode)0);
		}
		if (building_ABStairs2 != null && !((Thing)building_ABStairs2).Destroyed && ((Thing)building_ABStairs2).Spawned)
		{
			((Thing)building_ABStairs2).Destroy((DestroyMode)0);
		}
	}

	public int ClimbTicksFor(Pawn p)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		float num = Ext?.climbFactor ?? 1f;
		QualityCategory val = default(QualityCategory);
		if (QualityUtility.TryGetQuality((Thing)(object)this, ref val))
		{
			num *= Mathf.Lerp(1.15f, 0.75f, (float)val / 6f);
		}
		float num2 = ABMod.Settings?.climbTimeMultiplier ?? 1f;
		return Mathf.Max(1, Mathf.RoundToInt(90f * num * num2));
	}

	private void TryEstablishLink()
	{
		EstablishLink(DeltaLevel);
	}

	protected void EstablishLink(int delta)
	{
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		if (((Thing)this).Destroyed || !((Thing)this).Spawned || delta == 0 || GetLink(delta) != null || !ABGuard.On(ABGuard.LevelGen))
		{
			return;
		}
		try
		{
			int num = ((Thing)this).Map.Level() + delta;
			Map val = null;
			if (num == 0)
			{
				val = ((Thing)this).Map.GroundMap();
			}
			else if (num >= -1 && num <= 1)
			{
				MapGeneratorDef generatorDef = ((num == -1) ? ABDefOf.AB_Basement : ABDefOf.AB_Sky);
				val = LevelMapGen.GetOrGenerate(((Thing)this).Map, num, generatorDef, out var generated);
				if (generated && val != null)
				{
					string text = ((num == -1) ? "AB_BasementCreated" : "AB_SkyCreated");
					Messages.Message(TaggedString.op_Implicit(Translator.Translate(text)), LookTargets.op_Implicit(new TargetInfo(((Thing)this).Position, ((Thing)this).Map, false)), MessageTypeDefOf.PositiveEvent, true);
				}
			}
			if (val == null || val == ((Thing)this).Map)
			{
				Log.Warning("[As above, So below] Stairs at " + ((object)((Thing)this).Position/*cast due to .constrained prefix*/).ToString() + " could not resolve a target level map.");
			}
			else
			{
				SpawnCounterpartOn(val, delta);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.LevelGen, e, "stair link creation");
		}
	}

	private unsafe void SpawnCounterpartOn(Map targetMap, int delta)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Invalid comparison between Unknown and I4
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		ThingDef val = Ext?.counterpartDef;
		if (val == null)
		{
			Log.Error("[As above, So below] Missing counterpartDef on " + ((Def)((Thing)this).def).defName);
			return;
		}
		IntVec3 position = ((Thing)this).Position;
		CellRect footprint = GenAdj.OccupiedRect(position, ((Thing)this).Rotation, ((BuildableDef)((Thing)this).def).Size);
		PrepareLanding(targetMap, footprint);
		Building val2 = null;
		CellRect val3 = ((CellRect)(ref footprint)).ClipInsideMap(targetMap);
		Enumerator enumerator = ((CellRect)(ref val3)).GetEnumerator();
		try
		{
			while (((Enumerator)(ref enumerator)).MoveNext())
			{
				IntVec3 current = ((Enumerator)(ref enumerator)).Current;
				Building edifice = GridsUtility.GetEdifice(current, targetMap);
				if (edifice != null && (int)((BuildableDef)((Thing)edifice).def).passability == 2 && !(edifice is Building_ABStairs))
				{
					val2 = edifice;
					break;
				}
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
		if (val2 != null)
		{
			Messages.Message(TaggedString.op_Implicit(Translator.Translate("AB_LinkBlocked")), LookTargets.op_Implicit(new TargetInfo(((Thing)this).Position, ((Thing)this).Map, false)), MessageTypeDefOf.RejectInput, true);
			return;
		}
		Building_ABStairs building_ABStairs = (Building_ABStairs)(object)ThingMaker.MakeThing(val, ((Thing)this).Stuff);
		((Thing)building_ABStairs).SetFaction(Faction.OfPlayer, (Pawn)null);
		building_ABStairs.SetLink(-delta, this);
		SetLink(delta, building_ABStairs);
		GenSpawn.Spawn((Thing)(object)building_ABStairs, position, targetMap, ((Thing)this).Rotation, (WipeMode)0, false, false);
		ABLog.Dev("Linked stairs " + ((Thing)this).ThingID + " (level " + ((Thing)this).Map.Level() + ") with " + ((Thing)building_ABStairs).ThingID + " (level " + targetMap.Level() + ").");
	}

	private unsafe static void PrepareLanding(Map targetMap, CellRect footprint)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		int num = targetMap.Level();
		CellRect val = ((CellRect)(ref footprint)).ExpandedBy(1);
		val = ((CellRect)(ref val)).ClipInsideMap(targetMap);
		Enumerator enumerator = ((CellRect)(ref val)).GetEnumerator();
		try
		{
			while (((Enumerator)(ref enumerator)).MoveNext())
			{
				IntVec3 current = ((Enumerator)(ref enumerator)).Current;
				if (num != 0)
				{
					List<Thing> list = GridsUtility.GetThingList(current, targetMap).ToList();
					for (int i = 0; i < list.Count; i++)
					{
						Thing val2 = list[i];
						if (val2.def.mineable && val2.def.destroyable)
						{
							val2.Destroy((DestroyMode)0);
						}
					}
				}
				if (num > 0 && targetMap.terrainGrid.TerrainAt(current) == ABDefOf.AB_OpenAir)
				{
					targetMap.terrainGrid.SetTerrain(current, ABDefOf.AB_RoofSurface);
				}
				targetMap.fogGrid.Unfog(current);
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
	}

	internal void SeverLink()
	{
		Sever(ref counterpart);
		Sever(ref counterpartSecond);
	}

	private void Sever(ref Building_ABStairs link)
	{
		Building_ABStairs building_ABStairs = link;
		link = null;
		if (building_ABStairs != null && !((Thing)building_ABStairs).Destroyed)
		{
			building_ABStairs.Unlink(this);
		}
	}

	internal void Unlink(Building_ABStairs other)
	{
		if (counterpart == other)
		{
			counterpart = null;
		}
		if (counterpartSecond == other)
		{
			counterpartSecond = null;
		}
	}

	public override void Destroy(DestroyMode mode = (DestroyMode)0)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		Building_ABStairs building_ABStairs = counterpart;
		Building_ABStairs building_ABStairs2 = counterpartSecond;
		counterpart = null;
		counterpartSecond = null;
		((Building)this).Destroy(mode);
		if (building_ABStairs != null && !((Thing)building_ABStairs).Destroyed)
		{
			building_ABStairs.Unlink(this);
			((Thing)building_ABStairs).Destroy((DestroyMode)0);
		}
		if (building_ABStairs2 != null && !((Thing)building_ABStairs2).Destroyed)
		{
			building_ABStairs2.Unlink(this);
			((Thing)building_ABStairs2).Destroy((DestroyMode)0);
		}
	}

	public override void ExposeData()
	{
		((Building)this).ExposeData();
		Scribe_References.Look<Building_ABStairs>(ref counterpart, "AB_counterpart", false);
		Scribe_References.Look<Building_ABStairs>(ref counterpartSecond, "AB_counterpartSecond", false);
	}

	public override string GetInspectString()
	{
		string inspectString = ((ThingWithComps)this).GetInspectString();
		string text = LinkLine();
		return GenText.NullOrEmpty(inspectString) ? text : (inspectString + "\n" + text);
	}

	protected virtual string LinkLine()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (Counterpart != null)
		{
			return TaggedString.op_Implicit(Translator.Translate((DeltaLevel > 0) ? "AB_LinkedAbove" : "AB_LinkedBelow"));
		}
		return TaggedString.op_Implicit(Translator.Translate("AB_NotLinkedLine"));
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo item in _003C_003En__0())
		{
			yield return item;
		}
		List<Gizmo> extras = null;
		try
		{
			extras = BuildGizmos();
		}
		catch (Exception ex)
		{
			Log.WarningOnce("[As above, So below] Stairs gizmos failed: " + ex, 762195844);
		}
		if (extras != null)
		{
			for (int i = 0; i < extras.Count; i++)
			{
				yield return extras[i];
			}
		}
	}

	protected virtual List<Gizmo> BuildGizmos()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected O, but got Unknown
		List<Gizmo> list = new List<Gizmo>();
		if (((Thing)this).Faction == Faction.OfPlayer)
		{
			bool flag = ((Thing)this).Map.designationManager.DesignationOn((Thing)(object)this, DesignationDefOf.Deconstruct) != null;
			Command_Action val = new Command_Action
			{
				defaultLabel = TaggedString.op_Implicit(Translator.Translate("AB_RemoveLink")),
				defaultDesc = TaggedString.op_Implicit(Translator.Translate("AB_RemoveLinkDesc")),
				icon = RemoveIcon,
				action = delegate
				{
					//IL_0017: Unknown result type (might be due to invalid IL or missing references)
					//IL_0022: Unknown result type (might be due to invalid IL or missing references)
					//IL_002c: Expected O, but got Unknown
					((Thing)this).Map.designationManager.AddDesignation(new Designation(LocalTargetInfo.op_Implicit((Thing)(object)this), DesignationDefOf.Deconstruct, (ColorDef)null));
				}
			};
			if (flag)
			{
				((Gizmo)val).Disable(TaggedString.op_Implicit(Translator.Translate("AB_RemoveQueued")));
			}
			list.Add((Gizmo)(object)val);
		}
		Building_ABStairs cp = Counterpart;
		if (cp != null)
		{
			list.Add((Gizmo)new Command_Action
			{
				defaultLabel = TaggedString.op_Implicit(Translator.Translate((DeltaLevel > 0) ? "AB_ViewAbove" : "AB_ViewBelow")),
				defaultDesc = TaggedString.op_Implicit(Translator.Translate("AB_ViewOtherLevelDesc")),
				icon = (Texture)(object)((BuildableDef)((Thing)this).def).uiIcon,
				action = delegate
				{
					LevelCamera.JumpPreservingView(((Thing)cp).Map);
				}
			});
		}
		return list;
	}

	public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
	{
		foreach (FloatMenuOption item in _003C_003En__1(selPawn))
		{
			yield return item;
		}
		List<FloatMenuOption> extras = null;
		try
		{
			extras = BuildUseOptions(selPawn);
		}
		catch (Exception ex)
		{
			Log.WarningOnce("[As above, So below] Stairs float menu failed: " + ex, 762195843);
		}
		if (extras != null)
		{
			for (int i = 0; i < extras.Count; i++)
			{
				yield return extras[i];
			}
		}
	}

	protected static bool CanBeOrderedToUse(Pawn selPawn)
	{
		if (selPawn.RaceProps.Humanlike)
		{
			return true;
		}
		return ((Thing)selPawn).Faction == Faction.OfPlayer && selPawn.RaceProps.ToolUser;
	}

	protected virtual List<FloatMenuOption> BuildUseOptions(Pawn selPawn)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected O, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Expected O, but got Unknown
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		if (!CanBeOrderedToUse(selPawn))
		{
			return list;
		}
		string text = TaggedString.op_Implicit(Translator.Translate((DeltaLevel > 0) ? "AB_GoUp" : "AB_GoDown"));
		Building_ABStairs cp = Counterpart;
		if (cp == null)
		{
			list.Add(new FloatMenuOption(TaggedString.op_Implicit(text + " (" + Translator.Translate("AB_NotLinkedShort") + ")"), (Action)null, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
		}
		else if (!ReachabilityUtility.CanReach(selPawn, LocalTargetInfo.op_Implicit((Thing)(object)this), (PathEndMode)2, (Danger)3, false, false, (TraverseMode)0))
		{
			list.Add(new FloatMenuOption(TaggedString.op_Implicit(text + " (" + Translator.Translate("AB_NoPath") + ")"), (Action)null, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
		}
		else
		{
			list.Add(new FloatMenuOption(text, (Action)delegate
			{
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				//IL_001e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0023: Unknown result type (might be due to invalid IL or missing references)
				Job val = JobMaker.MakeJob(ABDefOf.AB_UseStairs, LocalTargetInfo.op_Implicit((Thing)(object)this));
				val.targetC = LocalTargetInfo.op_Implicit((Thing)(object)cp);
				selPawn.jobs.TryTakeOrderedJob(val, (JobTag?)(JobTag)0, false);
			}, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
		}
		return list;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private IEnumerable<Gizmo> _003C_003En__0()
	{
		return ((Building)this).GetGizmos();
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private IEnumerable<FloatMenuOption> _003C_003En__1(Pawn selPawn)
	{
		return ((ThingWithComps)this).GetFloatMenuOptions(selPawn);
	}
}

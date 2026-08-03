using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public static class ABDevTools
{
	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void EnsureLevels()
	{
		Map val = Find.CurrentMap?.GroundMap();
		if (val == null)
		{
			Messages.Message("AB dev: no ground map for this column.", MessageTypeDefOf.RejectInput, false);
			return;
		}
		bool generated;
		if (val.Levels()?.upperMap == null)
		{
			LevelMapGen.GetOrGenerate(val, 1, ABDefOf.AB_Sky, out generated);
		}
		if (val.Levels()?.lowerMap == null)
		{
			LevelMapGen.GetOrGenerate(val, -1, ABDefOf.AB_Basement, out generated);
		}
		Messages.Message("AB dev: ensured sky + basement for this column.", MessageTypeDefOf.TaskCompletion, false);
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private unsafe static void SelfTestCrossGapCombat()
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false, "no ground map");
				Report("cross-gap combat self-test", sb, pass, fail);
				return;
			}
			bool generated;
			Map val2 = val.Levels()?.upperMap ?? LevelMapGen.GetOrGenerate(val, 1, ABDefOf.AB_Sky, out generated);
			Check("sky level exists", val2 != null);
			if (val2 == null)
			{
				Report("cross-gap combat self-test", sb, pass, fail);
				return;
			}
			IntVec3 val3 = FindOpenBaseCell(val);
			IntVec3 val4 = val3 + IntVec3.East;
			if (!GenGrid.InBounds(val4, val2))
			{
				val4 = val3 + IntVec3.West;
			}
			if (val.roofGrid.Roofed(val3))
			{
				val.roofGrid.SetRoof(val3, (RoofDef)null);
			}
			ClearCell(val, val3);
			ClearCell(val2, val3);
			val2.terrainGrid.SetTerrain(val3, ABDefOf.AB_OpenAir);
			MakePlatform(val2, val, val4);
			Check("target column is open air on the sky", val2.terrainGrid.TerrainAt(val3) == ABDefOf.AB_OpenAir);
			bool cond = GenGrid.Standable(val4, val2);
			IntVec3 val5 = val4;
			Check("shooter cell is standable on the sky", cond, "s=" + ((object)(*(IntVec3*)(&val5))/*cast due to .constrained prefix*/).ToString());
			Pawn val6 = SpawnHostile(val, val3);
			Check("hostile spawned on surface", val6 != null && ((Thing)val6).Spawned && ((Thing)val6).Map == val);
			Pawn val7 = SpawnArmedColonist(val2, val4);
			Check("armed colonist spawned on sky", val7 != null && ((Thing)val7).Spawned && ((Thing)val7).Map == val2);
			if (val6 == null || val7 == null)
			{
				Report("cross-gap combat self-test", sb, pass, fail);
				return;
			}
			Verb_LaunchProjectile rangedVerb = CrossLevelCombat.GetRangedVerb(val7);
			Check("colonist has a ranged projectile verb", rangedVerb != null);
			Check("maps are a sky<->surface pair", CrossLevelCombat.AreCrossGapPaired(val2, val, out var _, out var _));
			CrossLevelCombat.GapShot shot = default(CrossLevelCombat.GapShot);
			bool flag = rangedVerb != null && CrossLevelCombat.CanCrossGapFire((Thing)(object)val7, (Thing)(object)val6, rangedVerb, out shot);
			Check("CanCrossGapFire from the sky at the surface target", flag);
			if (flag)
			{
				Check("resolved shot lands on the surface map", shot.targetMap == val, "map=" + shot.targetMap?.uniqueID);
				float num = CrossLevelCombat.ComputeAimChance((Thing)(object)val7, rangedVerb, (Thing)(object)val6, shot.distance);
				Check("aim chance is a sane probability", num > 0f && num <= 1f, "aim=" + num.ToString("0.000") + " dist=" + shot.distance.ToString("0.0"));
			}
			int count = val.listerThings.ThingsInGroup((ThingRequestGroup)60).Count;
			int num2 = 0;
			if (rangedVerb != null)
			{
				for (int i = 0; i < 12; i++)
				{
					if (CrossLevelCombat.Fire((Thing)(object)val7, rangedVerb, (Thing)(object)val6))
					{
						num2++;
					}
				}
			}
			int count2 = val.listerThings.ThingsInGroup((ThingRequestGroup)60).Count;
			Check("Fire() reported casts", num2 > 0, "fired=" + num2);
			Check("projectiles now live on the surface map", count2 > count, "before=" + count + " after=" + count2);
			bool cond2 = TestReverseEnclosed(val, val2, val3);
			Check("surface->sky is blocked for a fully enclosed sky cell", cond2);
			bool cond3 = CrossLevelCombat.TryStartCrossGapAttack(val7, (Thing)(object)val6);
			Check("sustained cross-gap attack job started", cond3);
			Find.Selector.ClearSelection();
			Find.Selector.Select((object)val7, false, true);
			Messages.Message("AB dev: cross-gap demo armed. View the SKY level to watch the colonist plunge-fire the raider below.", LookTargets.op_Implicit((Thing)(object)val7), MessageTypeDefOf.NeutralEvent, false);
		}
		catch (Exception ex)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex);
		}
		Report("cross-gap combat self-test", sb, pass, fail);
		void Check(string name, bool flag2, string detail = "")
		{
			if (flag2)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void SelfTestAutoEngage()
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false, "no ground map");
				Report("auto-engage self-test", sb, pass, fail);
				return;
			}
			bool generated;
			Map val2 = val.Levels()?.upperMap ?? LevelMapGen.GetOrGenerate(val, 1, ABDefOf.AB_Sky, out generated);
			Check("sky level exists", val2 != null);
			if (val2 == null)
			{
				Report("auto-engage self-test", sb, pass, fail);
				return;
			}
			IntVec3 val3 = FindOpenBaseCell(val);
			IntVec3 val4 = val3 + IntVec3.North;
			IntVec3 val5 = val3 + IntVec3.East;
			if (!GenGrid.InBounds(val4, val) || !GenGrid.InBounds(val5, val2))
			{
				Check("arena cells in bounds", cond: false);
				Report("auto-engage self-test", sb, pass, fail);
				return;
			}
			IntVec3[] array = (IntVec3[])(object)new IntVec3[2] { val3, val4 };
			foreach (IntVec3 val6 in array)
			{
				if (val.roofGrid.Roofed(val6))
				{
					val.roofGrid.SetRoof(val6, (RoofDef)null);
				}
				ClearCell(val, val6);
				ClearCell(val2, val6);
				val2.terrainGrid.SetTerrain(val6, ABDefOf.AB_OpenAir);
			}
			MakePlatform(val2, val, val5);
			Pawn val7 = SpawnHostile(val, val4);
			Check("hostile spawned on surface", val7 != null && ((Thing)val7).Spawned);
			if (val7 == null)
			{
				Report("auto-engage self-test", sb, pass, fail);
				return;
			}
			ArmWithRanged(val7);
			Check("hostile has a ranged verb", CrossLevelCombat.GetRangedVerb(val7) != null);
			Pawn val8 = SpawnArmedColonist(val2, val5);
			Check("colonist spawned on sky", val8 != null && ((Thing)val8).Spawned);
			if (val8 == null)
			{
				Report("auto-engage self-test", sb, pass, fail);
				return;
			}
			val8.drafter.Drafted = false;
			CrossLevelAutoEngage.ScanPair(val2, val);
			Check("hostile auto-engaged up through the gap", val7.CurJobDef == ABDefOf.AB_CrossLevelAttack, "job=" + (((Def)(val7.CurJobDef?)).defName ?? "null"));
			val8.drafter.Drafted = true;
			Pawn_JobTracker jobs = val8.jobs;
			if (jobs != null)
			{
				jobs.StopAll(false, true);
			}
			CrossLevelAutoEngage.ScanPair(val2, val);
			Check("drafted colonist returned fire on their own", val8.CurJobDef == ABDefOf.AB_CrossLevelAttack, "job=" + (((Def)(val8.CurJobDef?)).defName ?? "null"));
			Messages.Message("AB dev: auto-engage demo armed - watch the firefight through the hole.", LookTargets.op_Implicit((Thing)(object)val7), MessageTypeDefOf.NeutralEvent, false);
		}
		catch (Exception ex)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex);
		}
		Report("auto-engage self-test", sb, pass, fail);
		void Check(string name, bool cond, string detail = "")
		{
			if (cond)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void SelfTestTargetingHub()
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected O, but got Unknown
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Expected O, but got Unknown
		//IL_052a: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false);
				Report("targeting-hub self-test", sb, pass, fail);
				return;
			}
			bool generated;
			Map val2 = val.Levels()?.upperMap ?? LevelMapGen.GetOrGenerate(val, 1, ABDefOf.AB_Sky, out generated);
			Check("sky level exists", val2 != null);
			if (val2 == null)
			{
				Report("targeting-hub self-test", sb, pass, fail);
				return;
			}
			IntVec3 val3 = FindOpenBaseCell(val);
			IntVec3 val4 = val3 + IntVec3.East;
			MakePlatform(val2, val, val4);
			ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail("Turret_Mortar");
			Check("vanilla mortar def found", namedSilentFail != null);
			Building_Turret val5 = null;
			if (namedSilentFail != null)
			{
				val5 = (Building_Turret)GenSpawn.Spawn(ThingMaker.MakeThing(namedSilentFail, ThingDefOf.Steel), val4, val2, (WipeMode)0);
				((Thing)val5).SetFaction(Faction.OfPlayer, (Pawn)null);
			}
			Verb_LaunchProjectile val6 = CrossLevelTurret.LauncherVerb(val5);
			Check("mortar verb is an arc (flyOverhead) verb", val6 != null && CrossLevelTurret.IsArc(val6));
			IntVec3 val7 = IntVec3.Invalid;
			if (val6 != null)
			{
				foreach (IntVec3 item in GenRadial.RadialCellsAround(val4, 54f, false))
				{
					IntVec3 val8 = item - val4;
					if ((float)((IntVec3)(ref val8)).LengthHorizontalSquared < 1024f || !GenGrid.InBounds(item, val2) || !GenGrid.InBounds(item, val) || val2.terrainGrid.TerrainAt(item) != ABDefOf.AB_OpenAir)
					{
						continue;
					}
					val7 = item;
					break;
				}
			}
			Check("found a far open-air target column", ((IntVec3)(ref val7)).IsValid);
			CrossLevelCombat.GapShot shot;
			LocalTargetInfo target;
			Map targetMap;
			if (val6 != null && ((IntVec3)(ref val7)).IsValid)
			{
				Check("sky mortar can arc-fire at the surface cell", CrossLevelCombat.CanArcFireAt(val2, val4, val7, val, val6, out shot));
				IntVec3 tCol = val3;
				Check("arc fire respects min range (near cell rejected)", !CrossLevelCombat.CanArcFireAt(val2, val4, tCol, val, val6, out shot));
				Check("bombardment order stored", CrossLevelTurret.TryOrder(val5, LocalTargetInfo.op_Implicit(val7), val) && CrossLevelTurret.HasOrder(val5, out target, out targetMap));
			}
			IntVec3 val9 = val3 + IntVec3.North;
			MakePlatform(val2, val, val9);
			if (val.roofGrid.Roofed(val3))
			{
				val.roofGrid.SetRoof(val3, (RoofDef)null);
			}
			ClearCell(val, val3);
			ClearCell(val2, val3);
			val2.terrainGrid.SetTerrain(val3, ABDefOf.AB_OpenAir);
			ThingDef namedSilentFail2 = DefDatabase<ThingDef>.GetNamedSilentFail("Turret_MiniTurret");
			Check("mini-turret def found", namedSilentFail2 != null);
			Pawn val10 = SpawnHostile(val, val3);
			Check("hostile under the hole", val10 != null && ((Thing)val10).Spawned);
			if (namedSilentFail2 != null && val10 != null)
			{
				Building_Turret val11 = (Building_Turret)GenSpawn.Spawn(ThingMaker.MakeThing(namedSilentFail2, ThingDefOf.Steel), val9, val2, (WipeMode)0);
				((Thing)val11).SetFaction(Faction.OfPlayer, (Pawn)null);
				CompPowerTrader val12 = ThingCompUtility.TryGetComp<CompPowerTrader>((Thing)(object)val11);
				if (val12 != null)
				{
					val12.PowerOn = true;
				}
				Verb_LaunchProjectile val13 = CrossLevelTurret.LauncherVerb(val11);
				Check("mini-turret has a direct projectile verb", val13 != null && !CrossLevelTurret.IsArc(val13));
				if (val13 != null)
				{
					Check("direct turret has a gap line to the hostile", CrossLevelTurret.TurretCanFire(val11, (Thing)(object)val10, val13, out shot));
					CrossLevelTurret.AcquireAuto(val2, val);
					Check("direct turret auto-acquired the hostile below", CrossLevelTurret.HasOrder(val11, out target, out targetMap));
					int count = val.listerThings.ThingsInGroup((ThingRequestGroup)60).Count;
					int ticksGame = Find.TickManager.TicksGame;
					for (int i = 0; i < 300; i++)
					{
						CrossLevelTurret.TickPair(val2, val, ticksGame + i);
					}
					int count2 = val.listerThings.ThingsInGroup((ThingRequestGroup)60).Count;
					Check("tick driver put turret projectiles on the surface", count2 > count, "before=" + count + " after=" + count2);
				}
			}
			TargetingParameters val14 = TargetingParameters.ForAttackAny();
			Pawn val15 = ((val.mapPawns.AllPawnsSpawned.Count > 0) ? val.mapPawns.AllPawnsSpawned[0] : null);
			if (val15 != null)
			{
				Check("targetParams.CanTarget accepts a cross-map pawn TargetInfo", val14.CanTarget(new TargetInfo((Thing)(object)val15), (ITargetingSource)null));
			}
		}
		catch (Exception ex)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex);
		}
		Report("targeting-hub self-test", sb, pass, fail);
		void Check(string name, bool cond, string detail = "")
		{
			if (cond)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void SelfTestAnimalWander()
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false);
				Report("animal-wander self-test", sb, pass, fail);
				return;
			}
			bool generated;
			Map val2 = val.Levels()?.lowerMap ?? LevelMapGen.GetOrGenerate(val, -1, ABDefOf.AB_Basement, out generated);
			Check("basement exists", val2 != null);
			if (val2 == null)
			{
				Report("animal-wander self-test", sb, pass, fail);
				return;
			}
			IntVec3 val3 = FindOpenBaseCell(val);
			ClearCell(val, val3);
			Building_ABStairs building_ABStairs = null;
			ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail("AB_StairsDown");
			if (namedSilentFail != null)
			{
				building_ABStairs = GenSpawn.Spawn(ThingMaker.MakeThing(namedSilentFail, ThingDefOf.WoodLog), val3, val, (WipeMode)0) as Building_ABStairs;
				if (building_ABStairs != null)
				{
					((Thing)building_ABStairs).SetFaction(Faction.OfPlayer, (Pawn)null);
				}
			}
			bool flag = building_ABStairs != null && building_ABStairs.CounterpartTowards(val2) != null;
			Check("spawned stairs linked to the basement", flag, (building_ABStairs == null) ? "no stairs" : "no counterpart");
			PawnKindDef val4 = DefDatabase<PawnKindDef>.GetNamedSilentFail("Hare") ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Squirrel") ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Rat");
			Check("wild animal kind found", val4 != null);
			if (val4 == null || !flag)
			{
				Report("animal-wander self-test", sb, pass, fail);
				return;
			}
			Pawn val5 = PawnGenerator.GeneratePawn(val4, (Faction)null, (PlanetTile?)null);
			GenSpawn.Spawn((Thing)(object)val5, CellFinder.StandableCellNear(val3, val, 6f, (Predicate<IntVec3>)null), val, (WipeMode)0);
			Check("wild wanderer spawned on surface", ((Thing)val5).Spawned && ((Thing)val5).Faction == null);
			Check("ambient descent starts the stairs job", CrossLevelAnimals.TryDescend(val5, val2) && val5.CurJobDef == ABDefOf.AB_UseStairs, "job=" + (((Def)(val5.CurJobDef?)).defName ?? "null"));
			Building_ABStairs building_ABStairs2 = building_ABStairs.CounterpartTowards(val2);
			IntVec3 val6 = CellFinder.StandableCellNear(((Thing)building_ABStairs2).Position, val2, 6f, (Predicate<IntVec3>)null);
			Check("standable basement cell near the counterpart", ((IntVec3)(ref val6)).IsValid);
			if (((IntVec3)(ref val6)).IsValid)
			{
				Pawn val7 = PawnGenerator.GeneratePawn(val4, (Faction)null, (PlanetTile?)null);
				GenSpawn.Spawn((Thing)(object)val7, val6, val2, (WipeMode)0);
				if (val7.needs?.food != null)
				{
					((Need)val7.needs.food).CurLevel = 0.01f;
				}
				Pawn val8 = PawnGenerator.GeneratePawn(val4, (Faction)null, (PlanetTile?)null);
				IntVec3 val9 = CellFinder.StandableCellNear(((Thing)building_ABStairs2).Position, val2, 8f, (Predicate<IntVec3>)null);
				GenSpawn.Spawn((Thing)(object)val8, ((IntVec3)(ref val9)).IsValid ? val9 : val6, val2, (WipeMode)0);
				if (val8.needs?.food != null)
				{
					((Need)val8.needs.food).CurLevel = ((Need)val8.needs.food).MaxLevel;
				}
				LevelComp comp = val2.Levels();
				HostileDescend.ScanPocketMap(comp);
				Check("hungry basement animal heads for the stairs", val7.CurJobDef == ABDefOf.AB_UseStairs, "job=" + (((Def)(val7.CurJobDef?)).defName ?? "null"));
				Check("content fresh visitor lingers (no stairs job)", val8.CurJobDef != ABDefOf.AB_UseStairs, "job=" + (((Def)(val8.CurJobDef?)).defName ?? "null"));
			}
			Messages.Message("AB dev: animal-wander demo armed - unpause and watch the stairwell.", LookTargets.op_Implicit((Thing)(object)val5), MessageTypeDefOf.NeutralEvent, false);
		}
		catch (Exception ex)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex);
		}
		Report("animal-wander self-test", sb, pass, fail);
		void Check(string name, bool cond, string detail = "")
		{
			if (cond)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void SelfTestStairRouting()
	{
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_041c: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false);
				Report("stair-routing self-test", sb, pass, fail);
				return;
			}
			bool generated;
			Map val2 = val.Levels()?.lowerMap ?? LevelMapGen.GetOrGenerate(val, -1, ABDefOf.AB_Basement, out generated);
			Check("basement exists", val2 != null);
			if (val2 == null)
			{
				Report("stair-routing self-test", sb, pass, fail);
				return;
			}
			ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail("AB_StairsDown");
			Check("stairs def exists", namedSilentFail != null);
			if (namedSilentFail == null)
			{
				Report("stair-routing self-test", sb, pass, fail);
				return;
			}
			IntVec3 val3 = FindOpenBaseCell(val);
			IntVec3 val4 = default(IntVec3);
			((IntVec3)(ref val4))._002Ector(Mathf.Clamp(val3.x + 40, 6, val.Size.x - 6), 0, val3.z);
			ClearCell(val, val3);
			ClearCell(val, val4);
			Building_ABStairs building_ABStairs = GenSpawn.Spawn(ThingMaker.MakeThing(namedSilentFail, ThingDefOf.WoodLog), val3, val, (WipeMode)0) as Building_ABStairs;
			if (building_ABStairs != null)
			{
				((Thing)building_ABStairs).SetFaction(Faction.OfPlayer, (Pawn)null);
			}
			Building_ABStairs building_ABStairs2 = GenSpawn.Spawn(ThingMaker.MakeThing(namedSilentFail, ThingDefOf.WoodLog), val4, val, (WipeMode)0) as Building_ABStairs;
			if (building_ABStairs2 != null)
			{
				((Thing)building_ABStairs2).SetFaction(Faction.OfPlayer, (Pawn)null);
			}
			Building_ABStairs building_ABStairs3 = building_ABStairs?.CounterpartTowards(val2);
			Building_ABStairs building_ABStairs4 = building_ABStairs2?.CounterpartTowards(val2);
			Check("both stairwells linked to the basement", building_ABStairs3 != null && building_ABStairs4 != null, ((building_ABStairs3 == null) ? "A unlinked " : "") + ((building_ABStairs4 == null) ? "B unlinked" : ""));
			if (building_ABStairs3 == null || building_ABStairs4 == null)
			{
				Report("stair-routing self-test", sb, pass, fail);
				return;
			}
			IntVec3 val5 = default(IntVec3);
			((IntVec3)(ref val5))._002Ector((val3.x * 3 + val4.x * 2) / 5, 0, val3.z);
			IntVec3 val6 = CellFinder.StandableCellNear(val5, val, 6f, (Predicate<IntVec3>)null);
			Pawn val7 = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, (PlanetTile?)null);
			GenSpawn.Spawn((Thing)(object)val7, ((IntVec3)(ref val6)).IsValid ? val6 : val5, val, (WipeMode)0);
			int cond;
			if (((Thing)val7).Spawned)
			{
				IntVec3 val8 = ((Thing)val7).Position - val3;
				int lengthHorizontalSquared = ((IntVec3)(ref val8)).LengthHorizontalSquared;
				val8 = ((Thing)val7).Position - val4;
				cond = ((lengthHorizontalSquared < ((IntVec3)(ref val8)).LengthHorizontalSquared) ? 1 : 0);
			}
			else
			{
				cond = 0;
			}
			Check("walker spawned nearer stairwell A", (byte)cond != 0);
			IntVec3 dest = CellFinder.StandableCellNear(((Thing)building_ABStairs4).Position, val2, 4f, (Predicate<IntVec3>)null);
			Check("destination cell near B's landing", ((IntVec3)(ref dest)).IsValid);
			Building_ABStairs building_ABStairs5 = CrossLevelWork.NearestUsableStairs(val7, val2, checkReachability: true);
			Check("legacy pick is the pawn-nearest stairwell (A)", building_ABStairs5 == building_ABStairs, "picked " + (((building_ABStairs5 != null) ? ((Thing)building_ABStairs5).ThingID : null) ?? "null"));
			Building_ABStairs stairs;
			Building_ABStairs exit;
			bool flag = StairRouter.TryBestToward(val7, val2, dest, out stairs, out exit);
			Check("router picks the destination-side stairwell (B)", flag && stairs == building_ABStairs2 && exit == building_ABStairs4, "picked " + (((stairs != null) ? ((Thing)stairs).ThingID : null) ?? "null"));
			Building_ABStairs stairs2 = building_ABStairs5;
			Building_ABStairs exit2 = building_ABStairs5?.CounterpartTowards(val2);
			StairRouter.Reroute(val7, val2, dest, ref stairs2, ref exit2);
			Check("reroute upgrades a legacy pick to B", stairs2 == building_ABStairs2 && exit2 == building_ABStairs4);
			Job job;
			bool flag2 = CrossLevelWork.TryStairsJobToward(val7, val2, dest, out job);
			Check("dest-aware stairs job targets B", flag2 && (object)((job != null) ? ((LocalTargetInfo)(ref job.targetA)).Thing : null) == building_ABStairs2);
			if (flag2)
			{
				Pawn_JobTracker jobs = val7.jobs;
				if (jobs != null)
				{
					jobs.StartJob(job, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
				}
				Check("walker takes the stairs job", val7.CurJobDef == ABDefOf.AB_UseStairs, "job=" + (((Def)(val7.CurJobDef?)).defName ?? "null"));
			}
			Messages.Message("AB dev: stair-routing demo armed - unpause and watch the walker head for the far stairwell.", LookTargets.op_Implicit((Thing)(object)val7), MessageTypeDefOf.NeutralEvent, false);
		}
		catch (Exception ex)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex);
		}
		Report("stair-routing self-test", sb, pass, fail);
		void Check(string name, bool flag3, string detail = "")
		{
			if (flag3)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void SelfTestCavernBasement()
	{
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			if (!BiomesCavernsCompat.Active)
			{
				sb.AppendLine("  SKIP  Biomes! Caverns not loaded - nothing to verify.");
				Report("cavern-basement self-test", sb, pass, fail);
				return;
			}
			Check("setting on", ABMod.Settings != null && ABMod.Settings.cavernBasements);
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false);
				Report("cavern-basement self-test", sb, pass, fail);
				return;
			}
			bool flag = val.Levels()?.lowerMap != null;
			bool generated;
			Map val2 = val.Levels()?.lowerMap ?? LevelMapGen.GetOrGenerate(val, -1, ABDefOf.AB_Basement, out generated);
			Check("basement exists", val2 != null);
			if (val2 == null)
			{
				Report("cavern-basement self-test", sb, pass, fail);
				return;
			}
			if (flag)
			{
				sb.AppendLine("  NOTE  basement predates this test; verifying whatever it has.");
			}
			string text = ((Def)(val2.Biome?)).defName ?? "null";
			Check("basement biome is a cavern biome", text.StartsWith("BMT_"), "biome=" + text);
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			foreach (IntVec3 allCell in val2.AllCells)
			{
				if (GridsUtility.GetEdifice(allCell, val2) == null && GenGrid.Walkable(allCell, val2))
				{
					num++;
					if (!RoofCollapseUtility.WithinRangeOfRoofHolder(allCell, val2, false))
					{
						num2++;
					}
					if (GridsUtility.GetPlant(allCell, val2) != null)
					{
						num3++;
					}
				}
			}
			Check("carved network is substantial", num > 300, "open=" + num);
			Check("no carved cell is out of roof-holder range", num2 == 0, "unsupported=" + num2);
			Check("cave flora present", num3 > 10, "plants=" + num3);
			int num4 = 0;
			IReadOnlyList<Pawn> allPawnsSpawned = val2.mapPawns.AllPawnsSpawned;
			for (int i = 0; i < allPawnsSpawned.Count; i++)
			{
				if (allPawnsSpawned[i].RaceProps.Animal && ((Thing)allPawnsSpawned[i]).Faction == null)
				{
					num4++;
				}
			}
			Check("starting fauna present", num4 >= 1, "fauna=" + num4);
			Messages.Message("AB dev: cavern basement checked - view the level below to explore it.", MessageTypeDefOf.NeutralEvent, false);
		}
		catch (Exception ex)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex);
		}
		Report("cavern-basement self-test", sb, pass, fail);
		void Check(string name, bool cond, string detail = "")
		{
			if (cond)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void SelfTestPeakPlateau()
	{
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			Check("setting on", ABMod.Settings != null && ABMod.Settings.naturalPeaks);
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false);
				Report("peak-plateau self-test", sb, pass, fail);
				return;
			}
			bool flag = val.Levels()?.upperMap != null;
			bool generated;
			Map val2 = val.Levels()?.upperMap ?? LevelMapGen.GetOrGenerate(val, 1, ABDefOf.AB_Sky, out generated);
			Check("sky level exists", val2 != null);
			if (val2 == null)
			{
				Report("peak-plateau self-test", sb, pass, fail);
				return;
			}
			if (flag)
			{
				sb.AppendLine("  NOTE  sky level predates this test; verifying whatever it has.");
			}
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			foreach (IntVec3 allCell in val2.AllCells)
			{
				TerrainDef terrain = GridsUtility.GetTerrain(allCell, val2);
				Building edifice = GridsUtility.GetEdifice(allCell, val2);
				if (edifice != null && ((Thing)edifice).def.building != null && ((Thing)edifice).def.building.isNaturalRock)
				{
					num4++;
				}
				else if (terrain == TerrainDefOf.Soil || terrain == TerrainDefOf.Gravel)
				{
					num++;
					if (val2.roofGrid.Roofed(allCell))
					{
						num2++;
					}
					if (GridsUtility.GetPlant(allCell, val2) != null)
					{
						num3++;
					}
				}
			}
			if (num == 0)
			{
				sb.AppendLine("  NOTE  no plateau cells - the surface mountain may be too small to open one. Walls=" + num4);
				Check("mountain mass present at all", num4 > 0, "walls=" + num4);
			}
			else
			{
				Check("plateau ground present", num > 40, "plateau=" + num);
				Check("plateau is open sky (unroofed)", num2 == 0, "roofed=" + num2);
				Check("plateau vegetation present", num3 > 0, "plants=" + num3);
				Check("cliff rim walls present", num4 > 0, "walls=" + num4);
			}
			Messages.Message("AB dev: peak plateau checked - go up a level to see it.", MessageTypeDefOf.NeutralEvent, false);
		}
		catch (Exception ex)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex);
		}
		Report("peak-plateau self-test", sb, pass, fail);
		void Check(string name, bool cond, string detail = "")
		{
			if (cond)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void SelfTestRitualAttendance()
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false);
				Report("ritual-attendance self-test", sb, pass, fail);
				return;
			}
			bool generated;
			Map val2 = val.Levels()?.lowerMap ?? LevelMapGen.GetOrGenerate(val, -1, ABDefOf.AB_Basement, out generated);
			Check("basement exists", val2 != null);
			if (val2 == null)
			{
				Report("ritual-attendance self-test", sb, pass, fail);
				return;
			}
			IntVec3 val3 = FindOpenBaseCell(val);
			ClearCell(val, val3);
			Building_ABStairs building_ABStairs = null;
			ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail("AB_StairsDown");
			if (namedSilentFail != null)
			{
				building_ABStairs = GenSpawn.Spawn(ThingMaker.MakeThing(namedSilentFail, ThingDefOf.WoodLog), val3, val, (WipeMode)0) as Building_ABStairs;
				if (building_ABStairs != null)
				{
					((Thing)building_ABStairs).SetFaction(Faction.OfPlayer, (Pawn)null);
				}
			}
			bool flag = building_ABStairs != null && building_ABStairs.CounterpartTowards(val2) != null;
			Check("stairs linked to the basement", flag);
			if (!flag)
			{
				Report("ritual-attendance self-test", sb, pass, fail);
				return;
			}
			Building_ABStairs building_ABStairs2 = building_ABStairs.CounterpartTowards(val2);
			IntVec3 val4 = CellFinder.StandableCellNear(((Thing)building_ABStairs2).Position, val2, 6f, (Predicate<IntVec3>)null);
			Pawn val5 = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, (PlanetTile?)null);
			GenSpawn.Spawn((Thing)(object)val5, ((IntVec3)(ref val4)).IsValid ? val4 : ((Thing)building_ABStairs2).Position, val2, (WipeMode)0);
			Check("colonist spawned in the basement", ((Thing)val5).Spawned && ((Thing)val5).Map == val2);
			bool flag2 = val.mapPawns.FreeColonistsAndPrisonersSpawned.Contains(val5);
			Check("basement colonist NOT in the surface pool outside the scope", !flag2);
			ABRitualAttendance.EnterScope();
			bool cond;
			try
			{
				cond = val.mapPawns.FreeColonistsAndPrisonersSpawned.Contains(val5);
			}
			finally
			{
				ABRitualAttendance.ExitScope();
			}
			Check("basement colonist IS in the surface pool inside the ritual scope", cond);
			bool flag3 = val.mapPawns.FreeColonistsAndPrisonersSpawned.Contains(val5);
			Check("vanilla pool untouched after the scope (no cache corruption)", !flag3);
			bool cond2 = false;
			Building_ABStairs building_ABStairs3 = CrossLevelWork.NearestUsableStairsCached(val5, val);
			if (building_ABStairs3?.CounterpartTowards(val) != null)
			{
				Job val6 = CrossLevelWork.MakeStairsJob(building_ABStairs3, building_ABStairs3.CounterpartTowards(val));
				Pawn_JobTracker jobs = val5.jobs;
				if (jobs != null)
				{
					jobs.StartJob(val6, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
				}
				cond2 = val5.CurJobDef == ABDefOf.AB_UseStairs;
			}
			Check("below participant takes the stairs toward the ritual map", cond2, "job=" + (((Def)(val5.CurJobDef?)).defName ?? "null"));
			Messages.Message("AB dev: ritual-attendance mechanism verified. For the full flow, assign a role holder below and begin a real ritual.", MessageTypeDefOf.NeutralEvent, false);
		}
		catch (Exception ex)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex);
		}
		Report("ritual-attendance self-test", sb, pass, fail);
		void Check(string name, bool flag4, string detail = "")
		{
			if (flag4)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private unsafe static void BelowViewDiagnostic()
	{
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0646: Unknown result type (might be due to invalid IL or missing references)
		//IL_06af: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_073c: Unknown result type (might be due to invalid IL or missing references)
		//IL_078a: Unknown result type (might be due to invalid IL or missing references)
		//IL_07bd: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder stringBuilder = new StringBuilder();
		try
		{
			Map currentMap = Find.CurrentMap;
			Map val = currentMap?.GroundMap();
			Map val2 = val?.Levels()?.upperMap;
			stringBuilder.AppendLine("current map=" + (currentMap?.uniqueID.ToString() ?? "null") + " level=" + (currentMap?.Level() ?? 0) + " | ground=" + (val?.uniqueID.ToString() ?? "null") + " | sky=" + (val2?.uniqueID.ToString() ?? "null"));
			stringBuilder.AppendLine("guards: rendering=" + ABGuard.On(ABGuard.Rendering) + " async=" + ABGuard.On(ABGuard.Async) + " roofSync=" + ABGuard.On(ABGuard.RoofSync) + " | showLiveBelow=" + (ABMod.Settings?.showLiveBelow ?? false) + " | queueCeiling=" + LevelRenderer.DebugQueueCeiling);
			stringBuilder.AppendLine("belowThings tallies: " + SectionLayer_ABBelowThings.DiagSummary());
			if (val2 != null && !val2.Disposed)
			{
				Section[,] array = LevelRenderer.DebugSections(val2);
				if (array == null)
				{
					stringBuilder.AppendLine("sky drawer: sections NOT built yet");
				}
				else
				{
					int num = 0;
					int num2 = 0;
					long num3 = 0L;
					Section[,] array2 = array;
					foreach (Section val3 in array2)
					{
						SectionLayer val4 = ((val3 != null) ? val3.GetLayer(typeof(SectionLayer_ABBelowThings)) : null);
						if (val4 != null)
						{
							num++;
							long num4 = 0L;
							for (int k = 0; k < ((MapDrawLayer)val4).subMeshes.Count; k++)
							{
								num4 += ((MapDrawLayer)val4).subMeshes[k].verts.Count;
							}
							if (num4 > 0)
							{
								num2++;
							}
							num3 += num4;
						}
					}
					int num5 = 0;
					TerrainGrid terrainGrid = val2.terrainGrid;
					foreach (IntVec3 allCell in val2.AllCells)
					{
						if (terrainGrid.TerrainAt(allCell) == ABDefOf.AB_OpenAir)
						{
							num5++;
						}
					}
					stringBuilder.AppendLine("sky prints: sections=" + num + " withContent=" + num2 + " totalVerts=" + num3 + " openAirCells=" + num5);
				}
			}
			if (val != null)
			{
				Section[,] array3 = LevelRenderer.DebugSections(val);
				if (array3 != null)
				{
					IntVec3 val5 = Find.CameraDriver.MapPosition;
					if (!GenGrid.InBounds(val5, val))
					{
						val5 = val.Center;
					}
					Section val6 = val.mapDrawer.SectionAt(val5);
					if (val6 != null)
					{
						IntVec3 val7 = val5;
						stringBuilder.AppendLine("ground section @" + ((object)(*(IntVec3*)(&val7))/*cast due to .constrained prefix*/).ToString() + " layers:");
						List<SectionLayer> list = LevelRenderer.DebugLayers(val6);
						for (int l = 0; l < list.Count; l++)
						{
							SectionLayer val8 = list[l];
							long num6 = 0L;
							float num7 = -99f;
							int num8 = -1;
							for (int m = 0; m < ((MapDrawLayer)val8).subMeshes.Count; m++)
							{
								LayerSubMesh val9 = ((MapDrawLayer)val8).subMeshes[m];
								num6 += val9.verts.Count;
								if (val9.finalized && (Object)(object)val9.mesh != (Object)null)
								{
									float num9 = num7;
									Bounds bounds = val9.mesh.bounds;
									num7 = Mathf.Max(num9, ((Bounds)(ref bounds)).center.y);
								}
								if (num8 < 0 && (Object)(object)val9.material != (Object)null)
								{
									num8 = val9.material.renderQueue;
								}
							}
							if (num6 > 0)
							{
								stringBuilder.AppendLine("  " + ((object)val8).GetType().Name + ": verts=" + num6 + " maxBoundsY=" + num7.ToString("0.00") + " q=" + num8);
							}
						}
					}
				}
			}
			Map[] array4 = (Map[])(object)new Map[2] { val, val2 };
			foreach (Map val10 in array4)
			{
				if (val10 == null || val10.Disposed)
				{
					continue;
				}
				List<Building> allBuildingsColonist = val10.listerBuildings.allBuildingsColonist;
				for (int num10 = 0; num10 < allBuildingsColonist.Count; num10++)
				{
					Building val11 = allBuildingsColonist[num10];
					if (!(((Thing)(val11?)).def?.thingClass == null) && ((Thing)val11).def.thingClass.Name.Contains("AirDefense"))
					{
						RoofDef val12 = val10.roofGrid.RoofAt(((Thing)val11).Position);
						stringBuilder.AppendLine("ADA '" + ((Def)((Thing)val11).def).defName + "' on map " + val10.uniqueID + " (level " + val10.Level() + ") at " + ((object)((Thing)val11).Position/*cast due to .constrained prefix*/).ToString() + ": roof=" + (((Def)(val12?)).defName ?? "none") + " terrain=" + ((Def)(val10.terrainGrid.TerrainAt(((Thing)val11).Position)?)).defName);
						Map val13 = ((val10.Level() == 0) ? val2 : val);
						if (val13 != null && !val13.Disposed && GenGrid.InBounds(((Thing)val11).Position, val13))
						{
							stringBuilder.AppendLine("    same cell on level " + val13.Level() + ": roof=" + (((Def)(val13.roofGrid.RoofAt(((Thing)val11).Position)?)).defName ?? "none") + " terrain=" + ((Def)(val13.terrainGrid.TerrainAt(((Thing)val11).Position)?)).defName);
						}
					}
				}
			}
			if (val2 != null && !val2.Disposed && LevelRenderer.DrawerReady(val2))
			{
				val2.mapDrawer.WholeMapChanged(MapMeshFlagDef.op_Implicit(ABDefOf.AB_BelowThings));
				stringBuilder.AppendLine("forced below-print reprint armed (in-view sections regen next frame).");
			}
		}
		catch (Exception ex)
		{
			stringBuilder.AppendLine("EXCEPTION: " + ex);
		}
		Log.Warning("[As above, So below] BELOW-VIEW DIAGNOSTIC\n" + stringBuilder);
		Messages.Message("AB dev: below-view diagnostic logged. Run it again after a few seconds to compare.", MessageTypeDefOf.NeutralEvent, false);
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void SelfTestPodTransit()
	{
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Expected O, but got Unknown
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0495: Expected O, but got Unknown
		//IL_04bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_054c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0550: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false, "no ground map");
				Report("pod transit self-test", sb, pass, fail);
				return;
			}
			bool generated;
			Map val2 = val.Levels()?.upperMap ?? LevelMapGen.GetOrGenerate(val, 1, ABDefOf.AB_Sky, out generated);
			Check("sky level exists", val2 != null);
			if (val2 == null)
			{
				Report("pod transit self-test", sb, pass, fail);
				return;
			}
			Check("drop pod def is transit-eligible", PodTransit.IsTransitDef(ThingDefOf.DropPodIncoming));
			ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail("MeteoriteIncoming");
			Check("meteorite def is transit-eligible", namedSilentFail != null && PodTransit.IsTransitDef(namedSilentFail));
			ThingDef namedSilentFail2 = DefDatabase<ThingDef>.GetNamedSilentFail("ShuttleIncoming");
			if (namedSilentFail2 != null)
			{
				Check("shuttle def is NOT transit-eligible", !PodTransit.IsTransitDef(namedSilentFail2));
			}
			IntVec3 val3 = FindOpenBaseCell(val);
			ClearCell(val, val3);
			ClearCell(val2, val3);
			if (val.roofGrid.Roofed(val3))
			{
				val.roofGrid.SetRoof(val3, (RoofDef)null);
			}
			val2.terrainGrid.SetTerrain(val3, ABDefOf.AB_OpenAir);
			if (val2.roofGrid.Roofed(val3))
			{
				val2.roofGrid.SetRoof(val3, (RoofDef)null);
			}
			Check("gap is open through the sky level", PodTransit.GapOpen(val2, CellRect.SingleCell(val3)));
			bool flag = ABMod.Settings != null && ABMod.Settings.podTransit;
			Check("podTransit setting is on", flag, "enable it in mod settings to test");
			ActiveTransporterInfo val4 = new ActiveTransporterInfo();
			Thing val5 = ThingMaker.MakeThing(ThingDefOf.Steel, (ThingDef)null);
			val5.stackCount = 25;
			val4.innerContainer.TryAdd(val5, true);
			DropPodUtility.MakeDropPodAt(val3, val, val4, (Faction)null);
			DropPodIncoming val6 = null;
			List<Thing> thingList = GridsUtility.GetThingList(val3, val);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing obj = thingList[i];
				val6 = (DropPodIncoming)(object)((obj is DropPodIncoming) ? obj : null);
				if (val6 != null)
				{
					break;
				}
			}
			Check("pod spawned on the surface gap cell", val6 != null);
			if (val6 == null || !flag)
			{
				Report("pod transit self-test", sb, pass, fail);
				return;
			}
			PodTransitComp component = val.GetComponent<PodTransitComp>();
			Check("pod queued for lift to the sky level", component?.DevQueuedForLift((Skyfaller)(object)val6) ?? false);
			int ticksToImpact = ((Skyfaller)val6).ticksToImpact;
			if (component != null)
			{
				((MapComponent)component).MapComponentTick();
			}
			Check("pod transferred to the sky map", ((Thing)val6).Spawned && ((Thing)val6).Map == val2, "map=" + (((Thing)val6).Map?.uniqueID.ToString() ?? "null"));
			Check("descent clock preserved across the lift", ((Skyfaller)val6).ticksToImpact == ticksToImpact, "before=" + ticksToImpact + " after=" + ((Skyfaller)val6).ticksToImpact);
			PodTransitComp component2 = val2.GetComponent<PodTransitComp>();
			int num = component2?.DevTransferAt((Skyfaller)(object)val6) ?? (-1);
			Check("handoff mark registered on the sky map", num > 0, "at=" + num);
			if (num > 0)
			{
				((Skyfaller)val6).ticksToImpact = num;
				((MapComponent)component2).MapComponentTick();
				Check("pod handed off to the ground map", ((Thing)val6).Spawned && ((Thing)val6).Map == val, "map=" + (((Thing)val6).Map?.uniqueID.ToString() ?? "null"));
				Check("lower leg keeps the remaining descent", ((Skyfaller)val6).ticksToImpact == num, "expected=" + num + " actual=" + ((Skyfaller)val6).ticksToImpact);
			}
			ActiveTransporterInfo val7 = new ActiveTransporterInfo();
			Thing val8 = ThingMaker.MakeThing(ThingDefOf.WoodLog, (ThingDef)null);
			val8.stackCount = 10;
			val7.innerContainer.TryAdd(val8, true);
			DropPodUtility.MakeDropPodAt(val3, val2, val7, (Faction)null);
			DropPodIncoming val9 = null;
			List<Thing> thingList2 = GridsUtility.GetThingList(val3, val2);
			for (int j = 0; j < thingList2.Count; j++)
			{
				Thing obj2 = thingList2[j];
				val9 = (DropPodIncoming)(object)((obj2 is DropPodIncoming) ? obj2 : null);
				if (val9 != null && val9 != val6)
				{
					break;
				}
				val9 = null;
			}
			Check("sky-spawned pod over open air registers a descent", val9 != null && component2 != null && component2.DevTransferAt((Skyfaller)(object)val9) > 0);
			Messages.Message("AB dev: pod transit demo armed - two cargo pods are falling through the sky gap. View the SKY level to watch; check the surface for the deliveries.", LookTargets.op_Implicit(new TargetInfo(val3, val, false)), MessageTypeDefOf.NeutralEvent, false);
		}
		catch (Exception ex)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex);
		}
		Report("pod transit self-test", sb, pass, fail);
		void Check(string name, bool cond, string detail = "")
		{
			if (cond)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void ToggleCapCornerFillers()
	{
		SectionLayer_ABMountainCap.CornerFillersEnabled = !SectionLayer_ABMountainCap.CornerFillersEnabled;
		List<Map> maps = Find.Maps;
		for (int i = 0; i < maps.Count; i++)
		{
			if (maps[i].Level() == 1)
			{
				maps[i].mapDrawer.WholeMapChanged(MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Terrain));
			}
		}
		Messages.Message("AB dev: cap corner fillers " + (SectionLayer_ABMountainCap.CornerFillersEnabled ? "ON" : "OFF") + " - compare the dash artifacts.", MessageTypeDefOf.NeutralEvent, false);
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private unsafe static void ProbeLedgeCell()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		IntVec3 val = UI.MouseCell();
		Map currentMap = Find.CurrentMap;
		if (currentMap == null || !GenGrid.InBounds(val, currentMap))
		{
			return;
		}
		Map val2 = ((currentMap.Level() == 1) ? currentMap : currentMap.UpperMap());
		Map val3 = currentMap.GroundMap();
		StringBuilder stringBuilder = new StringBuilder();
		string[] obj = new string[7] { "[AB probe] cell ", null, null, null, null, null, null };
		IntVec3 val4 = val;
		obj[1] = ((object)(*(IntVec3*)(&val4))/*cast due to .constrained prefix*/).ToString();
		obj[2] = " viewing map ";
		obj[3] = currentMap.uniqueID.ToString();
		obj[4] = " (level ";
		obj[5] = currentMap.Level().ToString();
		obj[6] = ")";
		stringBuilder.AppendLine(string.Concat(obj));
		if (val2 != null && !val2.Disposed && GenGrid.InBounds(val, val2))
		{
			TerrainDef val5 = val2.terrainGrid.TerrainAt(val);
			Building edifice = GridsUtility.GetEdifice(val, val2);
			stringBuilder.AppendLine("  sky terrain=" + (((Def)(val5?)).defName ?? "null") + " edifice=" + (((Def)((Thing)(edifice?)).def).defName ?? "none") + " fogged=" + GridsUtility.Fogged(val, val2));
		}
		else
		{
			stringBuilder.AppendLine("  sky: none");
		}
		if (val3 != null && GenGrid.InBounds(val, val3))
		{
			RoofDef val6 = val3.roofGrid.RoofAt(val);
			Building val7 = val3.edificeGrid[val];
			stringBuilder.AppendLine("  ground roof=" + (((Def)(val6?)).defName ?? "none") + " (natural=" + (val6?.isNatural ?? false) + ", thick=" + (val6?.isThickRoof ?? false) + ") edifice=" + (((Def)((Thing)(val7?)).def).defName ?? "none") + " mineable=" + (((Thing)(val7?)).def.mineable ?? false) + " fogged=" + GridsUtility.Fogged(val, val3));
			stringBuilder.AppendLine("  CoveredBelow=" + LevelSync.CoveredBelow(val3, val));
		}
		if (val2 != null && !val2.Disposed && GenGrid.InBounds(val, val2))
		{
			TerrainGrid terrainGrid = val2.terrainGrid;
			TerrainDef aB_MountainTop = ABDefOf.AB_MountainTop;
			int num = 0;
			for (int i = 0; i < 4; i++)
			{
				IntVec3 c = val + GenAdj.CardinalDirections[i];
				if (SectionLayer_ABMountainCap.Linked(val2, terrainGrid, aB_MountainTop, c))
				{
					num |= 1 << i;
				}
			}
			stringBuilder.AppendLine("  cap fill: massCell=" + SectionLayer_ABMountainCap.IsMassCell(val2, terrainGrid, aB_MountainTop, val) + " linkMask=" + num + " (15 = fully interior)");
			stringBuilder.AppendLine("  " + SectionLayer_ABMountainCap.DebugCapFillInfo(val2, val3, val));
		}
		Log.Warning("[As above, So below] LEDGEPROBE:\n" + stringBuilder);
		val4 = val;
		Messages.Message("AB probe logged for " + ((object)(*(IntVec3*)(&val4))/*cast due to .constrained prefix*/).ToString() + " - check the dev log.", MessageTypeDefOf.NeutralEvent, false);
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void SelfTestMechOverseer()
	{
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Invalid comparison between Unknown and I4
		//IL_055e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0563: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0579: Unknown result type (might be due to invalid IL or missing references)
		//IL_057f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		//IL_0586: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0414: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Invalid comparison between Unknown and I4
		//IL_0482: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e7: Unknown result type (might be due to invalid IL or missing references)
		if (!ModsConfig.BiotechActive)
		{
			Messages.Message("AB dev: Biotech is not active; mech overseer test skipped.", MessageTypeDefOf.RejectInput, false);
			return;
		}
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false, "no ground map");
				Report("mech overseer self-test", sb, pass, fail);
				return;
			}
			bool generated;
			Map val2 = val.Levels()?.lowerMap ?? LevelMapGen.GetOrGenerate(val, -1, ABDefOf.AB_Basement, out generated);
			Check("basement exists", val2 != null);
			if (val2 == null)
			{
				Report("mech overseer self-test", sb, pass, fail);
				return;
			}
			List<Pawn> freeColonists = val.mapPawns.FreeColonists;
			Pawn val3 = ((freeColonists.Count > 0) ? freeColonists[0] : null);
			Check("a colonist is available as overseer", val3 != null);
			if (val3 == null)
			{
				Report("mech overseer self-test", sb, pass, fail);
				return;
			}
			if (!MechanitorUtility.IsMechanitor(val3))
			{
				HediffDef namedSilentFail = DefDatabase<HediffDef>.GetNamedSilentFail("MechlinkImplant");
				if (namedSilentFail != null)
				{
					val3.health.AddHediff(namedSilentFail, val3.health.hediffSet.GetBrain(), (DamageInfo?)null, (DamageResult)null);
					PawnComponentsUtility.AddAndRemoveDynamicComponents(val3, false);
				}
			}
			Check("overseer has a mechanitor tracker", val3.mechanitor != null);
			if (val3.mechanitor == null)
			{
				Report("mech overseer self-test", sb, pass, fail);
				return;
			}
			PawnKindDef namedSilentFail2 = DefDatabase<PawnKindDef>.GetNamedSilentFail("Mech_Lifter");
			Check("lifter pawn kind found", namedSilentFail2 != null);
			if (namedSilentFail2 == null)
			{
				Report("mech overseer self-test", sb, pass, fail);
				return;
			}
			Pawn val4 = PawnGenerator.GeneratePawn(new PawnGenerationRequest(namedSilentFail2, Faction.OfPlayer, (PawnGenerationContext)2, (PlanetTile?)null, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, (Pawn)null, 1f, (Predicate<Pawn>)null, (Predicate<Pawn>)null, (IEnumerable<TraitDef>)null, (IEnumerable<TraitDef>)null, (float?)null, (float?)null, (float?)null, (Gender?)null, (string)null, (string)null, (RoyalTitleDef)null, (Ideo)null, false, false, false, false, (List<GeneDef>)null, (List<GeneDef>)null, (XenotypeDef)null, (CustomXenotype)null, (List<XenotypeDef>)null, 0f, (DevelopmentalStage)8, (Func<XenotypeDef, PawnKindDef>)null, (FloatRange?)null, (FloatRange?)null, false, false, false, -1, 0, false));
			IntVec3 val5 = FindOpenBaseCell(val);
			GenSpawn.Spawn((Thing)(object)val4, val5, val, (WipeMode)0);
			val3.relations.AddDirectRelation(PawnRelationDefOf.Overseer, val4);
			PawnComponentsUtility.AddAndRemoveDynamicComponents(val4, false);
			val3.mechanitor.AssignPawnControlGroup(val4, (MechWorkModeDef)null);
			if (val4.needs?.energy != null)
			{
				((Need)val4.needs.energy).CurLevel = ((Need)val4.needs.energy).MaxLevel;
			}
			bool cond = val4.OverseerSubject != null && (int)val4.OverseerSubject.State == 2;
			CompOverseerSubject overseerSubject = val4.OverseerSubject;
			Check("mech is overseen on the surface", cond, "state=" + ((overseerSubject != null) ? new OverseerSubjectState?(overseerSubject.State) : ((OverseerSubjectState?)null)).ToString());
			IntVec3 val6 = CarveBasementPocket(val2, ((Thing)val3).Position);
			Pawn_JobTracker jobs = val4.jobs;
			if (jobs != null)
			{
				jobs.EndCurrentJob((JobCondition)16, false, true);
			}
			((Entity)val4).DeSpawn((DestroyMode)0);
			GenSpawn.Spawn((Thing)(object)val4, val6, val2, (WipeMode)0);
			Check("mech transferred to the basement", ((Thing)val4).Spawned && ((Thing)val4).Map == val2);
			bool cond2 = val4.OverseerSubject != null && (int)val4.OverseerSubject.State == 2;
			CompOverseerSubject overseerSubject2 = val4.OverseerSubject;
			Check("mech is STILL overseen across levels", cond2, "state=" + ((overseerSubject2 != null) ? new OverseerSubjectState?(overseerSubject2.State) : ((OverseerSubjectState?)null)).ToString());
			Check("mechanitor command range reaches through the column", MechanitorUtility.InMechanitorCommandRange(val4, LocalTargetInfo.op_Implicit(((Thing)val4).Position)), "overseer at " + ((object)((Thing)val3).Position/*cast due to .constrained prefix*/).ToString() + ", mech at " + ((object)((Thing)val4).Position/*cast due to .constrained prefix*/).ToString());
			for (int i = 0; i < 2; i++)
			{
				if (val4.needs?.energy != null)
				{
					((Need)val4.needs.energy).CurLevel = ((i == 0) ? 1f : 0.25f) * ((Need)val4.needs.energy).MaxLevel;
				}
				ThinkResult val7 = ThinkResult.NoJob;
				string text = null;
				try
				{
					val7 = val4.thinker.MainThinkNodeRoot.TryIssueJobPackage(val4, default(JobIssueParams));
				}
				catch (Exception ex)
				{
					text = ex.GetType().Name + ": " + ex.Message;
				}
				string text2 = ((Def)(((ThinkResult)(ref val7)).Job?.def?)).defName ?? "none";
				string text3 = ((object)((ThinkResult)(ref val7)).SourceNode)?.GetType().Name ?? "none";
				string text4 = ((i == 0) ? "full energy" : "25% energy");
				sb.AppendLine("  info  think (" + text4 + "): job=" + text2 + " giver=" + text3 + ((text != null) ? (" EX=" + text) : ""));
				Check("think tree (" + text4 + ") does not force dormant self-shutdown", text == null && (((ThinkResult)(ref val7)).Job == null || ((ThinkResult)(ref val7)).Job.def != JobDefOf.SelfShutdown), "job=" + text2 + " giver=" + text3 + ((text != null) ? (" EX=" + text) : ""));
			}
			if (val4.needs?.energy != null)
			{
				((Need)val4.needs.energy).CurLevel = ((Need)val4.needs.energy).MaxLevel;
			}
			Messages.Message("AB dev: mech overseer self-test done. Lifter left live in the basement pocket - watch what job it settles into.", MessageTypeDefOf.NeutralEvent, false);
		}
		catch (Exception ex2)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex2);
		}
		Report("mech overseer self-test", sb, pass, fail);
		void Check(string name, bool flag, string detail = "")
		{
			if (flag)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	[DebugAction(/*Could not decode attribute arguments.*/)]
	private static void SelfTestBondAndHome()
	{
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Expected O, but got Unknown
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Expected O, but got Unknown
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder sb = new StringBuilder();
		int pass = 0;
		int fail = 0;
		try
		{
			Map val = Find.CurrentMap?.GroundMap();
			if (val == null)
			{
				Check("ground/surface map exists", cond: false, "no ground map");
				Report("bond + home self-test", sb, pass, fail);
				return;
			}
			bool generated;
			Map val2 = val.Levels()?.lowerMap ?? LevelMapGen.GetOrGenerate(val, -1, ABDefOf.AB_Basement, out generated);
			Check("basement exists", val2 != null);
			if (val2 == null)
			{
				Report("bond + home self-test", sb, pass, fail);
				return;
			}
			Check("surface is player home", val.IsPlayerHome);
			Check("basement inherits the column's home verdict", val2.IsPlayerHome == val.IsPlayerHome, "basement=" + val2.IsPlayerHome + " surface=" + val.IsPlayerHome);
			Map val3 = val.Levels()?.upperMap;
			if (val3 != null)
			{
				Check("sky inherits the column's home verdict", val3.IsPlayerHome == val.IsPlayerHome, "sky=" + val3.IsPlayerHome + " surface=" + val.IsPlayerHome);
			}
			if (!ModsConfig.BiotechActive)
			{
				sb.AppendLine("  info  Biotech not active; bond half skipped.");
				Report("bond + home self-test", sb, pass, fail);
				return;
			}
			List<Pawn> freeColonists = val.mapPawns.FreeColonists;
			Pawn val4 = ((freeColonists.Count > 0) ? freeColonists[0] : null);
			Pawn val5 = ((freeColonists.Count > 1) ? freeColonists[1] : null);
			if (val5 == null && val4 != null)
			{
				val5 = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, (PlanetTile?)null);
				GenSpawn.Spawn((Thing)(object)val5, FindOpenBaseCell(val), val, (WipeMode)0);
			}
			Check("two colonists available for bonding", val4 != null && val5 != null);
			if (val4 == null || val5 == null)
			{
				Report("bond + home self-test", sb, pass, fail);
				return;
			}
			Hediff firstHediffOfDef = val4.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond, false);
			Hediff_PsychicBond val6 = (Hediff_PsychicBond)(object)((firstHediffOfDef is Hediff_PsychicBond) ? firstHediffOfDef : null);
			if (val6 == null)
			{
				val6 = (Hediff_PsychicBond)val4.health.AddHediff(HediffDefOf.PsychicBond, (BodyPartRecord)null, (DamageInfo?)null, (DamageResult)null);
				((HediffWithTarget)val6).target = (Thing)(object)val5;
			}
			Hediff firstHediffOfDef2 = val5.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond, false);
			Hediff_PsychicBond val7 = (Hediff_PsychicBond)(object)((firstHediffOfDef2 is Hediff_PsychicBond) ? firstHediffOfDef2 : null);
			if (val7 == null)
			{
				val7 = (Hediff_PsychicBond)val5.health.AddHediff(HediffDefOf.PsychicBond, (BodyPartRecord)null, (DamageInfo?)null, (DamageResult)null);
				((HediffWithTarget)val7).target = (Thing)(object)val4;
			}
			IntVec3 val8 = CarveBasementPocket(val2, ((Thing)val5).Position);
			Pawn_JobTracker jobs = val5.jobs;
			if (jobs != null)
			{
				jobs.EndCurrentJob((JobCondition)16, false, true);
			}
			((Entity)val5).DeSpawn((DestroyMode)0);
			GenSpawn.Spawn((Thing)(object)val5, val8, val2, (WipeMode)0);
			Check("bonded pawn moved to the basement", ((Thing)val5).Spawned && ((Thing)val5).Map == val2);
			Check("bond counts as NEAR from the surface side", ThoughtWorker_PsychicBondProximity.NearPsychicBondedPerson(val4, val6));
			Check("bond counts as NEAR from the basement side", ThoughtWorker_PsychicBondProximity.NearPsychicBondedPerson(val5, val7));
			Messages.Message("AB dev: bond + home self-test done. " + ((Entity)val4).LabelShort + " (surface) and " + ((Entity)val5).LabelShort + " (basement) are psychically bonded - check their mood tabs for the bond WITHOUT the distance debuff.", MessageTypeDefOf.NeutralEvent, false);
		}
		catch (Exception ex)
		{
			fail++;
			sb.AppendLine("  EXCEPTION during self-test:\n" + ex);
		}
		Report("bond + home self-test", sb, pass, fail);
		void Check(string name, bool cond, string detail = "")
		{
			if (cond)
			{
				pass++;
				sb.AppendLine("  PASS  " + name);
			}
			else
			{
				fail++;
				sb.AppendLine("  FAIL  " + name + (string.IsNullOrEmpty(detail) ? "" : ("   [" + detail + "]")));
			}
		}
	}

	private static IntVec3 CarveBasementPocket(Map basement, IntVec3 around)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		IntVec3 val = around;
		val.x = Mathf.Clamp(val.x, 2, basement.Size.x - 3);
		val.z = Mathf.Clamp(val.z, 2, basement.Size.z - 3);
		IntVec3 val2 = default(IntVec3);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				((IntVec3)(ref val2))._002Ector(val.x + i, 0, val.z + j);
				ClearCell(basement, val2);
				basement.fogGrid.Unfog(val2);
			}
		}
		return val;
	}

	private static void MakePlatform(Map sky, Map surface, IntVec3 c)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		ClearCell(sky, c);
		sky.terrainGrid.SetTerrain(c, ABDefOf.AB_RoofSurface);
		if (GenGrid.InBounds(c, surface))
		{
			surface.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
		}
	}

	private static IntVec3 FindOpenBaseCell(Map surface)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		foreach (IntVec3 item in GenRadial.RadialCellsAround(surface.Center, 24f, true))
		{
			if (GenGrid.InBounds(item, surface) && GenGrid.Standable(item, surface) && !GridsUtility.Fogged(item, surface) && GenGrid.InBounds(item + IntVec3.East, surface))
			{
				return item;
			}
		}
		return surface.Center;
	}

	private static void ClearCell(Map map, IntVec3 c)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		if (!GenGrid.InBounds(c, map))
		{
			return;
		}
		List<Thing> list = new List<Thing>(GridsUtility.GetThingList(c, map));
		for (int i = 0; i < list.Count; i++)
		{
			Thing val = list[i];
			if (val != null && !val.Destroyed)
			{
				ThingCategory category = val.def.category;
				if ((int)category == 3 || (int)category == 2 || (int)category == 4)
				{
					val.Destroy((DestroyMode)0);
				}
			}
		}
	}

	private static Pawn SpawnHostile(Map surface, IntVec3 cell)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Faction val = Find.FactionManager.RandomEnemyFaction(false, false, false, (TechLevel)0) ?? Find.FactionManager.RandomEnemyFaction(false, false, true, (TechLevel)0);
			PawnKindDef val2 = DefDatabase<PawnKindDef>.GetNamedSilentFail("Pirate") ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Drifter") ?? PawnKindDefOf.Colonist;
			Pawn val3 = PawnGenerator.GeneratePawn(val2, val, (PlanetTile?)null);
			GenSpawn.Spawn((Thing)(object)val3, cell, surface, (WipeMode)0);
			return val3;
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] dev self-test could not spawn a hostile: " + ex.Message);
			return null;
		}
	}

	private static void ArmWithRanged(Pawn p)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		try
		{
			if (p?.equipment != null && CrossLevelCombat.GetRangedVerb(p) == null)
			{
				ThingDef val = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_Revolver") ?? DefDatabase<ThingDef>.GetNamedSilentFail("Gun_Autopistol");
				if (val != null)
				{
					p.equipment.DestroyAllEquipment((DestroyMode)0);
					p.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(val, (ThingDef)null));
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] dev self-test could not arm pawn: " + ex.Message);
		}
	}

	private static Pawn SpawnArmedColonist(Map sky, IntVec3 cell)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		try
		{
			Pawn val = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, (PlanetTile?)null);
			GenSpawn.Spawn((Thing)(object)val, cell, sky, (WipeMode)0);
			ThingDef val2 = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_BoltActionRifle") ?? DefDatabase<ThingDef>.GetNamedSilentFail("Gun_Autopistol") ?? DefDatabase<ThingDef>.GetNamedSilentFail("Gun_Revolver");
			if (val2 != null && val.equipment != null)
			{
				val.equipment.DestroyAllEquipment((DestroyMode)0);
				val.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(val2, (ThingDef)null));
			}
			if (val.drafter == null)
			{
				val.drafter = new Pawn_DraftController(val);
			}
			val.drafter.Drafted = true;
			return val;
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] dev self-test could not spawn an armed colonist: " + ex.Message);
			return null;
		}
	}

	private static bool TestReverseEnclosed(Map surface, Map sky, IntVec3 b)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			IntVec3 val = b + new IntVec3(8, 0, 0);
			if (!GenGrid.InBounds(val, sky))
			{
				return true;
			}
			TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
			bool flag = sky.terrainGrid.TerrainAt(val) == aB_OpenAir;
			for (int i = 0; i < 8; i++)
			{
				IntVec3 val2 = val + GenAdj.AdjacentCells[i];
				if (GenGrid.InBounds(val2, sky) && sky.terrainGrid.TerrainAt(val2) == aB_OpenAir)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				sky.terrainGrid.SetTerrain(val, ABDefOf.AB_RoofSurface);
				for (int j = 0; j < 8; j++)
				{
					IntVec3 val3 = val + GenAdj.AdjacentCells[j];
					if (GenGrid.InBounds(val3, sky) && sky.terrainGrid.TerrainAt(val3) == aB_OpenAir)
					{
						sky.terrainGrid.SetTerrain(val3, ABDefOf.AB_RoofSurface);
					}
				}
			}
			Thing val4 = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.WoodLog);
			GenSpawn.Spawn(val4, val, sky, (WipeMode)0);
			Verb_LaunchProjectile val5 = null;
			Pawn val6 = null;
			try
			{
				val6 = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, (PlanetTile?)null);
				GenSpawn.Spawn((Thing)(object)val6, val, surface, (WipeMode)0);
				val5 = CrossLevelCombat.GetRangedVerb(val6);
				CrossLevelCombat.GapShot shot;
				return val5 == null || !CrossLevelCombat.CanFireFrom(surface, val, val4, val5, out shot);
			}
			finally
			{
				if (val4 != null)
				{
					val4.Destroy((DestroyMode)0);
				}
				if (val6 != null && ((Thing)val6).Spawned)
				{
					((Thing)val6).Destroy((DestroyMode)0);
				}
			}
		}
		catch
		{
			return true;
		}
	}

	private static void Report(string name, StringBuilder body, int pass, int fail)
	{
		int num = pass + fail;
		string text = "[As above, So below] SELF-TEST: " + name + "\nwhen: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\nresult: " + pass + "/" + num + " checks passed" + ((fail > 0) ? (" -- " + fail + " FAILED") : " -- ALL PASS") + "\n\n";
		string text2 = text + body;
		try
		{
			ModContentPack modContent = ABMod.ModContent;
			string text3 = ((modContent != null) ? modContent.RootDir : null);
			if (!string.IsNullOrEmpty(text3))
			{
				string text4 = Path.Combine(text3, "docs");
				Directory.CreateDirectory(text4);
				string text5 = Path.Combine(text4, "SelfTest.log");
				if (File.Exists(text5) && new FileInfo(text5).Length > 262144)
				{
					File.Delete(text5);
				}
				File.AppendAllText(text5, text2 + "\n----\n");
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] could not write docs/SelfTest.log: " + ex.Message);
		}
		if (fail > 0)
		{
			Log.Error("[As above, So below] SELFTEST '" + name + "': " + fail + " of " + num + " checks FAILED (see docs/SelfTest.log):\n" + body);
		}
		else
		{
			Log.Warning("[As above, So below] SELFTEST '" + name + "': all " + num + " checks passed.");
		}
		Messages.Message("AB self-test: " + pass + " pass / " + fail + " fail. See docs/SelfTest.log.", (fail > 0) ? MessageTypeDefOf.NegativeEvent : MessageTypeDefOf.TaskCompletion, false);
	}
}

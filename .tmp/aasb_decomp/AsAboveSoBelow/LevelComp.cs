using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

public class LevelComp : MapComponent
{
	private static WeakReference skyGame;

	private static int skyCount;

	public int level;

	public Map upperMap;

	public Map lowerMap;

	public Map groundMap;

	private Dictionary<int, Map> mapByLevel;

	private List<int> tmpLevels;

	private List<Map> tmpMaps;

	private List<Building_ABStairs> stairsList;

	private bool syncSubscribed;

	private Action<IntVec3> roofChangedHandler;

	private Action<IntVec3> terrainChangedHandler;

	private Action<Thing> thingSpawnedHandler;

	private Action<Thing> thingDespawnedHandler;

	private const int WeatherSyncInterval = 150;

	private const int PipeBridgeInterval = 100;

	private const int SubscribeRetryInterval = 250;

	private const int HostileScanInterval = 250;

	private const int AutoEngageInterval = 250;

	private const int RooftopSweepInterval = 2000;

	private const int HiddenWeatherMultiplier = 4;

	private const int HiddenSweepMultiplier = 5;

	private const int SweepCellsPerTick = 4096;

	private int nextWeatherDue = -1;

	private int nextSweepDue = -1;

	private int nextHostileDue = -1;

	private int nextAutoEngageDue = -1;

	private int nextGroundHostileDue = -1;

	private int nextAnimalDue = -1;

	private const int AnimalWanderInterval = 1200;

	private int nextPipesDue = -1;

	private int sweepCursor = -1;

	private bool wasVisible;

	private bool belowPrintsHealed;

	public static bool AnySkyLevels => skyCount > 0 && skyGame != null && skyGame.Target == Current.Game;

	public Dictionary<int, Map> MapByLevel => mapByLevel ?? (mapByLevel = new Dictionary<int, Map>());

	public List<Building_ABStairs> Stairs => stairsList ?? (stairsList = new List<Building_ABStairs>());

	public bool HasMultiLevels => mapByLevel != null && mapByLevel.Count > 1;

	private static void NoteSkyLevel(int delta)
	{
		Game game = Current.Game;
		if (game != null)
		{
			if (skyGame == null || skyGame.Target != game)
			{
				skyGame = new WeakReference(game);
				skyCount = 0;
			}
			skyCount = Math.Max(0, skyCount + delta);
		}
	}

	public void RegisterStairs(Building_ABStairs stairs)
	{
		if (stairs != null && !Stairs.Contains(stairs))
		{
			Stairs.Add(stairs);
		}
	}

	public void DeregisterStairs(Building_ABStairs stairs)
	{
		stairsList?.Remove(stairs);
	}

	public LevelComp(Map map)
		: base(map)
	{
		LevelMapGen.Context currentContext = LevelMapGen.CurrentContext;
		if (currentContext != null)
		{
			level = currentContext.levelToGenerate;
			groundMap = currentContext.groundMap;
			if (level == 1)
			{
				lowerMap = currentContext.groundMap;
			}
			else if (level == -1)
			{
				upperMap = currentContext.groundMap;
			}
		}
	}

	public override void FinalizeInit()
	{
		((MapComponent)this).FinalizeInit();
		if (level == 0 && groundMap == null)
		{
			groundMap = base.map;
		}
		if (level == 1)
		{
			NoteSkyLevel(1);
		}
		TrySubscribeSync();
		if (level == 1)
		{
			LevelSync.ReconcileRooftops(base.map);
		}
	}

	private static int Stagger(Map map, int interval)
	{
		return map.uniqueID % interval;
	}

	private bool Due(ref int due, int now, int interval)
	{
		if (due < 0)
		{
			due = now + Stagger(base.map, interval) + 1;
			return false;
		}
		if (now < due)
		{
			return false;
		}
		due = now + interval;
		return true;
	}

	public override void MapComponentUpdate()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		((MapComponent)this).MapComponentUpdate();
		if (!ABGuard.On(ABGuard.Ui) || base.map != Find.CurrentMap || (int)Current.ProgramState != 2)
		{
			return;
		}
		try
		{
			CrossLevelCombatUI.DrawEngagementVisuals(base.map);
			CrossLevelTurret.DrawVisuals(base.map);
			ABBelowGotoDrag.FrameUpdate(base.map);
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "cross level engagement visuals");
		}
	}

	public override void MapComponentTick()
	{
		int ticksGame = Find.TickManager.TicksGame;
		bool flag = base.map == Find.CurrentMap;
		if (flag && !wasVisible && level == 1)
		{
			nextWeatherDue = 0;
			if (sweepCursor < 0)
			{
				sweepCursor = 0;
			}
			if (!belowPrintsHealed && ABGuard.On(ABGuard.Rendering) && LevelRenderer.DrawerReady(base.map))
			{
				belowPrintsHealed = true;
				try
				{
					base.map.mapDrawer.WholeMapChanged(MapMeshFlagDef.op_Implicit(ABDefOf.AB_BelowThings));
					ABLog.Dev("Below-print convergence pass for sky map " + base.map.uniqueID + ".");
				}
				catch (Exception e)
				{
					ABGuard.Disable(ABGuard.Rendering, e, "below print convergence");
				}
			}
		}
		wasVisible = flag;
		if (level != 0 && !syncSubscribed && ticksGame % 250 == 0)
		{
			TrySubscribeSync();
		}
		if (level != 0 && ABGuard.On(ABGuard.HostileMove) && Due(ref nextHostileDue, ticksGame, 250))
		{
			try
			{
				HostileDescend.ScanPocketMap(this);
			}
			catch (Exception e2)
			{
				ABGuard.Disable(ABGuard.HostileMove, e2, "hostile descend scan");
			}
		}
		if (level == 1)
		{
			if (ABGuard.On(ABGuard.Combat))
			{
				try
				{
					Map val = lowerMap ?? groundMap;
					if (val != null && !val.Disposed)
					{
						CrossLevelTurret.TickPair(base.map, val);
					}
				}
				catch (Exception e3)
				{
					ABGuard.Disable(ABGuard.Combat, e3, "cross level turret tick");
				}
			}
			if (ABGuard.On(ABGuard.Combat) && Due(ref nextAutoEngageDue, ticksGame, 250))
			{
				try
				{
					Map val2 = lowerMap ?? groundMap;
					if (val2 != null && !val2.Disposed && (base.map.mapPawns.AllPawnsSpawned.Count > 0 || val2.mapPawns.AllPawnsSpawned.Count > 0))
					{
						CrossLevelAutoEngage.ScanPair(base.map, val2);
					}
				}
				catch (Exception e4)
				{
					ABGuard.Disable(ABGuard.Combat, e4, "cross gap auto engage scan");
				}
			}
			int interval = (flag ? 150 : 600);
			if (ABGuard.On(ABGuard.Weather) && Due(ref nextWeatherDue, ticksGame, interval))
			{
				try
				{
					Map val3 = lowerMap ?? groundMap;
					if (val3 != null && !val3.Disposed)
					{
						WeatherDef curWeather = val3.weatherManager.curWeather;
						if (curWeather != null && base.map.weatherManager.curWeather != curWeather)
						{
							base.map.weatherManager.TransitionTo(curWeather);
						}
					}
				}
				catch (Exception e5)
				{
					ABGuard.Disable(ABGuard.Weather, e5, "weather sync");
				}
			}
			int interval2 = (flag ? 2000 : 10000);
			if (sweepCursor < 0 && Due(ref nextSweepDue, ticksGame, interval2))
			{
				sweepCursor = 0;
			}
			if (sweepCursor >= 0 && !LevelSync.ReconcileRooftopsSlice(base.map, ref sweepCursor, 4096))
			{
				sweepCursor = -1;
			}
		}
		else
		{
			if (level != 0 || stairsList == null || stairsList.Count == 0)
			{
				return;
			}
			if (ABGuard.On(ABGuard.HostileMove) && Due(ref nextGroundHostileDue, ticksGame, 250))
			{
				try
				{
					HostileDescend.ScanGroundHostiles(this);
				}
				catch (Exception e6)
				{
					ABGuard.Disable(ABGuard.HostileMove, e6, "hostile ascent scan");
				}
			}
			if (ABGuard.On(ABGuard.HostileMove) && lowerMap != null && Due(ref nextAnimalDue, ticksGame, 1200))
			{
				try
				{
					CrossLevelAnimals.ScanSurfaceAmbient(this);
				}
				catch (Exception e7)
				{
					ABGuard.Disable(ABGuard.HostileMove, e7, "animal wander scan");
				}
			}
			if (ABGuard.On(ABGuard.Pipes) && Due(ref nextPipesDue, ticksGame, 100))
			{
				try
				{
					ABPipeCompat.TickGroundPairs(this);
				}
				catch (Exception e8)
				{
					ABGuard.Disable(ABGuard.Pipes, e8, "pipe network bridge");
				}
			}
		}
	}

	private void TrySubscribeSync()
	{
		if (syncSubscribed || level == 0 || base.map.events == null)
		{
			return;
		}
		Map self = base.map;
		if (level == 1)
		{
			Map ground = lowerMap ?? groundMap;
			if (ground == null || ground.events == null)
			{
				return;
			}
			roofChangedHandler = delegate(IntVec3 c)
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				LevelSync.OnGroundRoofChanged(ground, c);
			};
			terrainChangedHandler = delegate(IntVec3 c)
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				LevelSync.OnSkyTerrainChanged(self, c);
			};
			thingSpawnedHandler = delegate(Thing t)
			{
				LevelSync.OnSkyThingSpawned(self, t);
			};
			ground.events.RoofChanged += roofChangedHandler;
			base.map.events.TerrainChanged += terrainChangedHandler;
			base.map.events.ThingSpawned += thingSpawnedHandler;
		}
		thingDespawnedHandler = delegate(Thing t)
		{
			LevelSync.OnLevelMineableDespawned(self, t);
		};
		base.map.events.ThingDespawned += thingDespawnedHandler;
		syncSubscribed = true;
		ABLog.Dev("Level sync subscribed for map " + base.map.uniqueID + " (level " + level + ").");
		if (level != 1)
		{
			return;
		}
		try
		{
			Map obj = lowerMap ?? groundMap;
			if (obj != null)
			{
				MapDrawer mapDrawer = obj.mapDrawer;
				if (mapDrawer != null)
				{
					mapDrawer.WholeMapChanged(MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Roofs));
				}
			}
		}
		catch (Exception ex)
		{
			ABLog.Dev("Ceiling hint load nudge skipped: " + ex.Message);
		}
	}

	private void UnsubscribeSync()
	{
		if (!syncSubscribed)
		{
			return;
		}
		Map val = lowerMap ?? groundMap;
		if (val?.events != null && roofChangedHandler != null)
		{
			val.events.RoofChanged -= roofChangedHandler;
		}
		if (base.map?.events != null)
		{
			if (terrainChangedHandler != null)
			{
				base.map.events.TerrainChanged -= terrainChangedHandler;
			}
			if (thingSpawnedHandler != null)
			{
				base.map.events.ThingSpawned -= thingSpawnedHandler;
			}
			if (thingDespawnedHandler != null)
			{
				base.map.events.ThingDespawned -= thingDespawnedHandler;
			}
		}
		syncSubscribed = false;
	}

	public override void MapRemoved()
	{
		((MapComponent)this).MapRemoved();
		UnsubscribeSync();
		if (level != 0)
		{
			ABApi.NotifyLevelRemoved(base.map);
		}
		if (level == 1)
		{
			NoteSkyLevel(-1);
		}
		if (stairsList != null)
		{
			for (int i = 0; i < stairsList.Count; i++)
			{
				stairsList[i]?.SeverLink();
			}
			stairsList.Clear();
		}
		Map val = groundMap;
		if (val != null && !val.Disposed && val != base.map)
		{
			val.Levels()?.CleanupInvalidMaps();
		}
	}

	public void AddLevel(int lvl, Map newMap)
	{
		if (MapByLevel.ContainsKey(lvl))
		{
			Log.Warning("[As above, So below] Level " + lvl + " already registered on this column, replacing.");
			MapByLevel.Remove(lvl);
		}
		MapByLevel.Add(lvl, newMap);
	}

	public void CleanupInvalidMaps()
	{
		if (mapByLevel != null)
		{
			GenCollection.RemoveAll<int, Map>(mapByLevel, (Predicate<KeyValuePair<int, Map>>)((KeyValuePair<int, Map> kvp) => kvp.Value == null || kvp.Value.Disposed));
		}
		if (upperMap != null && upperMap.Disposed)
		{
			upperMap = null;
		}
		if (lowerMap != null && lowerMap.Disposed)
		{
			lowerMap = null;
		}
	}

	public override void ExposeData()
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Invalid comparison between Unknown and I4
		((MapComponent)this).ExposeData();
		Scribe_Values.Look<int>(ref level, "AB_level", 0, false);
		Scribe_References.Look<Map>(ref upperMap, "AB_upperMap", false);
		Scribe_References.Look<Map>(ref lowerMap, "AB_lowerMap", false);
		Scribe_References.Look<Map>(ref groundMap, "AB_groundMap", false);
		Scribe_Collections.Look<int, Map>(ref mapByLevel, "AB_mapByLevel", (LookMode)1, (LookMode)3, ref tmpLevels, ref tmpMaps, true, false, false);
		if ((int)Scribe.mode == 4)
		{
			CleanupInvalidMaps();
		}
	}
}

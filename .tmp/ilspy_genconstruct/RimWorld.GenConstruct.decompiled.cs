using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public static class GenConstruct
{
	private readonly struct ResourcesKey : IEquatable<ResourcesKey>
	{
		private readonly int mapId;

		private readonly int pawnId;

		private readonly BuildableDef entityToBuild;

		private readonly ThingDef stuff;

		private readonly int neededHash;

		private readonly bool forced;

		public extern ResourcesKey(Pawn pawn, Thing thing, IConstructible constructible, int neededHash, bool forced);

		public override extern bool Equals(object obj);

		public override extern int GetHashCode();

		public extern bool Equals(ResourcesKey other);
	}

	public const float ConstructionSpeedGlobalFactor = 1.7f;

	private static string SkillTooLowTrans;

	private static string IncapableOfDeconstruction;

	private static string IncapableOfMining;

	private static string TreeMarkedForExtraction;

	private static readonly Dictionary<ResourcesKey, bool> canGetResourcesCache;

	private static int canGetResourcesCacheTick;

	private static readonly List<string> tmpIdeoMemberNames;

	public static extern void Reset();

	public static extern bool CanGetResources(Thing thing, Pawn pawn);

	public static extern bool CanGetResources_NewTemp(Thing thing, Pawn pawn, bool forced = false);

	private static extern int NeededNow(IConstructible constructible, ThingDef need, Pawn pawn);

	private static extern int NeededNow_NewTemp(IConstructible constructible, ThingDef need, Pawn pawn, bool forced = false);

	public static extern Blueprint_Build PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff, Precept_ThingStyle styleSource = null, ThingStyleDef styleDef = null, bool sendBPSpawnedSignal = true);

	public static extern Blueprint_Install PlaceBlueprintForInstall(MinifiedThing itemToInstall, IntVec3 center, Map map, Rot4 rotation, Faction faction, bool sendBPSpawnedSignal = true);

	public static extern Blueprint_Install PlaceBlueprintForReinstall(Building buildingToReinstall, IntVec3 center, Map map, Rot4 rotation, Faction faction, bool sendBPSpawnedSignal = true);

	public static extern bool CanBuildOnTerrain(BuildableDef entDef, IntVec3 c, Map map, Rot4 rot, Thing thingToIgnore = null, ThingDef stuffDef = null);

	public static extern Thing MiniToInstallOrBuildingToReinstall(Blueprint b);

	public static extern bool CanConstruct(Thing t, Pawn pawn, WorkTypeDef workType, bool forced = false, JobDef jobForReservation = null);

	public static extern bool CanConstruct(Thing t, Pawn p, bool checkSkills = true, bool forced = false, JobDef jobForReservation = null);

	public static extern AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null, Thing thing = null, ThingDef stuffDef = null, bool ignoreEdgeArea = false, bool ignoreInteractionSpots = false, bool ignoreClearableFreeBuildings = false);

	public static extern AcceptanceReport CanPlaceBlueprintAt_NewTemp(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null, Thing thing = null, ThingDef stuffDef = null, bool ignoreEdgeArea = false, bool ignoreInteractionSpots = false, bool ignoreClearableFreeBuildings = false, Predicate<Thing> skipBlockingThing = null);

	public static extern bool CanReplace(BuildableDef placing, BuildableDef existing, ThingDef placingStuff = null, ThingDef existingStuff = null);

	public static extern bool HasMatchingReplacementTag(ThingDef a, ThingDef b);

	public static extern AcceptanceReport InteractionCellStandable(ThingDef thingDef, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null);

	public static extern AcceptanceReport NotBlockingAnyInteractionCells(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null);

	public static extern BuildableDef BuiltDefOf(ThingDef def);

	public static extern bool CanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef, ThingDef newStuff = null, ThingDef oldStuff = null);

	public static extern Thing FirstBlockingThing(Thing constructible, Pawn pawnToIgnore);

	public static extern bool CanTouchTargetFromValidCell(Thing constructible, Pawn worker);

	public static extern Job HandleBlockingThingJob(Thing constructible, Pawn worker, bool forced = false);

	public static extern bool BlocksConstruction(Thing constructible, Thing t);

	private static extern ThingDef BlueprintDefOf(Thing constructible);

	public static extern bool TerrainCanSupport(CellRect rect, Map map, ThingDef thing);

	public static extern List<Thing> GetAttachedBuildings(Thing thing);

	public static extern Thing GetWallAttachedTo(Thing thing);

	public static extern Thing GetWallAttachedTo(IntVec3 pos, Rot4 rot, Map map);
}

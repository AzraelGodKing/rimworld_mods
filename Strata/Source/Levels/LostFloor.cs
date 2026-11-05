using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Strata
{
    // A lost floor is a sealed set of rooms deep in the rock, left by people
    // who dug down long ago. Their diaries tell how their mining noise drew
    // the insects that ended them. The rooms are fogged and walled in by
    // rock, so the colony finds them only by digging into them.
    public static class LostFloor
    {
        public const int MinDepth = 2;

        public const float Chance = 0.08f;

        public const int DiscoverIntervalTicks = 250;

        public const int DiaryEntries = 5;

        private const float MinDistFromLanding = 45f;
        private const int EdgeMargin = 16;
        private const float MinChamberRadius = 4.2f;
        private const float MaxChamberRadius = 5.6f;
        private const int PlanTries = 6;

        public static bool Enabled => StrataMod.Settings?.specialLevelsEnabled ?? true;

        internal static void PlaceFeatures(Map map, MapComponent_StrataLevelKind comp)
        {
            IntVec3 landing = MagmaLayer.FindLanding(map);
            if (!TryPlan(map, landing, out StrataCavernPlan.Result plan))
            {
                return;
            }

            TerrainDef floor = AncientFloor(map);
            int width = map.Size.x;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!plan.carveMask[cell.z * width + cell.x])
                {
                    continue;
                }
                cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
                if (floor != null)
                {
                    map.terrainGrid.SetTerrain(cell, floor);
                }
            }

            List<IntVec3> rooms = plan.chambers;
            comp.SetLostAnchor(rooms[0]);
            PlaceDiaries(map, rooms);
            PlaceRemains(map, rooms);
            int depth = StrataDepth.Of(map);
            float points = Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(Find.World) * (0.5f + 0.25f * depth), 300f, 3000f);
            PlaceHives(map, rooms, points);
            PlaceLoot(map, rooms[rooms.Count - 1], points * 1.5f);
        }

        // Rooms only go where there is still solid rock, so the floor stays
        // sealed until a miner breaks in.
        private static bool TryPlan(Map map, IntVec3 landing, out StrataCavernPlan.Result plan)
        {
            int width = map.Size.x;
            for (int attempt = 0; attempt < PlanTries; attempt++)
            {
                var start = new IntVec3(
                    Rand.RangeInclusive(EdgeMargin, map.Size.x - 1 - EdgeMargin), 0,
                    Rand.RangeInclusive(EdgeMargin, map.Size.z - 1 - EdgeMargin));
                if (start.DistanceTo(landing) < MinDistFromLanding)
                {
                    continue;
                }
                int seed = Rand.Int;
                plan = StrataCavernPlan.PlanVault(map.Size.x, map.Size.z, start,
                    Rand.RangeInclusive(3, 4), MinChamberRadius, MaxChamberRadius, EdgeMargin, seed);
                if (plan.chambers == null || plan.chambers.Count < 3 || !Sealed(map, plan.carveMask, width))
                {
                    continue;
                }
                return true;
            }
            plan = default;
            return false;
        }

        // Every planned cell and its neighbours must still be rock.
        private static bool Sealed(Map map, bool[] mask, int width)
        {
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!mask[cell.z * width + cell.x])
                {
                    continue;
                }
                foreach (IntVec3 offset in GenAdj.AdjacentCellsAndInside)
                {
                    IntVec3 c = cell + offset;
                    if (!c.InBounds(map) || c.GetFirstMineable(map) == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // The host rock's smoothed floor: worked stone, not raw cave.
        private static TerrainDef AncientFloor(Map map)
        {
            TerrainDef natural = StrataRockUtility.NaturalFloorFor(StrataRockUtility.PrimaryRock(map));
            return natural?.smoothedTerrain ?? natural;
        }

        private static void PlaceDiaries(Map map, List<IntVec3> rooms)
        {
            for (int entry = 1; entry <= DiaryEntries; entry++)
            {
                IntVec3 room = rooms[(entry - 1) % rooms.Count];
                if (!TryFindStandable(map, room, out IntVec3 cell))
                {
                    continue;
                }
                Thing diary = ThingMaker.MakeThing(StrataLostFloorDefOf.Strata_LostDiary);
                diary.TryGetComp<CompLostDiary>()?.SetEntry(entry);
                GenPlace.TryPlaceThing(diary, cell, map, ThingPlaceMode.Near);
            }
        }

        private static void PlaceRemains(Map map, List<IntVec3> rooms)
        {
            Faction ancients = Faction.OfAncients;
            int count = Rand.RangeInclusive(3, 5);
            for (int i = 0; i < count; i++)
            {
                IntVec3 room = rooms[Rand.Range(0, rooms.Count)];
                if (!TryFindStandable(map, room, out IntVec3 cell))
                {
                    continue;
                }
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Villager, ancients, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false));
                GenSpawn.Spawn(pawn, cell, map);
                pawn.Kill(null);
                CompRottable rot = pawn.Corpse?.TryGetComp<CompRottable>();
                if (rot != null)
                {
                    rot.RotProgress += 10000000f;
                }
            }
        }

        // What ended them is still here.
        private static void PlaceHives(Map map, List<IntVec3> rooms, float points)
        {
            Faction insects = Faction.OfInsects;
            if (insects == null)
            {
                return;
            }
            int hives = Mathf.Clamp(Mathf.RoundToInt(points / 500f), 1, 4);
            for (int i = 0; i < hives; i++)
            {
                IntVec3 room = rooms[1 + Rand.Range(0, rooms.Count - 1)];
                if (TryFindStandable(map, room, out IntVec3 cell))
                {
                    Hive hive = (Hive)GenSpawn.Spawn(ThingDefOf.Hive, cell, map);
                    hive.SetFaction(insects);
                }
            }
            var spawned = new List<Pawn>();
            float budget = points;
            for (int guard = 0; guard < 60 && budget > 0f; guard++)
            {
                PawnKindDef kind = Rand.Chance(0.6f) ? PawnKindDefOf.Megascarab : PawnKindDefOf.Spelopede;
                if (kind == null || (kind.combatPower > budget && spawned.Count > 0))
                {
                    break;
                }
                IntVec3 room = rooms[1 + Rand.Range(0, rooms.Count - 1)];
                if (!TryFindStandable(map, room, out IntVec3 cell))
                {
                    continue;
                }
                Pawn pawn = PawnGenerator.GeneratePawn(kind, insects);
                GenSpawn.Spawn(pawn, cell, map);
                spawned.Add(pawn);
                budget -= kind.combatPower;
            }
            if (spawned.Count > 0)
            {
                LordMaker.MakeNewLord(insects, new LordJob_DefendAndExpandHive(), map, spawned);
            }
        }

        private static void PlaceLoot(Map map, IntVec3 room, float marketValue)
        {
            ThingSetMakerParams parms = default;
            parms.totalMarketValueRange = new FloatRange(marketValue * 0.85f, marketValue * 1.15f);
            parms.qualityGenerator = QualityGenerator.Reward;
            foreach (Thing thing in ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate(parms))
            {
                if (TryFindStandable(map, room, out IntVec3 cell))
                {
                    GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
                }
            }
        }

        internal static void TickDiscover(Map map, MapComponent_StrataLevelKind comp)
        {
            IntVec3 anchor = comp.LostAnchor;
            if (!anchor.IsValid || comp.LostFound || !anchor.InBounds(map) || anchor.Fogged(map))
            {
                return;
            }
            comp.MarkLostFound();
            Find.LetterStack.ReceiveLetter(
                "Strata_LostFloorLetterTitle".Translate(),
                "Strata_LostFloorLetterText".Translate(),
                LetterDefOf.ThreatSmall,
                new LookTargets(anchor, map));
        }

        private static bool TryFindStandable(Map map, IntVec3 near, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomCellNear(near, map, 4,
                c => c.Standable(map) && c.GetFirstItem(map) == null, out cell);
        }
    }

    [DefOf]
    public static class StrataLostFloorDefOf
    {
        public static ThingDef Strata_LostDiary;

        static StrataLostFloorDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataLostFloorDefOf));
        }
    }

    // One page of the lost floor's story. Select it and press Read.
    public class CompLostDiary : ThingComp
    {
        private int entry = 1;

        internal void SetEntry(int value)
        {
            entry = Mathf.Clamp(value, 1, LostFloor.DiaryEntries);
        }

        private string TitleKey => "Strata_LostDiary_Title" + entry;

        private string TextKey => "Strata_LostDiary_Entry" + entry;

        public override string TransformLabel(string label)
        {
            return label + " (" + TitleKey.Translate() + ")";
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "Strata_LostDiary_Read".Translate(),
                defaultDesc = "Strata_LostDiary_ReadDesc".Translate(),
                icon = parent.def.uiIcon,
                action = () => Find.WindowStack.Add(new Dialog_MessageBox(
                    TextKey.Translate(), title: TitleKey.Translate())),
            };
        }

        public override bool AllowStackWith(Thing other)
        {
            return false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref entry, "strataDiaryEntry", 1);
        }
    }
}

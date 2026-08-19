using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Stable defName lists for Homesteader systems and sibling soft-compat.
    /// HS-S03 (Deep Colony meals) and HS-S04 (Nemesis pantry targets) live here.
    /// </summary>
    public static class HomesteaderPantry
    {
        public static readonly string[] LarderBuildingDefNames =
        {
            "Homesteader_RootCellar",
            "Homesteader_Icehouse",
            "Homesteader_Springhouse",
            "Homesteader_PreservesShelf",
        };

        public static readonly string[] PreservedFoodDefNames =
        {
            "Homesteader_Jerky",
            "Homesteader_DriedProduce",
            "Homesteader_FruitLeather",
            "Homesteader_DriedMushrooms",
            "Homesteader_SmokedMeat",
            "Homesteader_SaltedMeat",
            "Homesteader_SaltedFish",
            "Homesteader_PickledVegetables",
            "Homesteader_Jam",
            "Homesteader_CannedJam",
            "Homesteader_CannedStew",
            "Homesteader_Cheese",
            "Homesteader_WaxedCheese",
            "Homesteader_SmokedCheese",
            "Homesteader_Cider",
            "Homesteader_Honey",
            "Homesteader_MapleSyrup",
            "Pemmican",
        };

        public static readonly string[] MealDefNames =
        {
            "Homesteader_ToastAndJam",
            "Homesteader_PloughmansLunch",
            "Homesteader_HoneyPorridge",
            "Homesteader_PumpkinPie",
            "Homesteader_ButtermilkBiscuits",
            "Homesteader_TrailStew",
            "Homesteader_HeartyStew",
            "Homesteader_Bread",
            "Homesteader_Flapjacks",
        };

        public static readonly string[] NemesisTargetBuildingDefNames =
        {
            "Homesteader_RootCellar",
            "Homesteader_Icehouse",
            "Homesteader_Springhouse",
            "Homesteader_Smokehouse",
            "Homesteader_Farmstand",
            "Homesteader_PreservesShelf",
        };

        public static readonly string[] AgeableDefNames =
        {
            "Homesteader_WaxedCheese",
            "Homesteader_Cider",
            "Homesteader_SmokedMeat",
        };

        public static bool IsLarderBuilding(ThingDef def) => Contains(LarderBuildingDefNames, def?.defName);

        public static bool IsPreservedFood(ThingDef def) => Contains(PreservedFoodDefNames, def?.defName);

        private static bool Contains(string[] names, string defName)
        {
            if (defName == null)
            {
                return false;
            }

            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == defName)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class MapComponent_HomesteaderPantry : MapComponent
    {
        private const int RebuildInterval = 2500;

        public int DistinctPreservedKinds { get; private set; }

        public MapComponent_HomesteaderPantry(Map map) : base(map)
        {
        }

        public override void FinalizeInit()
        {
            Rebuild();
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % RebuildInterval != 0)
            {
                return;
            }

            Rebuild();
        }

        public void Rebuild()
        {
            HashSet<string> kinds = new HashSet<string>();
            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < buildings.Count; i++)
            {
                Thing b = buildings[i];
                if (b?.def == null || !HomesteaderPantry.IsLarderBuilding(b.def))
                {
                    continue;
                }

                foreach (IntVec3 cell in b.OccupiedRect())
                {
                    List<Thing> things = map.thingGrid.ThingsListAt(cell);
                    if (things == null)
                    {
                        continue;
                    }

                    for (int t = 0; t < things.Count; t++)
                    {
                        Thing item = things[t];
                        if (item != null && HomesteaderPantry.IsPreservedFood(item.def))
                        {
                            kinds.Add(item.def.defName);
                        }
                    }
                }
            }

            DistinctPreservedKinds = kinds.Count;
        }
    }

    public class ThoughtWorker_WellStockedLarder : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p?.Map == null || !p.IsColonist || HomesteaderMod.Settings == null
                || !HomesteaderMod.Settings.larderMoodEnabled)
            {
                return ThoughtState.Inactive;
            }

            MapComponent_HomesteaderPantry pantry = p.Map.GetComponent<MapComponent_HomesteaderPantry>();
            if (pantry == null)
            {
                return ThoughtState.Inactive;
            }

            int n = pantry.DistinctPreservedKinds;
            if (n >= 9)
            {
                return ThoughtState.ActiveAtStage(2);
            }

            if (n >= 6)
            {
                return ThoughtState.ActiveAtStage(1);
            }

            if (n >= 3)
            {
                return ThoughtState.ActiveAtStage(0);
            }

            return ThoughtState.Inactive;
        }
    }
}

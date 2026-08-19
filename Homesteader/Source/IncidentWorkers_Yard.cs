using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    public class IncidentWorker_FoxOnCoop : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map
                && map.IsPlayerHome
                && FindCoop(map) != null
                && !HasScarecrow(map);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Building coop = FindCoop(map);
            if (coop == null)
            {
                return false;
            }

            int ruined = RuinNearbyEggs(map, coop.Position);
            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_FoxRaid");
            if (thought != null)
            {
                List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    pawns[i].needs?.mood?.thoughts.memories.TryGainMemory(thought);
                }
            }

            Find.LetterStack.ReceiveLetter(
                def.letterLabel,
                ruined > 0
                    ? "Homesteader_FoxLetterEggs".Translate(ruined)
                    : "Homesteader_FoxLetterEmpty".Translate(),
                def.letterDef ?? LetterDefOf.NegativeEvent,
                coop);
            return true;
        }

        private static Building FindCoop(Map map)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_ChickenCoop");
            if (def == null)
            {
                return null;
            }

            List<Building> list = map.listerBuildings.AllBuildingsColonistOfDef(def);
            return list != null && list.Count > 0 ? list[0] : null;
        }

        private static bool HasScarecrow(Map map)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_Scarecrow");
            if (def == null)
            {
                return false;
            }

            List<Building> list = map.listerBuildings.AllBuildingsColonistOfDef(def);
            return list != null && list.Count > 0;
        }

        private static int RuinNearbyEggs(Map map, IntVec3 near)
        {
            ThingDef eggs = DefDatabase<ThingDef>.GetNamedSilentFail("EggChickenUnfertilized");
            if (eggs == null)
            {
                return 0;
            }
            int ruined = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(near, 8, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = map.thingGrid.ThingsListAt(cell);
                for (int i = things.Count - 1; i >= 0 && ruined < 18; i--)
                {
                    Thing t = things[i];
                    if (t.def != eggs)
                    {
                        continue;
                    }

                    int lose = Mathf.Clamp(t.stackCount, 1, 8);
                    t.SplitOff(lose).Destroy(DestroyMode.Vanish);
                    ruined += lose;
                }
            }

            return ruined;
        }
    }

    public class IncidentWorker_CountyFair : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Map map) || !map.IsPlayerHome)
            {
                return false;
            }

            MapComponent_HomesteaderPantry pantry = map.GetComponent<MapComponent_HomesteaderPantry>();
            pantry?.Rebuild();
            int kinds = pantry?.DistinctPreservedKinds ?? 0;
            float brand = GameComponent_HomesteaderYard.Get()?.brand ?? 0f;
            return kinds >= 3 || brand >= 15f;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            MapComponent_HomesteaderPantry pantry = map.GetComponent<MapComponent_HomesteaderPantry>();
            pantry?.Rebuild();
            int kinds = pantry?.DistinctPreservedKinds ?? 0;
            float brand = GameComponent_HomesteaderYard.Get()?.brand ?? 0f;
            bool prize = kinds >= 6 || brand >= 40f;

            if (prize)
            {
                int silver = 40 + kinds * 8;
                Thing money = ThingMaker.MakeThing(ThingDefOf.Silver);
                money.stackCount = silver;
                IntVec3 drop = DropCellFinder.TradeDropSpot(map);
                GenPlace.TryPlaceThing(money, drop, map, ThingPlaceMode.Near);
                ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_CountyFairPrize");
                Give(map, thought);
                GameComponent_HomesteaderYard.Get()?.AddBrand(4f);
                Find.LetterStack.ReceiveLetter(
                    def.letterLabel,
                    "Homesteader_FairPrizeText".Translate(silver, kinds),
                    LetterDefOf.PositiveEvent);
            }
            else
            {
                ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_CountyFairShrug");
                Give(map, thought);
                Find.LetterStack.ReceiveLetter(
                    def.letterLabel,
                    "Homesteader_FairShrugText".Translate(kinds),
                    LetterDefOf.NeutralEvent);
            }

            return true;
        }

        private static void Give(Map map, ThoughtDef thought)
        {
            if (thought == null)
            {
                return;
            }

            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                pawns[i].needs?.mood?.thoughts.memories.TryGainMemory(thought);
            }
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Homesteader
{
    public class GameComponent_HomesteaderYard : GameComponent
    {
        public int lastFestivalYear = -1;
        public int festivalUntilTick;
        public float brand;
        public int lastLwLetterTick = -999999;
        public Dictionary<int, int> prizeQualityByThingId = new Dictionary<int, int>();

        public GameComponent_HomesteaderYard(Game game)
        {
        }

        public static GameComponent_HomesteaderYard Get()
        {
            return Current.Game?.GetComponent<GameComponent_HomesteaderYard>();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref lastFestivalYear, "lastFestivalYear", -1);
            Scribe_Values.Look(ref festivalUntilTick, "festivalUntilTick");
            Scribe_Values.Look(ref brand, "homesteadBrand");
            Scribe_Values.Look(ref lastLwLetterTick, "lastLwLetterTick", -999999);
            Scribe_Collections.Look(ref prizeQualityByThingId, "prizeQualityByThingId", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                prizeQualityByThingId ??= new Dictionary<int, int>();
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 2000 != 0)
            {
                return;
            }

            if (HomesteaderMod.Settings == null || !HomesteaderMod.Settings.harvestFestivalEnabled)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (!map.IsPlayerHome)
                {
                    continue;
                }

                TryStartFestival(map);
            }
        }

        public bool FestivalActive => Find.TickManager.TicksGame < festivalUntilTick;

        public void TryStartFestival(Map map, bool force = false)
        {
            int year = GenDate.Year(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(map.Tile).x);
            Season season = GenDate.Season(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(map.Tile));
            if (!force)
            {
                if (season != Season.Fall || lastFestivalYear >= year)
                {
                    return;
                }
            }

            ThingDef maypoleDef = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_HarvestMaypole");
            if (maypoleDef == null)
            {
                return;
            }

            List<Building> poles = map.listerBuildings.AllBuildingsColonistOfDef(maypoleDef);
            if (poles == null || poles.Count == 0)
            {
                return;
            }

            Building pole = poles[0];
            if (!force)
            {
                lastFestivalYear = year;
            }

            festivalUntilTick = Find.TickManager.TicksGame + GenDate.TicksPerDay;
            GiveFestivalThoughts(map);
            SpawnFestivalFood(map, pole.Position);
            TryStartGathering(map, pole);
            Find.LetterStack.ReceiveLetter(
                "Homesteader_FestivalLetterLabel".Translate(),
                "Homesteader_FestivalLetterText".Translate(),
                LetterDefOf.PositiveEvent,
                pole);
        }

        public int GetPrizeQuality(Thing building)
        {
            if (building == null)
            {
                return 1;
            }

            if (!prizeQualityByThingId.TryGetValue(building.thingIDNumber, out int q) || q < 1)
            {
                q = 1;
                prizeQualityByThingId[building.thingIDNumber] = q;
            }

            return Mathf.Clamp(q, 1, 3);
        }

        public void BumpPrizeQuality(Thing building)
        {
            if (building == null)
            {
                return;
            }

            int q = GetPrizeQuality(building);
            if (q < 3)
            {
                prizeQualityByThingId[building.thingIDNumber] = q + 1;
            }
        }

        public void AddBrand(float amount)
        {
            brand = Mathf.Clamp(brand + amount, 0f, 100f);
        }

        private static void GiveFestivalThoughts(Map map)
        {
            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_HarvestFestival");
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

        private static void SpawnFestivalFood(Map map, IntVec3 near)
        {
            string[] extras =
            {
                "Homesteader_Jam",
                "Homesteader_Cider",
                "Homesteader_WaxedCheese",
            };
            for (int i = 0; i < extras.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(extras[i]);
                if (def == null)
                {
                    continue;
                }

                Thing thing = ThingMaker.MakeThing(def);
                thing.stackCount = Mathf.Clamp(def.stackLimit / 15, 5, 20);
                if (GenPlace.TryPlaceThing(thing, near, map, ThingPlaceMode.Near))
                {
                    continue;
                }

                thing.Destroy();
            }
        }

        private static void TryStartGathering(Map map, Building pole)
        {
            List<Pawn> dancers = new List<Pawn>();
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count && dancers.Count < 8; i++)
            {
                Pawn p = colonists[i];
                if (p.Downed || p.Drafted || p.InMentalState || !p.mindState.IsIdle)
                {
                    continue;
                }

                dancers.Add(p);
            }

            if (dancers.Count == 0)
            {
                return;
            }

            for (int i = 0; i < dancers.Count; i++)
            {
                Job job = JobMaker.MakeJob(JobDefOf.Goto, pole);
                job.locomotionUrgency = LocomotionUrgency.Walk;
                dancers[i].jobs?.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }
    }
}

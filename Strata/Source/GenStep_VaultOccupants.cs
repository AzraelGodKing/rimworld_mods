using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Strata
{
    // Populates a sealed vault with mechanoids when that faction exists, else
    // insects, and places a richer hoard in the deepest chamber.
    public class GenStep_VaultOccupants : GenStep
    {
        private const float LootMultiplier = 1.75f;

        private static readonly string[] InsectKindNames = { "Megaspider", "Spelopede", "Megascarab" };

        private static readonly string[] MechanoidKindNames = { "Mech_Scyther", "Mech_Pikeman", "Mech_Lancer" };

        public override int SeedPart => 1289047314;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!MapGenerator.TryGetVar(GenStep_CarveWarren.ChambersVar, out List<IntVec3> chambers)
                || chambers.Count < 2)
            {
                return;
            }

            float points = ResolveThreatPoints(parms);
            IntVec3 lootChamber = chambers[chambers.Count - 1];
            Faction mechanoids = Faction.OfMechanoids;
            if (mechanoids != null)
            {
                SpawnMechanoids(map, chambers, mechanoids, points);
            }
            else
            {
                SpawnInsects(map, chambers, points);
            }
            SpawnLoot(map, lootChamber, points * LootMultiplier);
        }

        private static float ResolveThreatPoints(GenStepParams parms)
        {
            float points = parms.sitePart?.parms?.threatPoints ?? 0f;
            if (points <= 0f)
            {
                points = StorytellerUtility.DefaultThreatPointsNow(Find.World);
            }
            return Mathf.Clamp(points, 400f, 3000f);
        }

        private static void SpawnMechanoids(Map map, List<IntVec3> chambers, Faction mechanoids, float points)
        {
            var kinds = ResolveKinds(MechanoidKindNames);
            if (kinds.Count == 0)
            {
                SpawnInsects(map, chambers, points);
                return;
            }

            var spawned = new List<Pawn>();
            float budget = points;
            int guard = 0;
            while (budget > 0f && guard++ < 60)
            {
                PawnKindDef kind = kinds.RandomElement();
                if (kind.combatPower > budget && spawned.Count > 0)
                {
                    break;
                }
                IntVec3 chamber = chambers[Rand.RangeInclusive(1, chambers.Count - 1)];
                if (!TryFindStandable(map, chamber, out IntVec3 cell))
                {
                    continue;
                }
                Pawn pawn = PawnGenerator.GeneratePawn(kind, mechanoids);
                GenSpawn.Spawn(pawn, cell, map);
                spawned.Add(pawn);
                budget -= kind.combatPower;
            }
            if (spawned.Count > 0)
            {
                LordMaker.MakeNewLord(mechanoids, new LordJob_AssaultColony(mechanoids), map, spawned);
            }
        }

        private static void SpawnInsects(Map map, List<IntVec3> chambers, float points)
        {
            Faction insects = Faction.OfInsects;
            if (insects == null)
            {
                return;
            }

            for (int i = 1; i < chambers.Count; i++)
            {
                if (TryFindStandable(map, chambers[i], out IntVec3 cell))
                {
                    Hive hive = (Hive)GenSpawn.Spawn(ThingDefOf.Hive, cell, map);
                    hive.SetFaction(insects);
                }
            }

            var kinds = ResolveKinds(InsectKindNames);
            if (kinds.Count == 0)
            {
                return;
            }

            var spawned = new List<Pawn>();
            float budget = points;
            int guard = 0;
            while (budget > 0f && guard++ < 80)
            {
                PawnKindDef kind = kinds.RandomElement();
                if (kind.combatPower > budget && spawned.Count > 0)
                {
                    break;
                }
                IntVec3 chamber = chambers[Rand.RangeInclusive(1, chambers.Count - 1)];
                if (!TryFindStandable(map, chamber, out IntVec3 cell))
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

        private static List<PawnKindDef> ResolveKinds(string[] names)
        {
            var kinds = new List<PawnKindDef>();
            foreach (string name in names)
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(name);
                if (kind != null)
                {
                    kinds.Add(kind);
                }
            }
            return kinds;
        }

        private static void SpawnLoot(Map map, IntVec3 chamber, float marketValue)
        {
            ThingSetMakerParams lootParms = default;
            lootParms.totalMarketValueRange = new FloatRange(marketValue * 0.85f, marketValue * 1.15f);
            lootParms.qualityGenerator = QualityGenerator.Reward;
            List<Thing> loot = ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate(lootParms);
            foreach (Thing thing in loot)
            {
                if (TryFindStandable(map, chamber, out IntVec3 cell))
                {
                    GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
                }
            }
        }

        private static bool TryFindStandable(Map map, IntVec3 near, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomCellNear(near, map, 6, c => c.Standable(map), out cell);
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Strata
{
    // Populates the carved warren: hives and insects through the middle
    // chambers, and the loot hoard in the chamber farthest from the stairs.
    // Threat scale comes from the site part's threat points.
    public class GenStep_WarrenInfestation : GenStep
    {
        private static readonly string[] InsectKindNames = { "Megaspider", "Spelopede", "Megascarab" };

        public override int SeedPart => 1289047312;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!MapGenerator.TryGetVar(GenStep_CarveWarren.ChambersVar, out List<IntVec3> chambers)
                || chambers.Count < 2)
            {
                return;
            }

            float points = parms.sitePart?.parms?.threatPoints ?? 0f;
            if (points <= 0f)
            {
                points = StorytellerUtility.DefaultThreatPointsNow(Find.World);
            }
            points = Mathf.Clamp(points, 300f, 2500f);

            // chambers[0] is the arrival chamber at the stairs; the last one is
            // the tip of the warren and holds the hoard.
            IntVec3 lootChamber = chambers[chambers.Count - 1];
            SpawnDefenders(map, chambers, points);
            SpawnLoot(map, lootChamber, points);
        }

        private static void SpawnDefenders(Map map, List<IntVec3> chambers, float points)
        {
            Faction insects = Faction.OfInsects;
            if (insects == null)
            {
                return;
            }

            for (int i = 1; i < chambers.Count; i++)
            {
                bool isLootChamber = i == chambers.Count - 1;
                if (!isLootChamber && !Rand.Chance(0.6f))
                {
                    continue;
                }
                if (TryFindStandable(map, chambers[i], out IntVec3 cell))
                {
                    Hive hive = (Hive)GenSpawn.Spawn(ThingDefOf.Hive, cell, map);
                    hive.SetFaction(insects);
                }
            }

            var kinds = new List<PawnKindDef>();
            foreach (string name in InsectKindNames)
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(name);
                if (kind != null)
                {
                    kinds.Add(kind);
                }
            }
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
                if (kind.combatPower > budget)
                {
                    kind = CheapestKind(kinds);
                    if (kind.combatPower > budget && spawned.Count > 0)
                    {
                        break;
                    }
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

        private static PawnKindDef CheapestKind(List<PawnKindDef> kinds)
        {
            PawnKindDef cheapest = kinds[0];
            for (int i = 1; i < kinds.Count; i++)
            {
                if (kinds[i].combatPower < cheapest.combatPower)
                {
                    cheapest = kinds[i];
                }
            }
            return cheapest;
        }

        private static void SpawnLoot(Map map, IntVec3 chamber, float points)
        {
            ThingSetMakerParams lootParms = default;
            lootParms.totalMarketValueRange = new FloatRange(points * 0.8f, points * 1.2f);
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
            return CellFinder.TryFindRandomCellNear(near, map, 5,
                c => c.Standable(map) && !StrataPortalUtility.CellBlockedByProtectedPortal(map, c),
                out cell);
        }
    }
}

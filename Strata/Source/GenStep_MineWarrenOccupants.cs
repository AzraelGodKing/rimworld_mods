using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Strata
{
    // Populates a collapsed mine warren with a mix of insects and opportunistic
    // scavengers (rats, drifters, wild boars) plus mining-themed loot.
    public class GenStep_MineWarrenOccupants : GenStep
    {
        private static readonly string[] InsectKindNames = { "Megaspider", "Spelopede", "Megascarab" };

        private static readonly string[] ScavengerKindNames = { "Rat", "WildBoar", "Drifter", "Thrasher" };

        public override int SeedPart => 1289047313;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!MapGenerator.TryGetVar(GenStep_CarveWarren.ChambersVar, out List<IntVec3> chambers)
                || chambers.Count < 2)
            {
                return;
            }

            float points = ResolveThreatPoints(parms);
            IntVec3 lootChamber = chambers[chambers.Count - 1];
            SpawnDefenders(map, chambers, points);
            SpawnLoot(map, lootChamber, points);
        }

        private static float ResolveThreatPoints(GenStepParams parms)
        {
            float points = parms.sitePart?.parms?.threatPoints ?? 0f;
            if (points <= 0f)
            {
                points = StorytellerUtility.DefaultThreatPointsNow(Find.World);
            }
            return Mathf.Clamp(points, 300f, 2500f);
        }

        private static void SpawnDefenders(Map map, List<IntVec3> chambers, float points)
        {
            Faction insects = Faction.OfInsects;
            float insectBudget = points * 0.65f;
            float scavengerBudget = points * 0.35f;

            if (insects != null)
            {
                SpawnInsects(map, chambers, insects, insectBudget);
            }
            SpawnScavengers(map, chambers, scavengerBudget);
        }

        private static void SpawnInsects(Map map, List<IntVec3> chambers, Faction insects, float budget)
        {
            for (int i = 1; i < chambers.Count; i++)
            {
                bool isLootChamber = i == chambers.Count - 1;
                if (!isLootChamber && !Rand.Chance(0.55f))
                {
                    continue;
                }
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
            float remaining = budget;
            int guard = 0;
            while (remaining > 0f && guard++ < 80)
            {
                PawnKindDef kind = kinds.RandomElement();
                if (kind.combatPower > remaining && spawned.Count > 0)
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
                remaining -= kind.combatPower;
            }
            if (spawned.Count > 0)
            {
                LordMaker.MakeNewLord(insects, new LordJob_DefendAndExpandHive(), map, spawned);
            }
        }

        private static void SpawnScavengers(Map map, List<IntVec3> chambers, float budget)
        {
            var kinds = ResolveKinds(ScavengerKindNames);
            if (kinds.Count == 0 || budget <= 0f)
            {
                return;
            }

            Faction hostiles = null;
            foreach (Faction faction in Find.FactionManager.AllFactionsVisible)
            {
                if (faction.HostileTo(Faction.OfPlayer))
                {
                    hostiles = faction;
                    break;
                }
            }
            if (hostiles == null)
            {
                hostiles = Faction.OfInsects;
            }
            if (hostiles == null)
            {
                return;
            }

            var spawned = new List<Pawn>();
            float remaining = budget;
            int guard = 0;
            while (remaining > 0f && guard++ < 40)
            {
                PawnKindDef kind = kinds.RandomElement();
                if (kind.combatPower > remaining && spawned.Count > 0)
                {
                    break;
                }
                IntVec3 chamber = chambers[Rand.RangeInclusive(1, chambers.Count - 1)];
                if (!TryFindStandable(map, chamber, out IntVec3 cell))
                {
                    continue;
                }
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, hostiles,
                    PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true));
                if (kind.RaceProps.Animal && pawn.mindState != null)
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
                }
                GenSpawn.Spawn(pawn, cell, map);
                spawned.Add(pawn);
                remaining -= kind.combatPower;
            }
            if (spawned.Count > 0 && hostiles != null)
            {
                LordMaker.MakeNewLord(hostiles, new LordJob_AssaultColony(hostiles), map, spawned);
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
            return CellFinder.TryFindRandomCellNear(near, map, 5, c => c.Standable(map), out cell);
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Strata
{
    // Lost miners stumble into your workings — potential allies or trouble.
    public class IncidentWorker_LostMiners : IncidentWorker
    {
        private const int MinMiners = 1;
        private const int MaxMiners = 3;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map
                && StrataMapUtility.IsUnderground(map)
                && map.mapPawns.FreeColonistsSpawnedCount > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!CellFinder.TryFindRandomCell(map, c => c.Standable(map) && !c.Fogged(map), out IntVec3 spawn))
            {
                return false;
            }
            bool hostile = Rand.Chance(0.35f);
            Faction faction = hostile
                ? Find.FactionManager.RandomEnemyFaction()
                : Find.FactionManager.RandomNonHostileFaction(allowHidden: false, allowNonHumanlike: false);
            if (faction == null)
            {
                faction = Faction.OfAncients;
            }
            int count = Rand.RangeInclusive(MinMiners, MaxMiners);
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Miner") ?? PawnKindDefOf.Colonist;
            var spawned = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction,
                    PawnGenerationContext.NonPlayer, map.Tile));
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(spawn, map, 4), map);
                spawned.Add(pawn);
            }
            var targets = new LookTargets(spawned);
            if (hostile && faction.HostileTo(Faction.OfPlayer))
            {
                LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction), map, spawned);
                // Neutral letters are easy to miss while viewing another floor —
                // hostiles get a raid-style threat letter that jumps to them.
                Find.LetterStack.ReceiveLetter(
                    "Strata_LostMiners_HostileLabel".Translate(),
                    "Strata_LostMiners_HostileText".Translate(spawned.Count),
                    LetterDefOf.ThreatSmall,
                    targets);
                Find.TickManager.slower.SignalForceNormalSpeedShort();
            }
            else
            {
                SendStandardLetter(parms, targets);
            }
            return true;
        }
    }
}

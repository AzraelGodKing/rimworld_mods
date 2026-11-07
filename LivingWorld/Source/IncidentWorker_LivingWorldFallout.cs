using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace LivingWorld
{
    public class IncidentWorker_LivingWorldRefugees : IncidentWorker
    {
        private const int MinPawns = 2;
        private const int MaxPawns = 5;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (LivingWorldMod.Settings == null
                || !LivingWorldMod.Settings.enabled
                || !LivingWorldMod.Settings.falloutEnabled)
            {
                return false;
            }
            if (parms.target is not Map)
            {
                return false;
            }
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            return comp?.PeekFallout(FalloutKind.Refugees) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            // Peek, not dequeue: the entry is only consumed once the refugees
            // actually arrive, or dropped when it can never fire.
            PendingFallout fallout = comp?.PeekFallout(FalloutKind.Refugees);
            if (fallout == null)
            {
                return false;
            }
            Faction faction = fallout.Loser();
            if (faction == null || faction.defeated || faction.IsPlayer
                || faction.HostileTo(Faction.OfPlayer))
            {
                // The losing faction is gone or hostile now: these refugees never come.
                comp.RemoveFallout(fallout);
                return false;
            }

            if (!RCellFinder.TryFindRandomPawnEntryCell(
                    out IntVec3 entry,
                    map,
                    CellFinder.EdgeRoadChance_Neutral))
            {
                // No way in right now; keep it queued for a later try.
                return false;
            }

            int count = Rand.RangeInclusive(MinPawns, MaxPawns);
            var spawned = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = faction.RandomPawnKind() ?? PawnKindDefOf.Villager;
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind,
                    faction,
                    PawnGenerationContext.NonPlayer,
                    map.Tile,
                    forceGenerateNewPawn: true,
                    canGeneratePawnRelations: true,
                    mustBeCapableOfViolence: false,
                    colonistRelationChanceFactor: 0f,
                    inhabitant: false,
                    certainlyBeenInCryptosleep: false,
                    forceRedressWorldPawnIfFormerColonist: true,
                    worldPawnFactionDoesntMatter: false,
                    biocodeWeaponChance: 0f,
                    extraPawnForExtraRelationChance: null,
                    relationWithExtraPawnChanceFactor: 0f,
                    allowPregnant: true));
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(entry, map, 4), map);
                spawned.Add(pawn);
            }

            if (spawned.Count == 0)
            {
                return false;
            }
            comp.RemoveFallout(fallout);

            LordMaker.MakeNewLord(
                faction,
                new LordJob_VisitColony(faction, spawned[0].Position, 45000),
                map,
                spawned);

            var targets = new LookTargets(spawned);
            string place = string.IsNullOrEmpty(fallout.settlementLabel)
                ? "LivingWorld_UnknownPlace".Translate().ToString()
                : fallout.settlementLabel;
            Find.LetterStack.ReceiveLetter(
                "LivingWorld_LetterLabel_RefugeeFlight".Translate(),
                "LivingWorld_LetterText_RefugeeFlight".Translate(
                    faction.Name,
                    fallout.Winner()?.Name ?? "LivingWorld_UnknownFaction".Translate(),
                    place),
                LetterDefOf.NeutralEvent,
                targets);

            WorldEvent ev = WorldEvent.Create(
                WorldEventKind.RefugeeFlight,
                NewsSeverity.Normal,
                faction,
                fallout.Winner(),
                null);
            ev.settlementLabel = place;
            ev.Where = fallout.Where;
            ev.seenByPlayer = true;
            comp.RecordAndPublish(ev);

            return true;
        }
    }

    public class IncidentWorker_LivingWorldWarband : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (LivingWorldMod.Settings == null
                || !LivingWorldMod.Settings.enabled
                || !LivingWorldMod.Settings.falloutEnabled
                || !LivingWorldMod.Settings.warbandFalloutEnabled)
            {
                return false;
            }
            if (parms.target is not Map)
            {
                return false;
            }
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            return comp?.PeekFallout(FalloutKind.Warband) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            PendingFallout fallout = comp?.PeekFallout(FalloutKind.Warband);
            if (fallout == null)
            {
                return false;
            }
            // Warband is from the winner chasing remnants — hostile if that faction hates player.
            Faction faction = fallout.Winner();
            if (faction == null || faction.defeated || faction.IsPlayer)
            {
                comp.RemoveFallout(fallout);
                return false;
            }

            parms.points = System.Math.Min(
                parms.points > 0 ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map),
                600f);
            parms.faction = faction;

            if (!RCellFinder.TryFindRandomPawnEntryCell(
                    out IntVec3 entry,
                    map,
                    CellFinder.EdgeRoadChance_Hostile))
            {
                return false;
            }

            var group = new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                tile = map.Tile,
                faction = faction,
                points = parms.points,
            };
            var spawned = new List<Pawn>();
            foreach (Pawn pawn in PawnGroupMakerUtility.GeneratePawns(group, false))
            {
                if (pawn == null)
                {
                    continue;
                }
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(entry, map, 8), map);
                spawned.Add(pawn);
            }

            if (spawned.Count == 0)
            {
                return false;
            }
            comp.RemoveFallout(fallout);

            if (faction.HostileTo(Faction.OfPlayer))
            {
                LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction), map, spawned);
            }
            else
            {
                // A friendly warband is passing through, not reinforcing: walk across
                // the map and leave, like refugees do after their visit.
                if (!RCellFinder.TryFindTravelDestFrom(entry, map, out IntVec3 exit))
                {
                    exit = entry;
                }
                LordMaker.MakeNewLord(faction, new LordJob_TravelAndExit(exit), map, spawned);
            }

            var targets = new LookTargets(spawned);
            Find.LetterStack.ReceiveLetter(
                "LivingWorld_LetterLabel_WarbandPass".Translate(),
                "LivingWorld_LetterText_WarbandPass".Translate(
                    faction.Name,
                    fallout.Loser()?.Name ?? "LivingWorld_UnknownFaction".Translate(),
                    fallout.settlementLabel ?? "LivingWorld_UnknownPlace".Translate()),
                faction.HostileTo(Faction.OfPlayer)
                    ? LetterDefOf.ThreatSmall
                    : LetterDefOf.NeutralEvent,
                targets);

            WorldEvent ev = WorldEvent.Create(
                WorldEventKind.WarbandPass,
                NewsSeverity.Normal,
                faction,
                fallout.Loser(),
                null);
            ev.settlementLabel = fallout.settlementLabel;
            ev.Where = fallout.Where;
            ev.seenByPlayer = true;
            comp.RecordAndPublish(ev);

            return true;
        }
    }
}

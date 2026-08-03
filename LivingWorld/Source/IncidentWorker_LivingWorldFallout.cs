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
            return comp?.PeekFallout(FalloutKind.Refugees) != null
                || parms.forced;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            PendingFallout fallout = comp?.TryDequeueFallout(FalloutKind.Refugees);
            Faction faction = fallout?.Loser();
            if (faction == null || faction.defeated || faction.IsPlayer)
            {
                faction = Find.FactionManager.RandomNonHostileFaction(
                    allowHidden: false,
                    allowNonHumanlike: false,
                    allowDefeated: false);
            }
            if (faction == null)
            {
                return false;
            }

            if (!RCellFinder.TryFindRandomPawnEntryCell(
                    out IntVec3 entry,
                    map,
                    CellFinder.EdgeRoadChance_Neutral))
            {
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

            LordMaker.MakeNewLord(
                faction,
                new LordJob_DefendPoint(spawned[0].Position),
                map,
                spawned);

            var targets = new LookTargets(spawned);
            string place = string.IsNullOrEmpty(fallout?.settlementLabel)
                ? "LivingWorld_UnknownPlace".Translate().ToString()
                : fallout.settlementLabel;
            Find.LetterStack.ReceiveLetter(
                "LivingWorld_LetterLabel_RefugeeFlight".Translate(),
                "LivingWorld_LetterText_RefugeeFlight".Translate(
                    faction.Name,
                    fallout?.Winner()?.Name ?? "LivingWorld_UnknownFaction".Translate(),
                    place),
                LetterDefOf.NeutralEvent,
                targets);

            if (comp != null)
            {
                WorldEvent ev = WorldEvent.Create(
                    WorldEventKind.RefugeeFlight,
                    NewsSeverity.Normal,
                    faction,
                    fallout?.Winner(),
                    null);
                ev.settlementLabel = place;
                ev.tile = fallout?.tile ?? -1;
                ev.seenByPlayer = true;
                comp.RecordAndPublish(ev);
            }

            return true;
        }
    }

    public class IncidentWorker_LivingWorldWarband : IncidentWorker
    {
        private const int MinPawns = 3;
        private const int MaxPawns = 6;

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
            return comp?.PeekFallout(FalloutKind.Warband) != null || parms.forced;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            PendingFallout fallout = comp?.TryDequeueFallout(FalloutKind.Warband);
            // Warband is from the winner chasing remnants — hostile if that faction hates player.
            Faction faction = fallout?.Winner()
                ?? Find.FactionManager.RandomEnemyFaction(allowHidden: false, allowDefeated: false);
            if (faction == null)
            {
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

            int count = Rand.RangeInclusive(MinPawns, MaxPawns);
            var spawned = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = faction.RandomPawnKind() ?? PawnKindDefOf.Drifter;
                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(entry, map, 4), map);
                spawned.Add(pawn);
            }

            if (spawned.Count == 0)
            {
                return false;
            }

            if (faction.HostileTo(Faction.OfPlayer))
            {
                LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction), map, spawned);
            }
            else
            {
                LordMaker.MakeNewLord(
                    faction,
                    new LordJob_AssistColony(faction, spawned[0].Position),
                    map,
                    spawned);
            }

            var targets = new LookTargets(spawned);
            Find.LetterStack.ReceiveLetter(
                "LivingWorld_LetterLabel_WarbandPass".Translate(),
                "LivingWorld_LetterText_WarbandPass".Translate(
                    faction.Name,
                    fallout?.Loser()?.Name ?? "LivingWorld_UnknownFaction".Translate(),
                    fallout?.settlementLabel ?? "LivingWorld_UnknownPlace".Translate()),
                faction.HostileTo(Faction.OfPlayer)
                    ? LetterDefOf.ThreatSmall
                    : LetterDefOf.NeutralEvent,
                targets);

            if (comp != null)
            {
                WorldEvent ev = WorldEvent.Create(
                    WorldEventKind.WarbandPass,
                    NewsSeverity.Normal,
                    faction,
                    fallout?.Loser(),
                    null);
                ev.settlementLabel = fallout?.settlementLabel;
                ev.tile = fallout?.tile ?? -1;
                ev.seenByPlayer = true;
                comp.RecordAndPublish(ev);
            }

            return true;
        }
    }
}

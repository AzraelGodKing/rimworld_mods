using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Strata
{
    // The deep is not entirely safe. Ordinary raids can't reach a sealed rock
    // level - but something can dig. A hostile group tunnels up through the
    // stone and emerges among your colonists. Underground levels only.
    public class IncidentWorker_DeepRaid : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Map map) || !StrataMapUtility.IsUnderground(map))
            {
                return Fail("target is not an underground level");
            }
            if (map.mapPawns.FreeColonistsSpawned.Count == 0)
            {
                return Fail("no colonists on this level");
            }
            if (parms.faction == null && PickFaction() == null)
            {
                return Fail("no enemy faction available");
            }
            return true;
        }

        // Dev-mode breadcrumb: "cannot fire" without a reason is undebuggable.
        // If the debug menu refuses with NO such log line, the failure is in
        // vanilla's base checks (big threats disabled, refire cooldown, ...).
        private static bool Fail(string reason)
        {
            if (Prefs.DevMode)
            {
                Log.Message("[Strata] Deep raid can't fire: " + reason);
            }
            return false;
        }

        // Prefer a faction that can actually field a raid; fall back to any
        // hostile faction.
        private static Faction PickFaction()
        {
            return Find.FactionManager.RandomRaidableEnemyFaction()
                ?? Find.FactionManager.RandomEnemyFaction();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Faction faction = parms.faction ?? PickFaction();
            if (faction == null)
            {
                if (Prefs.DevMode)
                {
                    Log.Message("[Strata] Deep raid aborted: no enemy faction available.");
                }
                return false;
            }

            float points = parms.points > 0f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);
            var groupParms = new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                tile = map.Tile,
                faction = faction,
                points = points,
                generateFightersOnly = true,
            };

            List<Pawn> raiders = PawnGroupMakerUtility.GeneratePawns(groupParms).ToList();
            if (raiders.Count == 0)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"[Strata] Deep raid aborted: {faction} generated no raiders at {points} points.");
                }
                return false;
            }

            IntVec3 anchor = map.mapPawns.FreeColonistsSpawned.RandomElement().Position;
            foreach (Pawn raider in raiders)
            {
                IntVec3 cell = EmergenceCell(map, anchor);
                GenSpawn.Spawn(raider, cell, map);
                FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, 2f, Color.white);
            }

            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction), map, raiders);
            SendStandardLetter(parms, new LookTargets(raiders));
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            return true;
        }

        private static IntVec3 EmergenceCell(Map map, IntVec3 anchor)
        {
            bool Valid(IntVec3 c) => c.Standable(map) && !c.Fogged(map) && c.GetRoom(map) != null;
            if (CellFinder.TryFindRandomCellNear(anchor, map, 14, Valid, out IntVec3 near))
            {
                return near;
            }
            if (CellFinder.TryFindRandomCell(map, Valid, out IntVec3 any))
            {
                return any;
            }
            return anchor;
        }
    }
}

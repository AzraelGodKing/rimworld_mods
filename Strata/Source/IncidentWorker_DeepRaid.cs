using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // The deep is not entirely safe. Ordinary raids can't reach a sealed rock
    // level - but the deep belongs to the insects. A swarm tunnels up through
    // the floor (vanilla's tunnel spawner: rubble, dust, then eruption) and
    // emerges among your colonists. Underground levels only.
    public class IncidentWorker_DeepRaid : IncidentWorker
    {
        private const float MinInsectPoints = 200f;
        private const float MaxInsectPoints = 1000f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Map map) || !StrataMapUtility.IsUnderground(map))
            {
                return Fail(parms, "target is not an underground level");
            }
            if (map.mapPawns.FreeColonistsSpawned.Count == 0)
            {
                return Fail(parms, "no colonists on this level");
            }
            if (Faction.OfInsects == null)
            {
                return Fail(parms, "no insect faction in this game");
            }
            return true;
        }

        // Dev-mode breadcrumb: "cannot fire" without a reason is undebuggable.
        // If the debug menu refuses with NO such log line, the failure is in
        // vanilla's base checks (big threats disabled, refire cooldown, ...).
        // Only for deliberate attempts (forced = Strata's debug buttons); the
        // storyteller probes CanFireNow constantly and would spam the log.
        private static bool Fail(IncidentParms parms, string reason)
        {
            if (Prefs.DevMode && parms.forced)
            {
                StrataLog.Verbose("[Strata] Deep raid can't fire: " + reason);
            }
            return false;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (Faction.OfInsects == null || map.mapPawns.FreeColonistsSpawned.Count == 0)
            {
                return false;
            }
            float points = parms.points > 0f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);
            // Deeper levels crawl with more bugs - the flip side of richer ore.
            points *= 1f + 0.15f * StrataDepth.Of(map);

            IntVec3 anchor = map.mapPawns.FreeColonistsSpawned.RandomElement().Position;
            IntVec3 cell = CellFinder.FindNoWipeSpawnLocNear(anchor, map, ThingDefOf.TunnelHiveSpawner, Rot4.North, 14,
                x => x.Walkable(map) && !x.Fogged(map)
                    && x.GetFirstThing(map, ThingDefOf.Hive) == null
                    && x.GetFirstThing(map, ThingDefOf.TunnelHiveSpawner) == null);

            var spawner = (TunnelHiveSpawner)ThingMaker.MakeThing(ThingDefOf.TunnelHiveSpawner);
            spawner.spawnHive = false;
            // A fresh dig has almost no wealth; the floor guarantees a real
            // swarm, the ceiling keeps a treasure vault from spawning an army.
            spawner.insectsPoints = Mathf.Clamp(points, MinInsectPoints, MaxInsectPoints);
            GenSpawn.Spawn(spawner, cell, map, WipeMode.FullRefund);

            SendStandardLetter(parms, new TargetInfo(cell, map));
            // Telegraph: the ground shakes while the tunneler digs (the
            // spawner's own delay is the reaction window).
            if (Find.CurrentMap == map)
            {
                Find.CameraDriver.shaker.DoShake(1.5f);
            }
            else if (StrataCrossLevelThreatNotify.ShouldForceAttention(map))
            {
                // Threat letter postfix jumps; also cut immediately so a slow
                // letter stack still pulls the player onto B1.
                CameraJumper.TryJump(new GlobalTargetInfo(cell, map), CameraJumper.MovementMode.Cut);
            }
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            return true;
        }
    }
}

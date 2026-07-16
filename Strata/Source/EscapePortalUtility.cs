using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Prisoners, slaves, guests, and other flee/exit AI need a real way off an
    // underground level — there is no map edge to walk to. Prefer stairwells /
    // elevators that climb toward the surface so prison breaks and slave escapes
    // keep working without escorts.
    public static class EscapePortalUtility
    {
        public static bool NeedsPortalEscape(Map map)
        {
            return map != null && LevelGraph.AnyLinkFrom(map);
        }

        public static MapPortal FindEscapePortal(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Downed || pawn.Dead)
            {
                return null;
            }
            Map map = pawn.Map;
            if (!NeedsPortalEscape(map))
            {
                return null;
            }

                int altitude = StrataDepth.Altitude(map);
            MapPortal best = null;
            float bestScore = float.MinValue;

            List<Thing> portals = map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal);
            for (int i = 0; i < portals.Count; i++)
            {
                if (portals[i] is not MapPortal portal || !portal.Spawned)
                {
                    continue;
                }
                if (!portal.IsEnterable(out _))
                {
                    continue;
                }
                Map other = LevelGraph.OtherMapSafe(portal);
                if (other == null)
                {
                    continue;
                }
                // Escapers accept more danger than colonists commuting for work.
                if (!pawn.CanReach(portal, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                int otherAltitude = StrataDepth.Altitude(other);
                // Prefer stepping toward the surface (altitude 0) from above or below.
                float score = (System.Math.Abs(altitude) - System.Math.Abs(otherAltitude)) * 10000f;
                if (otherAltitude == 0)
                {
                    score += 50000f;
                }
                // Prefer nearer shafts when scores tie.
                score -= pawn.Position.DistanceTo(portal.Position);
                // Elevators that need power to go down are fine for going up;
                // IsEnterable already encodes that.
                if (score > bestScore)
                {
                    bestScore = score;
                    best = portal;
                }
            }

            return best;
        }

        public static Job MakeEscapeJob(Pawn pawn)
        {
            MapPortal portal = FindEscapePortal(pawn);
            if (portal == null)
            {
                return null;
            }
            return JobMaker.MakeJob(JobDefOf.EnterPortal, portal);
        }
    }
}

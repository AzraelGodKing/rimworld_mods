using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    public static class LivingWorldHearRules
    {
        public static bool PlayerShouldHear(WorldEvent ev)
        {
            if (ev == null)
            {
                return false;
            }

            LivingWorldSettings s = LivingWorldMod.Settings;
            if (s == null || !s.chronicleEnabled)
            {
                return false;
            }

            if (ev.severity == NewsSeverity.Major)
            {
                return true;
            }

            int verbosity = s.newsVerbosity;
            if (verbosity <= 0)
            {
                return false;
            }

            if (HasPriorContact(ev) || IsNearby(ev) || HasAllyInvolved(ev))
            {
                return true;
            }

            if (verbosity >= 2 && HasComms())
            {
                return true;
            }

            return verbosity >= 2;
        }

        private static bool HasPriorContact(WorldEvent ev)
        {
            return Contacted(ev.FactionA()) || Contacted(ev.FactionB());
        }

        private static bool Contacted(Faction faction)
        {
            if (faction == null || faction.IsPlayer || faction.defeated)
            {
                return false;
            }
            // Any non-default goodwill or ever interacted via relation kind.
            int goodwill = faction.GoodwillWith(Faction.OfPlayer);
            if (goodwill != 0)
            {
                return true;
            }
            return faction.PlayerRelationKind != FactionRelationKind.Neutral;
        }

        private static bool IsNearby(WorldEvent ev)
        {
            if (ev.tile < 0)
            {
                return false;
            }
            int playerTile = Find.AnyPlayerHomeMap?.Tile ?? -1;
            if (playerTile < 0)
            {
                return false;
            }
            int dist = Find.WorldGrid.TraversalDistanceBetween(playerTile, ev.tile);
            int max = LivingWorldMod.Settings?.proximityTiles ?? 30;
            return dist >= 0 && dist <= max;
        }

        private static bool HasAllyInvolved(WorldEvent ev)
        {
            return IsFriendly(ev.FactionA()) || IsFriendly(ev.FactionB());
        }

        private static bool IsFriendly(Faction faction)
        {
            if (faction == null || faction.IsPlayer || faction.defeated)
            {
                return false;
            }
            return faction.PlayerRelationKind == FactionRelationKind.Ally
                || faction.GoodwillWith(Faction.OfPlayer) >= 50;
        }

        private static bool HasComms()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map?.IsPlayerHome != true)
                {
                    continue;
                }
                if (map.listerBuildings.ColonistsHaveBuilding(ThingDefOf.CommsConsole)
                    || map.listerBuildings.ColonistsHaveBuilding(ThingDefOf.OrbitalTradeBeacon))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

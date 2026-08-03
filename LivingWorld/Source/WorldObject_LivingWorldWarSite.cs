using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    public class WorldObject_LivingWorldWarSite : WorldObject
    {
        public int expiresTick;
        public string siteKind = "battlefield";

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref expiresTick, "expiresTick");
            Scribe_Values.Look(ref siteKind, "siteKind", "battlefield");
        }

        protected override void Tick()
        {
            base.Tick();
            if (expiresTick > 0 && Find.TickManager.TicksGame >= expiresTick)
            {
                Destroy();
            }
        }

        public override string GetInspectString()
        {
            string baseStr = base.GetInspectString();
            string key = siteKind == "refugeecamp"
                ? "LivingWorld_WarSite_RefugeeCampInspect"
                : "LivingWorld_WarSite_BattlefieldInspect";
            string line = key.Translate();
            return string.IsNullOrEmpty(baseStr) ? line : baseStr + "\n" + line;
        }
    }

    public static class LivingWorldWarSites
    {
        private const int LifetimeTicks = 60000 * 15; // ~15 days

        public static void TrySpawnNear(int nearTile, Faction winner, Faction loser)
        {
            if (nearTile < 0)
            {
                return;
            }

            PlanetTile? tile = FindOpenTileNear(nearTile);
            if (tile == null)
            {
                return;
            }

            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("LivingWorld_WarSite");
            if (def == null)
            {
                return;
            }

            var site = (WorldObject_LivingWorldWarSite)WorldObjectMaker.MakeWorldObject(def);
            site.Tile = tile.Value;
            site.SetFaction(winner ?? loser);
            site.expiresTick = Find.TickManager.TicksGame + LifetimeTicks;
            site.siteKind = Rand.Bool ? "battlefield" : "refugeecamp";
            Find.WorldObjects.Add(site);
        }

        public static void TickExpire()
        {
            // Expiry handled in WorldObject.Tick; keep hook for future batch work.
        }

        private static PlanetTile? FindOpenTileNear(int nearTile)
        {
            PlanetTile center = nearTile;
            for (int i = 0; i < 30; i++)
            {
                if (!TileFinder.TryFindPassableTileWithTraversalDistance(
                        center,
                        1,
                        8,
                        out PlanetTile tile))
                {
                    continue;
                }
                if (Find.WorldObjects.AnyWorldObjectAt(tile))
                {
                    continue;
                }
                if (TileBlockedByNemesis(tile))
                {
                    continue;
                }
                return tile;
            }
            return null;
        }

        /// <summary>
        /// Soft avoid: if a Nemesis world object already sits on the tile, skip.
        /// No hard dependency — match by type/def name only.
        /// </summary>
        private static bool TileBlockedByNemesis(PlanetTile tile)
        {
            foreach (WorldObject wo in Find.WorldObjects.ObjectsAt(tile))
            {
                if (wo?.def == null)
                {
                    continue;
                }
                string defName = wo.def.defName ?? string.Empty;
                string typeName = wo.GetType().FullName ?? string.Empty;
                if (defName.IndexOf("Nemesis", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || typeName.IndexOf("Nemesis", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

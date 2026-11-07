#nullable enable
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Nemesis
{
    /// <summary>
    /// Hunt camps on the world map (AZR-555). A bought lead places a camp of the nemesis's
    /// faction near their nearest settlement. On a real lead the nemesis waits there and joins
    /// the camp's defenders when your caravan arrives; on a false lead the camp holds only their
    /// people and a calling card. Camps use the <c>Nemesis_HuntSite</c> world object def, so
    /// Living World keeps its war sites off them, and time out after a while.
    /// </summary>
    public static class NemesisHuntSites
    {
        public const string SiteDefName = "Nemesis_HuntSite";
        private const int TimeoutDays = 12;

        public static WorldObjectDef? SiteDef => DefDatabase<WorldObjectDef>.GetNamedSilentFail(SiteDefName);

        /// <summary>
        /// Place a camp for this hunt. False when no camp can be made (no faction, def or tile);
        /// the caller then records a heading instead.
        /// </summary>
        public static bool TryCreateCamp(NemesisData h, bool real, Map? home)
        {
            Faction? faction = NemesisActions.FindFaction(h);
            WorldObjectDef? def = SiteDef;
            SitePartDef? part = SitePartDefOf.BanditCamp;
            Map? homeMap = home ?? Find.AnyPlayerHomeMap;
            if (faction == null || def == null || part == null || homeMap == null) return false;

            Settlement? near = NearestSettlement(faction, homeMap.Tile);
            PlanetTile origin = near?.Tile ?? homeMap.Tile;
            int minDist = near != null ? 2 : 4;
            int maxDist = near != null ? 7 : 12;
            if (!TileFinder.TryFindNewSiteTile(out PlanetTile tile, origin, minDist, maxDist))
                return false;

            try
            {
                Site site = SiteMaker.MakeSite(part, tile, faction,
                    threatPoints: StorytellerUtility.DefaultSiteThreatPointsNow(), worldObjectDef: def);
                site.GetComponent<TimeoutComp>()?.StartTimeout(TimeoutDays * GenDate.TicksPerDay);
                Find.WorldObjects.Add(site);

                ForgetCamp(h);
                h.campSite = site;
                h.campIsReal = real;
                h.campVisited = false;
                h.lastKnownTile = tile;
                h.lastKnownTileLabel = TileLabel(tile);
                NemesisTells.RecordNote(h, "Nemesis_Note_Camp", h.lastKnownTileLabel);
                return true;
            }
            catch (System.Exception e)
            {
                Log.Warning("[Nemesis] Hunt camp creation failed: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// After an escape the nemesis heads home: point the dossier at their faction's nearest
        /// settlement rather than at your colony.
        /// </summary>
        public static void RecordHeading(NemesisData h, Map? from)
        {
            Faction? faction = NemesisActions.FindFaction(h);
            PlanetTile fromTile = from?.Tile ?? Find.AnyPlayerHomeMap?.Tile ?? PlanetTile.Invalid;
            Settlement? near = faction != null && fromTile.Valid ? NearestSettlement(faction, fromTile) : null;
            if (near == null)
            {
                NemesisTells.RecordNote(h, "Nemesis_Note_HeadingUnknown");
                return;
            }
            h.lastKnownTile = near.Tile;
            h.lastKnownTileLabel = near.LabelCap;
            NemesisTells.RecordNote(h, "Nemesis_Note_Heading", near.LabelCap);
        }

        /// <summary>Called every 250 ticks per active hunt: camp expiry and arrival.</summary>
        public static void Tick(NemesisData h)
        {
            Site? site = h.campSite;
            if (site == null) return;
            if (site.Destroyed || !Find.WorldObjects.Contains(site))
            {
                if (!h.campVisited)
                    NemesisTells.RecordNote(h, "Nemesis_Note_CampGone");
                h.campSite = null;
                return;
            }
            if (h.campVisited || !site.HasMap) return;

            h.campVisited = true;
            Map map = site.Map;
            string name = h.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            Pawn? placed = h.campIsReal ? TryPlaceNemesis(h, map) : null;
            if (placed != null)
            {
                NemesisTells.RecordNote(h, "Nemesis_Note_CampFound");
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_CampFoundTitle".Translate(h.ColoredName),
                    "Nemesis_Letter_CampFoundBody".Translate(name),
                    LetterDefOf.ThreatBig,
                    placed);
                return;
            }

            // False lead, or the nemesis was out hunting you: the camp says so.
            string key = h.campIsReal ? "Nemesis_Letter_CampMissedBody" : "Nemesis_Letter_CampFalseBody";
            NemesisTells.RecordNote(h, h.campIsReal ? "Nemesis_Note_CampMissed" : "Nemesis_Note_CampFalse");
            NemesisTells.MaybeLeaveMark(h, map, null, always: true);
            h.aggressionLevel = Mathf.Min(h.aggressionLevel + 0.25f, 10f);
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_CampEmptyTitle".Translate(h.ColoredName),
                key.Translate(name),
                LetterDefOf.NeutralEvent,
                new LookTargets(map.Center, map));
        }

        /// <summary>Drop a camp reference when its hunt ends or a new camp replaces it.</summary>
        public static void ForgetCamp(NemesisData h)
        {
            h.campSite = null;
            h.campVisited = false;
        }

        /// <summary>The nemesis waits in the camp only if they are off every map, free and well.</summary>
        private static Pawn? TryPlaceNemesis(NemesisData h, Map map)
        {
            Pawn? nemesis = GameComponent_Nemesis.Instance?.FindNemesisPawnFor(h);
            if (nemesis == null || nemesis.Dead || nemesis.Destroyed || nemesis.Spawned || !nemesis.IsWorldPawn())
                return null;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 14,
                    c => c.Standable(map), out IntVec3 cell))
                return null;

            NemesisPawnUtil.EnsureHuntFaction(nemesis, h.faction);
            NemesisProgression.Apply(nemesis, h);
            if (!NemesisPawnUtil.TrySpawnOnMap(nemesis, map, cell)) return null;
            h.cachedNemesis = nemesis;

            Faction? faction = nemesis.Faction;
            if (faction == null || faction.IsPlayer) return nemesis;
            Lord? defenders = null;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i].faction == faction) { defenders = lords[i]; break; }
            }
            if (defenders != null)
                defenders.AddPawn(nemesis);
            else
                LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, map.Center, 0, false), map, new[] { nemesis });
            NemesisProgression.TrySpawnMechEscort(map, faction, h, nemesis);
            NemesisProgression.TrySpawnSoftEscorts(map, faction, h, nemesis);
            return nemesis;
        }

        private static Settlement? NearestSettlement(Faction faction, PlanetTile from)
        {
            Settlement? best = null;
            float bestDist = float.MaxValue;
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                Settlement s = all[i];
                if (s.Faction != faction || s.Tile.Layer != from.Layer) continue;
                float d = Find.WorldGrid.ApproxDistanceInTiles(from, s.Tile);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }
            return best;
        }

        private static string TileLabel(PlanetTile tile) =>
            Find.WorldGrid.LongLatOf(tile).ToString();
    }
}

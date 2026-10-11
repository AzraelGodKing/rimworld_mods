using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// LW9 — an NPC caravan walking a precomputed world path between two settlements.
    /// Purely cosmetic on the map; arrival writes a chronicle-only entry.
    /// </summary>
    public class WorldObject_LivingWorldTraffic : WorldObject
    {
        public List<int> route = new List<int>();
        public int routeIndex;
        public int nextHopTick;
        public int destinationId = -1;
        public string originLabel;
        public string destinationLabel;
        public bool divertRolled;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref route, "route", LookMode.Value);
            Scribe_Values.Look(ref routeIndex, "routeIndex");
            Scribe_Values.Look(ref nextHopTick, "nextHopTick");
            Scribe_Values.Look(ref destinationId, "destinationId", -1);
            Scribe_Values.Look(ref originLabel, "originLabel");
            Scribe_Values.Look(ref destinationLabel, "destinationLabel");
            Scribe_Values.Look(ref divertRolled, "divertRolled");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                route ??= new List<int>();
            }
        }

        protected override void Tick()
        {
            base.Tick();
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (settings == null || !settings.enabled || !settings.trafficEnabled || route.Count < 2)
            {
                Destroy();
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (now < nextHopTick)
            {
                return;
            }
            nextHopTick = now + LivingWorldTraffic.TicksPerTile;
            routeIndex++;
            if (routeIndex >= route.Count - 1)
            {
                LivingWorldTraffic.Arrive(this);
                return;
            }
            Tile = route[routeIndex];
            LivingWorldTraffic.MaybeDivert(this);
        }

        public override Vector3 DrawPos
        {
            get
            {
                Vector3 here = Find.WorldGrid.GetTileCenter(Tile);
                if (route == null || routeIndex + 1 >= route.Count)
                {
                    return here;
                }
                float left = nextHopTick - Find.TickManager.TicksGame;
                float t = Mathf.Clamp01(1f - left / LivingWorldTraffic.TicksPerTile);
                return Vector3.Lerp(here, Find.WorldGrid.GetTileCenter(route[routeIndex + 1]), t);
            }
        }

        public override string GetInspectString()
        {
            string baseStr = base.GetInspectString();
            int now = Find.TickManager.TicksGame;
            int hopsAfterNext = Math.Max(0, route.Count - 2 - routeIndex);
            int ticksLeft = Math.Max(0, nextHopTick - now) + hopsAfterNext * LivingWorldTraffic.TicksPerTile;
            string line = "LivingWorld_Traffic_Inspect".Translate(
                originLabel ?? "LivingWorld_UnknownPlace".Translate().ToString(),
                destinationLabel ?? "LivingWorld_UnknownPlace".Translate().ToString(),
                ticksLeft.ToStringTicksToPeriod());
            return string.IsNullOrEmpty(baseStr) ? line : baseStr + "\n" + line;
        }
    }

    public static class LivingWorldTraffic
    {
        public const int TicksPerTile = 2500;
        private const int MaxActive = 3;
        private const float SpawnChancePerPulse = 0.05f;
        private const int MinRouteTiles = 3;
        private const int MaxRouteTiles = 45;
        private const float MaxStraightDistance = 35f;
        private const float DivertRadiusTiles = 8f;
        private const float DivertChance = 0.1f;
        private const float ProsperityBumpChance = 0.25f;

        public static void TryResolvePulse(GameComponent_LivingWorld comp)
        {
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (comp == null || settings == null || !settings.trafficEnabled)
            {
                return;
            }
            if (!Rand.Chance(SpawnChancePerPulse))
            {
                return;
            }
            TrySpawn(comp);
        }

        public static bool TrySpawn(GameComponent_LivingWorld comp)
        {
            if (comp == null || CountActive() >= MaxActive)
            {
                return false;
            }
            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("LivingWorld_TrafficCaravan");
            if (def == null)
            {
                return false;
            }

            List<Settlement> eligible = EligibleSettlements();
            if (eligible.Count < 2)
            {
                return false;
            }

            for (int attempt = 0; attempt < 6; attempt++)
            {
                Settlement origin = eligible.RandomElement();
                if (origin.Faction.def.caravanTraderKinds.NullOrEmpty()
                    || LivingWorldDiplomacy.FactionUnderTradeBlackout(origin.Faction))
                {
                    continue;
                }
                Settlement dest = PickDestination(comp, origin, eligible);
                if (dest == null)
                {
                    continue;
                }
                List<int> route = TryFindRoute(origin.Tile, dest.Tile);
                if (route == null)
                {
                    continue;
                }

                var caravan = (WorldObject_LivingWorldTraffic)WorldObjectMaker.MakeWorldObject(def);
                caravan.Tile = route[0];
                caravan.SetFaction(origin.Faction);
                caravan.route = route;
                caravan.routeIndex = 0;
                caravan.nextHopTick = Find.TickManager.TicksGame + TicksPerTile;
                caravan.destinationId = dest.ID;
                caravan.originLabel = origin.Label;
                caravan.destinationLabel = dest.Label;
                Find.WorldObjects.Add(caravan);
                return true;
            }
            return false;
        }

        internal static void Arrive(WorldObject_LivingWorldTraffic caravan)
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            Settlement dest = FindSettlement(caravan.destinationId);
            Faction faction = caravan.Faction;
            if (comp != null
                && dest?.Faction != null
                && faction != null
                && !faction.defeated
                && (dest.Faction == faction || !dest.Faction.HostileTo(faction)))
            {
                if (Rand.Chance(ProsperityBumpChance))
                {
                    SettlementMood mood = comp.GetOrCreateMood(dest);
                    mood.prosperity = Math.Min(2, mood.prosperity + 1);
                }
                comp.RecordAndPublish(
                    WorldEvent.Create(
                        WorldEventKind.TradeCaravan,
                        NewsSeverity.Minor,
                        faction,
                        dest.Faction == faction ? null : dest.Faction,
                        dest),
                    sendLetter: false);
            }
            caravan.Destroy();
        }

        internal static void MaybeDivert(WorldObject_LivingWorldTraffic caravan)
        {
            if (caravan.divertRolled)
            {
                return;
            }
            Faction faction = caravan.Faction;
            if (faction == null || faction.defeated || faction.HostileTo(Faction.OfPlayer))
            {
                return;
            }
            Map home = NearbyPlayerHome(caravan.Tile);
            if (home == null)
            {
                return;
            }
            caravan.divertRolled = true;
            if (!Rand.Chance(DivertChance))
            {
                return;
            }

            IncidentDef def = IncidentDefOf.TraderCaravanArrival;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, home);
            parms.faction = faction;
            parms.forced = true;
            if (!def.Worker.CanFireNow(parms) || !def.Worker.TryExecute(parms))
            {
                return;
            }

            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp != null)
            {
                WorldEvent ev = WorldEvent.Create(WorldEventKind.CaravanDiverted, NewsSeverity.Minor, faction);
                ev.settlementLabel = caravan.destinationLabel;
                ev.tile = home.Tile;
                ev.seenByPlayer = true;
                comp.RecordAndPublish(ev, sendLetter: false);
            }
            caravan.Destroy();
        }

        private static int CountActive()
        {
            int n = 0;
            List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is WorldObject_LivingWorldTraffic)
                {
                    n++;
                }
            }
            return n;
        }

        private static List<Settlement> EligibleSettlements()
        {
            var result = new List<Settlement>();
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                Settlement s = all[i];
                Faction f = s?.Faction;
                if (f == null || f.IsPlayer || f.defeated || f.def == null || f.def.hidden || !f.def.humanlikeFaction)
                {
                    continue;
                }
                result.Add(s);
            }
            return result;
        }

        private static Settlement PickDestination(
            GameComponent_LivingWorld comp,
            Settlement origin,
            List<Settlement> eligible)
        {
            List<Settlement> candidates = null;
            for (int i = 0; i < eligible.Count; i++)
            {
                Settlement s = eligible[i];
                if (s == origin)
                {
                    continue;
                }
                if (s.Faction != origin.Faction
                    && (s.Faction.HostileTo(origin.Faction) || AtWar(comp, s.Faction, origin.Faction)))
                {
                    continue;
                }
                float dist = Find.WorldGrid.ApproxDistanceInTiles(origin.Tile, s.Tile);
                if (dist < 2f || dist > MaxStraightDistance)
                {
                    continue;
                }
                candidates ??= new List<Settlement>();
                candidates.Add(s);
            }
            return candidates?.RandomElement();
        }

        private static bool AtWar(GameComponent_LivingWorld comp, Faction a, Faction b)
        {
            IReadOnlyList<FactionPairState> pairs = comp.Pairs;
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].tone == FactionRelationTone.War && pairs[i].Matches(a, b))
                {
                    return true;
                }
            }
            return false;
        }

        private static List<int> TryFindRoute(PlanetTile from, PlanetTile to)
        {
            WorldPath path = null;
            try
            {
                path = Find.WorldGrid.Surface.Pather.FindPath(from, to, null);
                if (path == null || !path.Found)
                {
                    return null;
                }
                List<PlanetTile> nodes = path.NodesReversed;
                if (nodes.Count < MinRouteTiles || nodes.Count > MaxRouteTiles)
                {
                    return null;
                }
                var route = new List<int>(nodes.Count);
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    route.Add(nodes[i]);
                }
                return route;
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Living World] Traffic route failed: " + e.Message, 0x4C57_0009);
                return null;
            }
            finally
            {
                if (path != null && path != WorldPath.NotFound)
                {
                    path.ReleaseToPool();
                }
            }
        }

        private static Settlement FindSettlement(int id)
        {
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].ID == id)
                {
                    return all[i];
                }
            }
            return null;
        }

        private static Map NearbyPlayerHome(PlanetTile tile)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map != null
                    && map.IsPlayerHome
                    && Find.WorldGrid.ApproxDistanceInTiles(map.Tile, tile) <= DivertRadiusTiles)
                {
                    return map;
                }
            }
            return null;
        }
    }
}

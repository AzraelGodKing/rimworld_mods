using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    public static class LivingWorldMorph
    {
        private const int MorphCooldownTicks = 60000 * 15; // ~15 days

        public enum MorphKind
        {
            ProsperityDrift,
            OwnershipFlip,
            Abandon,
            Outpost,
            Epithet,
        }

        public static bool TryResolveRandom(GameComponent_LivingWorld comp)
        {
            if (LivingWorldMod.Settings == null || !LivingWorldMod.Settings.morphEnabled)
            {
                return false;
            }
            // Check budget before work, but only consume on a successful morph.
            if (!comp.HasMorphBudget())
            {
                return false;
            }

            List<Settlement> settlements = EligibleSettlements();
            if (settlements.Count == 0)
            {
                return false;
            }

            // Weighted picks: prosperity often, abandon/outpost rarer. Ownership
            // flips are not random: they only follow battles (TryConquest).
            float roll = Rand.Value;
            bool ok;
            if (roll < 0.62f)
            {
                ok = TryProsperityDrift(comp, settlements.RandomElement());
            }
            else if (roll < 0.75f)
            {
                ok = TryAbandon(comp, settlements);
            }
            else if (roll < 0.90f)
            {
                ok = TryOutpost(comp, settlements);
            }
            else
            {
                ok = TryEpithet(comp, settlements.RandomElement());
            }

            if (ok)
            {
                comp.ConsumeMorphBudget();
            }
            return ok;
        }

        public static bool TryForce(GameComponent_LivingWorld comp, MorphKind kind)
        {
            List<Settlement> settlements = EligibleSettlements();
            if (settlements.Count == 0)
            {
                return false;
            }
            switch (kind)
            {
                case MorphKind.OwnershipFlip:
                    return TryForceConquest(comp);
                case MorphKind.Abandon:
                    return TryAbandon(comp, settlements, ignoreCooldown: true);
                case MorphKind.Outpost:
                    return TryOutpost(comp, settlements, ignoreCooldown: true);
                case MorphKind.Epithet:
                    return TryEpithet(comp, settlements.RandomElement(), ignoreCooldown: true);
                default:
                    return TryProsperityDrift(comp, settlements.RandomElement(), ignoreCooldown: true);
            }
        }

        private static readonly List<Settlement> eligibleBuffer = new List<Settlement>();

        /// <summary>Reused buffer: valid until the next call. Not cached across pulses
        /// because "in active use" (maps, caravans, quests) changes all the time.</summary>
        private static List<Settlement> EligibleSettlements()
        {
            eligibleBuffer.Clear();
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                Settlement s = all[i];
                if (s == null || s.Faction == null || s.Faction.IsPlayer || s.Faction.defeated)
                {
                    continue;
                }
                if (s.Faction.def.hidden || !s.Faction.def.humanlikeFaction)
                {
                    continue;
                }
                if (IsPlayerHomeTile(s.Tile) || InActiveUse(s))
                {
                    continue;
                }
                eligibleBuffer.Add(s);
            }
            return eligibleBuffer;
        }

        private static bool IsPlayerHomeTile(PlanetTile tile)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].IsPlayerHome && maps[i].Tile == tile)
                {
                    return true;
                }
            }
            return false;
        }

        // A generated map, a parked caravan, an open trade request, or a quest
        // look-target. Abandon destroys the settlement; a flip changes its faction.
        private static bool InActiveUse(Settlement settlement)
        {
            if (settlement.HasMap)
            {
                return true;
            }
            if (Find.WorldObjects.PlayerControlledCaravanAt(settlement.Tile) != null)
            {
                return true;
            }
            TradeRequestComp trade = settlement.GetComponent<TradeRequestComp>();
            if (trade?.ActiveRequest != null)
            {
                return true;
            }
            return QuestLooksAt(settlement);
        }

        private static bool QuestLooksAt(WorldObject worldObject)
        {
            List<Quest> quests = Find.QuestManager.QuestsListForReading;
            for (int i = 0; i < quests.Count; i++)
            {
                Quest quest = quests[i];
                if (quest == null)
                {
                    continue;
                }
                if (quest.State != QuestState.Ongoing && quest.State != QuestState.NotYetAccepted)
                {
                    continue;
                }
                foreach (GlobalTargetInfo target in quest.QuestLookTargets)
                {
                    if (target.HasWorldObject && target.WorldObject == worldObject)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool OnCooldown(GameComponent_LivingWorld comp, Settlement settlement, bool ignoreCooldown = false)
        {
            if (ignoreCooldown)
            {
                return false;
            }
            SettlementMood mood = comp.GetOrCreateMood(settlement);
            return Find.TickManager.TicksGame - mood.lastMorphTick < MorphCooldownTicks
                && mood.lastMorphTick > 0;
        }

        private static void TouchMorph(GameComponent_LivingWorld comp, Settlement settlement)
        {
            // Ownership or settlement count may have changed: refresh pair and count caches.
            comp.PairsDirty = true;
            LivingWorldWorldStats.Invalidate();
            SettlementMood mood = comp.GetOrCreateMood(settlement);
            mood.lastMorphTick = Find.TickManager.TicksGame;
        }

        private static bool TryProsperityDrift(
            GameComponent_LivingWorld comp,
            Settlement settlement,
            bool ignoreCooldown = false)
        {
            if (settlement == null || OnCooldown(comp, settlement, ignoreCooldown))
            {
                return false;
            }
            SettlementMood mood = comp.GetOrCreateMood(settlement);
            int delta = Rand.Bool ? 1 : -1;
            int next = mood.prosperity + delta;
            if (next > 2 || next < -2)
            {
                return false;
            }
            mood.prosperity = next;
            TouchMorph(comp, settlement);

            // Distinct FamineRumor when prosperity drifts into struggle/collapse.
            WorldEventKind kind;
            if (delta < 0 && mood.prosperity <= -1)
            {
                kind = WorldEventKind.FamineRumor;
            }
            else if (delta > 0)
            {
                kind = WorldEventKind.ProsperityRise;
            }
            else
            {
                kind = WorldEventKind.ProsperityFall;
            }
            NewsSeverity sev = System.Math.Abs(mood.prosperity) >= 2
                ? NewsSeverity.Normal
                : NewsSeverity.Minor;
            comp.RecordAndPublish(WorldEvent.Create(kind, sev, settlement.Faction, null, settlement));
            return true;
        }

        /// <summary>
        /// After a battle the winner takes the loser's settlement closest to its own
        /// lands. Settlements in use by the player are never taken.
        /// </summary>
        public static bool TryConquest(GameComponent_LivingWorld comp, Faction winner, Faction loser, bool ignoreCooldown = false)
        {
            if (comp == null || winner == null || loser == null || winner == loser
                || winner.defeated || winner.IsPlayer || loser.IsPlayer)
            {
                return false;
            }

            List<Settlement> all = Find.WorldObjects.Settlements;
            Settlement target = null;
            float best = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                Settlement s = all[i];
                if (s?.Faction != loser || IsPlayerHomeTile(s.Tile) || InActiveUse(s)
                    || OnCooldown(comp, s, ignoreCooldown))
                {
                    continue;
                }
                float d = NearestDistanceTo(winner, s, all);
                // Tile id breaks ties so every client picks the same settlement.
                if (d < best || (d == best && target != null && s.ID < target.ID))
                {
                    best = d;
                    target = s;
                }
            }
            if (target == null)
            {
                return false;
            }

            target.SetFaction(winner);
            TouchMorph(comp, target);
            SettlementMood mood = comp.GetOrCreateMood(target);
            mood.prosperity = System.Math.Max(-2, mood.prosperity - 1);

            comp.RecordAndPublish(WorldEvent.Create(
                WorldEventKind.OwnershipFlip,
                NewsSeverity.Major,
                winner,
                loser,
                target));

            // Last settlement taken: faction is gone from the rim.
            LivingWorldDiplomacy.MaybePublishFactionCollapse(comp, loser, winner, target);
            return true;
        }

        private static float NearestDistanceTo(Faction faction, Settlement target, List<Settlement> all)
        {
            float best = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                Settlement s = all[i];
                if (s?.Faction != faction || s.Tile.Layer != target.Tile.Layer)
                {
                    continue;
                }
                float d = Find.WorldGrid.ApproxDistanceInTiles(s.Tile, target.Tile);
                if (d < best)
                {
                    best = d;
                }
            }
            return best;
        }

        /// <summary>Debug: a war pair if there is one, else any pair; the stronger side conquers.</summary>
        private static bool TryForceConquest(GameComponent_LivingWorld comp)
        {
            LivingWorldDiplomacy.EnsurePairs(comp);
            List<FactionPairState> pairs = comp.PairsMutable;
            FactionPairState pick = null;
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].tone == FactionRelationTone.War)
                {
                    pick = pairs[i];
                    break;
                }
            }
            pick ??= pairs.RandomElementWithFallback();
            if (pick == null)
            {
                return false;
            }
            Faction a = pick.FactionA();
            Faction b = pick.FactionB();
            return TryConquest(comp, a, b, ignoreCooldown: true)
                || TryConquest(comp, b, a, ignoreCooldown: true);
        }

        private static bool TryAbandon(
            GameComponent_LivingWorld comp,
            List<Settlement> settlements,
            bool ignoreCooldown = false)
        {
            // Prefer factions that still have another settlement.
            Settlement target = settlements
                .Where(s => !OnCooldown(comp, s, ignoreCooldown)
                    && LivingWorldWorldStats.SettlementCount(s.Faction) >= 2)
                .RandomElementWithFallback();
            if (target == null)
            {
                return false;
            }

            Faction faction = target.Faction;
            string label = target.Label;
            PlanetTile tile = target.Tile;
            TouchMorph(comp, target);

            var ev = WorldEvent.Create(
                WorldEventKind.SettlementAbandoned,
                NewsSeverity.Major,
                faction,
                null,
                target);
            ev.settlementLabel = label;
            ev.Where = tile;

            target.Destroy();
            comp.RemoveMood(target);
            comp.RecordAndPublish(ev);
            return true;
        }

        private static bool TryOutpost(
            GameComponent_LivingWorld comp,
            List<Settlement> settlements,
            bool ignoreCooldown = false)
        {
            Settlement parent = settlements
                .Where(s => !OnCooldown(comp, s, ignoreCooldown)
                    && LivingWorldWorldStats.SettlementCount(s.Faction) < 4)
                .RandomElementWithFallback();
            if (parent?.Faction == null)
            {
                return false;
            }

            PlanetTile? tile = TryFindOutpostTile(parent.Tile);
            if (tile == null)
            {
                return false;
            }

            Settlement outpost = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
            outpost.Tile = tile.Value;
            outpost.SetFaction(parent.Faction);
            if (parent.Faction.def.settlementNameMaker != null)
            {
                outpost.Name = NameGenerator.GenerateName(parent.Faction.def.settlementNameMaker);
            }
            Find.WorldObjects.Add(outpost);
            TouchMorph(comp, parent);
            TouchMorph(comp, outpost);

            comp.RecordAndPublish(WorldEvent.Create(
                WorldEventKind.OutpostFounded,
                NewsSeverity.Normal,
                parent.Faction,
                null,
                outpost));
            return true;
        }

        private static PlanetTile? TryFindOutpostTile(PlanetTile near)
        {
            for (int i = 0; i < 40; i++)
            {
                if (!TileFinder.TryFindPassableTileWithTraversalDistance(
                    near,
                    3,
                    12,
                    out PlanetTile tile))
                {
                    continue;
                }
                if (Find.WorldObjects.AnySettlementAt(tile))
                {
                    continue;
                }
                if (!TileFinder.IsValidTileForNewSettlement(tile, null, false))
                {
                    continue;
                }
                if (TooCloseToPlayer(tile))
                {
                    continue;
                }
                return tile;
            }
            return null;
        }

        private const int OutpostMinTilesFromPlayer = 10;

        private static bool TooCloseToPlayer(PlanetTile tile)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].IsPlayerHome && NearPlayerTile(tile, maps[i].Tile))
                {
                    return true;
                }
            }
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement settlement = settlements[i];
                if (settlement?.Faction != null
                    && settlement.Faction.IsPlayer
                    && NearPlayerTile(tile, settlement.Tile))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool NearPlayerTile(PlanetTile tile, PlanetTile playerTile)
        {
            int dist = LivingWorldTiles.Distance(playerTile, tile, OutpostMinTilesFromPlayer);
            return dist >= 0 && dist < OutpostMinTilesFromPlayer;
        }

        private static bool TryEpithet(
            GameComponent_LivingWorld comp,
            Settlement settlement,
            bool ignoreCooldown = false)
        {
            if (settlement == null || OnCooldown(comp, settlement, ignoreCooldown))
            {
                return false;
            }
            SettlementMood mood = comp.GetOrCreateMood(settlement);
            string[] keys =
            {
                "LivingWorld_Epithet_Fortress",
                "LivingWorld_Epithet_Market",
                "LivingWorld_Epithet_Watch",
                "LivingWorld_Epithet_Haven",
                "LivingWorld_Epithet_Ruinous",
            };
            mood.epithet = keys.RandomElement();
            TouchMorph(comp, settlement);
            comp.RecordAndPublish(WorldEvent.Create(
                WorldEventKind.Epithet,
                NewsSeverity.Minor,
                settlement.Faction,
                null,
                settlement));
            return true;
        }
    }
}

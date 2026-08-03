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
            if (!comp.TryConsumeMorphBudget())
            {
                return false;
            }

            List<Settlement> settlements = EligibleSettlements().ToList();
            if (settlements.Count == 0)
            {
                return false;
            }

            // Weighted picks: prosperity often, flip/abandon/outpost rarer.
            float roll = Rand.Value;
            if (roll < 0.55f)
            {
                return TryProsperityDrift(comp, settlements.RandomElement());
            }
            if (roll < 0.75f)
            {
                return TryOwnershipFlip(comp, settlements);
            }
            if (roll < 0.85f)
            {
                return TryAbandon(comp, settlements);
            }
            if (roll < 0.93f)
            {
                return TryOutpost(comp, settlements);
            }
            return TryEpithet(comp, settlements.RandomElement());
        }

        public static bool TryForce(GameComponent_LivingWorld comp, MorphKind kind)
        {
            List<Settlement> settlements = EligibleSettlements().ToList();
            if (settlements.Count == 0)
            {
                return false;
            }
            switch (kind)
            {
                case MorphKind.OwnershipFlip:
                    return TryOwnershipFlip(comp, settlements, ignoreBudget: true);
                case MorphKind.Abandon:
                    return TryAbandon(comp, settlements, ignoreBudget: true);
                case MorphKind.Outpost:
                    return TryOutpost(comp, settlements, ignoreBudget: true);
                case MorphKind.Epithet:
                    return TryEpithet(comp, settlements.RandomElement(), ignoreBudget: true);
                default:
                    return TryProsperityDrift(comp, settlements.RandomElement(), ignoreBudget: true);
            }
        }

        private static IEnumerable<Settlement> EligibleSettlements()
        {
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
                if (IsPlayerHomeTile(s.Tile))
                {
                    continue;
                }
                yield return s;
            }
        }

        private static bool IsPlayerHomeTile(int tile)
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

        private static bool OnCooldown(GameComponent_LivingWorld comp, Settlement settlement)
        {
            SettlementMood mood = comp.GetOrCreateMood(settlement);
            return Find.TickManager.TicksGame - mood.lastMorphTick < MorphCooldownTicks
                && mood.lastMorphTick > 0;
        }

        private static void TouchMorph(GameComponent_LivingWorld comp, Settlement settlement)
        {
            SettlementMood mood = comp.GetOrCreateMood(settlement);
            mood.lastMorphTick = Find.TickManager.TicksGame;
        }

        private static bool TryProsperityDrift(
            GameComponent_LivingWorld comp,
            Settlement settlement,
            bool ignoreBudget = false)
        {
            if (settlement == null || OnCooldown(comp, settlement))
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

            WorldEventKind kind = delta > 0 ? WorldEventKind.ProsperityRise : WorldEventKind.ProsperityFall;
            NewsSeverity sev = System.Math.Abs(mood.prosperity) >= 2
                ? NewsSeverity.Normal
                : NewsSeverity.Minor;
            comp.RecordAndPublish(WorldEvent.Create(kind, sev, settlement.Faction, null, settlement));
            return true;
        }

        private static bool TryOwnershipFlip(
            GameComponent_LivingWorld comp,
            List<Settlement> settlements,
            bool ignoreBudget = false)
        {
            Settlement target = settlements.Where(s => !OnCooldown(comp, s)).RandomElementWithFallback();
            if (target == null)
            {
                return false;
            }

            Faction winner = Find.FactionManager.AllFactionsListForReading
                .Where(f => f != null
                    && !f.IsPlayer
                    && !f.defeated
                    && f.def.humanlikeFaction
                    && !f.def.hidden
                    && f != target.Faction)
                .RandomElementWithFallback();
            if (winner == null)
            {
                return false;
            }

            Faction loser = target.Faction;
            target.SetFaction(winner);
            TouchMorph(comp, target);
            SettlementMood mood = comp.GetOrCreateMood(target);
            mood.prosperity = 0;

            comp.RecordAndPublish(WorldEvent.Create(
                WorldEventKind.OwnershipFlip,
                NewsSeverity.Major,
                winner,
                loser,
                target));
            return true;
        }

        private static bool TryAbandon(
            GameComponent_LivingWorld comp,
            List<Settlement> settlements,
            bool ignoreBudget = false)
        {
            // Prefer factions that still have another settlement.
            Settlement target = settlements
                .Where(s => !OnCooldown(comp, s)
                    && settlements.Count(o => o.Faction == s.Faction) >= 2)
                .RandomElementWithFallback();
            if (target == null)
            {
                return false;
            }

            Faction faction = target.Faction;
            string label = target.Label;
            int tile = target.Tile;
            TouchMorph(comp, target);

            var ev = WorldEvent.Create(
                WorldEventKind.SettlementAbandoned,
                NewsSeverity.Major,
                faction,
                null,
                target);
            ev.settlementLabel = label;
            ev.tile = tile;

            target.Destroy();
            comp.RemoveMood(target);
            comp.RecordAndPublish(ev);
            return true;
        }

        private static bool TryOutpost(
            GameComponent_LivingWorld comp,
            List<Settlement> settlements,
            bool ignoreBudget = false)
        {
            Settlement parent = settlements
                .Where(s => !OnCooldown(comp, s)
                    && settlements.Count(o => o.Faction == s.Faction) < 4)
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
                return tile;
            }
            return null;
        }

        private static bool TryEpithet(
            GameComponent_LivingWorld comp,
            Settlement settlement,
            bool ignoreBudget = false)
        {
            if (settlement == null || OnCooldown(comp, settlement))
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

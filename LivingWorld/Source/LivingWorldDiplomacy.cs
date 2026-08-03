using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    public static class LivingWorldDiplomacy
    {
        private const int PairCooldownTicks = 60000 * 3; // ~3 days between resolves on a pair

        public static void EnsurePairs(GameComponent_LivingWorld comp)
        {
            List<Faction> factions = EligibleFactions().ToList();
            for (int i = 0; i < factions.Count; i++)
            {
                for (int j = i + 1; j < factions.Count; j++)
                {
                    if (comp.GetOrCreatePair(factions[i], factions[j]) == null)
                    {
                        // GetOrCreatePair always creates when missing.
                    }
                }
            }

            // Drop pairs whose factions vanished / lost settlements.
            List<FactionPairState> pairs = comp.PairsMutable;
            for (int i = pairs.Count - 1; i >= 0; i--)
            {
                Faction a = pairs[i].FactionA();
                Faction b = pairs[i].FactionB();
                if (!IsEligible(a) || !IsEligible(b))
                {
                    pairs.RemoveAt(i);
                }
            }
        }

        public static bool TryResolveRandom(GameComponent_LivingWorld comp)
        {
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (settings == null || !settings.diplomacyEnabled)
            {
                return false;
            }

            EnsurePairs(comp);
            List<FactionPairState> candidates = comp.PairsMutable
                .Where(p => Find.TickManager.TicksGame - p.lastResolveTick >= PairCooldownTicks
                    || p.lastResolveTick <= 0)
                .ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            FactionPairState pair = candidates.RandomElement();
            return TryResolvePair(comp, pair);
        }

        public static bool TryResolvePair(GameComponent_LivingWorld comp, FactionPairState pair)
        {
            if (pair == null)
            {
                return false;
            }
            Faction a = pair.FactionA();
            Faction b = pair.FactionB();
            if (!IsEligible(a) || !IsEligible(b))
            {
                return false;
            }

            LivingWorldSettings settings = LivingWorldMod.Settings;
            float rate = settings?.warRate ?? 1f;
            pair.lastResolveTick = Find.TickManager.TicksGame;

            switch (pair.tone)
            {
                case FactionRelationTone.Peace:
                    return ResolvePeace(comp, pair, a, b, rate);
                case FactionRelationTone.Tension:
                    return ResolveTension(comp, pair, a, b, rate);
                case FactionRelationTone.War:
                    return ResolveWar(comp, pair, a, b, rate);
                case FactionRelationTone.Alliance:
                    return ResolveAlliance(comp, pair, a, b, rate);
                default:
                    return false;
            }
        }

        public static bool ForceTone(
            GameComponent_LivingWorld comp,
            FactionRelationTone tone,
            bool thenBattle = false)
        {
            EnsurePairs(comp);
            FactionPairState pair = comp.PairsMutable.RandomElementWithFallback();
            if (pair == null)
            {
                return false;
            }
            Faction a = pair.FactionA();
            Faction b = pair.FactionB();
            if (a == null || b == null)
            {
                return false;
            }

            if (tone == FactionRelationTone.War
                && CountWars(comp) >= (LivingWorldMod.Settings?.maxConcurrentWars ?? 2)
                && pair.tone != FactionRelationTone.War)
            {
                // Replace oldest war if at cap.
                FactionPairState oldest = OldestWar(comp);
                if (oldest != null && oldest != pair)
                {
                    oldest.tone = FactionRelationTone.Tension;
                    oldest.intensity = 1;
                }
                else if (pair.tone != FactionRelationTone.War)
                {
                    return false;
                }
            }

            pair.tone = tone;
            pair.intensity = tone == FactionRelationTone.Peace ? 0 : 1;
            pair.lastResolveTick = Find.TickManager.TicksGame;

            if (thenBattle && tone == FactionRelationTone.War)
            {
                return ResolveBattle(comp, pair, a, b, forceMajor: true);
            }

            WorldEventKind kind = tone switch
            {
                FactionRelationTone.War => WorldEventKind.Skirmish,
                FactionRelationTone.Alliance => WorldEventKind.Alliance,
                FactionRelationTone.Tension => WorldEventKind.Skirmish,
                _ => WorldEventKind.Skirmish,
            };
            Settlement site = PickSettlementNear(a, b);
            comp.RecordAndPublish(WorldEvent.Create(
                kind,
                tone == FactionRelationTone.War ? NewsSeverity.Normal : NewsSeverity.Minor,
                a,
                b,
                site));
            return true;
        }

        private static bool ResolvePeace(
            GameComponent_LivingWorld comp,
            FactionPairState pair,
            Faction a,
            Faction b,
            float rate)
        {
            float roll = Rand.Value;
            // Rare formal pact.
            if (roll < 0.04f * rate)
            {
                pair.tone = FactionRelationTone.Alliance;
                pair.intensity = 1;
                Settlement site = PickSettlementNear(a, b);
                comp.RecordAndPublish(WorldEvent.Create(
                    WorldEventKind.Alliance,
                    NewsSeverity.Normal,
                    a,
                    b,
                    site));
                return true;
            }
            // Border friction → tension.
            if (roll < 0.04f * rate + 0.22f * rate)
            {
                pair.tone = FactionRelationTone.Tension;
                pair.intensity = 1;
                Settlement site = PickSettlementNear(a, b);
                comp.RecordAndPublish(WorldEvent.Create(
                    WorldEventKind.Skirmish,
                    NewsSeverity.Minor,
                    a,
                    b,
                    site));
                return true;
            }
            return false;
        }

        private static bool ResolveTension(
            GameComponent_LivingWorld comp,
            FactionPairState pair,
            Faction a,
            Faction b,
            float rate)
        {
            float roll = Rand.Value;
            // Cool off toward peace.
            if (roll < 0.28f)
            {
                pair.intensity = System.Math.Max(0, pair.intensity - 1);
                if (pair.intensity <= 0)
                {
                    pair.tone = FactionRelationTone.Peace;
                    pair.intensity = 0;
                }
                return true;
            }
            // Escalate to war.
            if (roll < 0.28f + 0.18f * rate)
            {
                int maxWars = LivingWorldMod.Settings?.maxConcurrentWars ?? 2;
                if (CountWars(comp) >= maxWars)
                {
                    pair.intensity = System.Math.Min(3, pair.intensity + 1);
                    return Skirmish(comp, pair, a, b);
                }
                pair.tone = FactionRelationTone.War;
                pair.intensity = System.Math.Max(1, pair.intensity);
                Settlement site = PickSettlementNear(a, b);
                comp.RecordAndPublish(WorldEvent.Create(
                    WorldEventKind.Skirmish,
                    NewsSeverity.Normal,
                    a,
                    b,
                    site));
                return true;
            }
            // Skirmish under tension.
            if (roll < 0.28f + 0.18f * rate + 0.35f)
            {
                pair.intensity = System.Math.Min(3, pair.intensity + 1);
                return Skirmish(comp, pair, a, b);
            }
            return false;
        }

        private static bool ResolveWar(
            GameComponent_LivingWorld comp,
            FactionPairState pair,
            Faction a,
            Faction b,
            float rate)
        {
            float roll = Rand.Value;
            // White peace.
            if (roll < 0.20f)
            {
                pair.tone = FactionRelationTone.Tension;
                pair.intensity = 1;
                Settlement site = PickSettlementNear(a, b);
                WorldEvent ev = WorldEvent.Create(
                    WorldEventKind.WhitePeace,
                    NewsSeverity.Normal,
                    a,
                    b,
                    site);
                comp.RecordAndPublish(ev);
                return true;
            }
            // Battle.
            if (roll < 0.20f + 0.55f * System.Math.Max(0.35f, rate))
            {
                return ResolveBattle(comp, pair, a, b, forceMajor: false);
            }
            return false;
        }

        private static bool ResolveAlliance(
            GameComponent_LivingWorld comp,
            FactionPairState pair,
            Faction a,
            Faction b,
            float rate)
        {
            float roll = Rand.Value;
            // Rare betrayal.
            if (roll < 0.08f * rate)
            {
                int maxWars = LivingWorldMod.Settings?.maxConcurrentWars ?? 2;
                if (CountWars(comp) >= maxWars && pair.tone != FactionRelationTone.War)
                {
                    FactionPairState oldest = OldestWar(comp);
                    if (oldest != null)
                    {
                        oldest.tone = FactionRelationTone.Tension;
                        oldest.intensity = 1;
                    }
                }
                pair.tone = FactionRelationTone.War;
                pair.intensity = 2;
                Settlement site = PickSettlementNear(a, b);
                WorldEvent ev = WorldEvent.Create(
                    WorldEventKind.Betrayal,
                    NewsSeverity.Major,
                    a,
                    b,
                    site);
                comp.RecordAndPublish(ev);
                MaybeEnqueueFallout(comp, pair, loser: b, winner: a, site, NewsSeverity.Major, seen: true);
                return true;
            }
            // Soft dissolve toward peace.
            if (roll < 0.08f * rate + 0.12f)
            {
                pair.tone = FactionRelationTone.Peace;
                pair.intensity = 0;
                return true;
            }
            return false;
        }

        private static bool Skirmish(
            GameComponent_LivingWorld comp,
            FactionPairState pair,
            Faction a,
            Faction b)
        {
            Settlement site = PickSettlementNear(a, b);
            if (site != null)
            {
                SettlementMood mood = comp.GetOrCreateMood(site);
                mood.prosperity = System.Math.Max(-2, mood.prosperity - 1);
            }
            comp.RecordAndPublish(WorldEvent.Create(
                WorldEventKind.Skirmish,
                NewsSeverity.Normal,
                a,
                b,
                site));
            return true;
        }

        private static bool ResolveBattle(
            GameComponent_LivingWorld comp,
            FactionPairState pair,
            Faction a,
            Faction b,
            bool forceMajor)
        {
            bool aWins = Rand.Bool;
            Faction winner = aWins ? a : b;
            Faction loser = aWins ? b : a;
            Settlement site = PickSettlementOf(loser) ?? PickSettlementNear(a, b);

            if (site != null)
            {
                SettlementMood loserMood = comp.GetOrCreateMood(site);
                loserMood.prosperity = System.Math.Max(-2, loserMood.prosperity - 2);
                Settlement winSite = PickSettlementOf(winner);
                if (winSite != null)
                {
                    SettlementMood winMood = comp.GetOrCreateMood(winSite);
                    winMood.prosperity = System.Math.Min(2, winMood.prosperity + 1);
                }
            }

            pair.intensity = System.Math.Min(3, pair.intensity + 1);
            NewsSeverity sev = forceMajor || pair.intensity >= 2
                ? NewsSeverity.Major
                : NewsSeverity.Normal;

            WorldEvent ev = WorldEvent.Create(
                WorldEventKind.DecisiveVictory,
                sev,
                winner,
                loser,
                site);
            comp.RecordAndPublish(ev);

            // Trade blackout on total war (optional, cautious default).
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (settings != null && settings.tradeBlackoutEnabled && pair.intensity >= 2)
            {
                pair.tradeBlackoutUntilTick = Find.TickManager.TicksGame + 60000 * 8;
                WorldEvent blackout = WorldEvent.Create(
                    WorldEventKind.TradeBlackout,
                    NewsSeverity.Normal,
                    winner,
                    loser,
                    site);
                comp.RecordAndPublish(blackout);
            }

            // Generic war site (LW8).
            if (settings != null && settings.warSitesEnabled && sev == NewsSeverity.Major)
            {
                LivingWorldWarSites.TrySpawnNear(site?.Tile ?? -1, winner, loser);
            }

            MaybeEnqueueFallout(comp, pair, loser, winner, site, sev, ev.seenByPlayer);
            return true;
        }

        private static void MaybeEnqueueFallout(
            GameComponent_LivingWorld comp,
            FactionPairState pair,
            Faction loser,
            Faction winner,
            Settlement site,
            NewsSeverity sev,
            bool seen)
        {
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (settings == null || !settings.falloutEnabled)
            {
                return;
            }
            if (sev < NewsSeverity.Major && !seen)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (pair.lastFalloutTick > 0 && now - pair.lastFalloutTick < 60000 * 20)
            {
                return;
            }

            pair.lastFalloutTick = now;
            comp.EnqueueFallout(new PendingFallout
            {
                loserFactionId = loser?.loadID ?? -1,
                winnerFactionId = winner?.loadID ?? -1,
                tile = site?.Tile ?? -1,
                settlementLabel = site?.Label,
                enqueueTick = now,
                kind = FalloutKind.Refugees,
            });

            // Optional warband — default rare / off via setting.
            if (settings.warbandFalloutEnabled && Rand.Chance(0.25f))
            {
                comp.EnqueueFallout(new PendingFallout
                {
                    loserFactionId = loser?.loadID ?? -1,
                    winnerFactionId = winner?.loadID ?? -1,
                    tile = site?.Tile ?? -1,
                    settlementLabel = site?.Label,
                    enqueueTick = now,
                    kind = FalloutKind.Warband,
                });
            }
        }

        public static int CountWars(GameComponent_LivingWorld comp)
        {
            int n = 0;
            List<FactionPairState> pairs = comp.PairsMutable;
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].tone == FactionRelationTone.War)
                {
                    n++;
                }
            }
            return n;
        }

        private static FactionPairState OldestWar(GameComponent_LivingWorld comp)
        {
            FactionPairState best = null;
            int bestTick = int.MaxValue;
            List<FactionPairState> pairs = comp.PairsMutable;
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].tone != FactionRelationTone.War)
                {
                    continue;
                }
                if (pairs[i].lastResolveTick < bestTick)
                {
                    bestTick = pairs[i].lastResolveTick;
                    best = pairs[i];
                }
            }
            return best;
        }

        public static bool FactionUnderTradeBlackout(Faction faction)
        {
            if (faction == null)
            {
                return false;
            }
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return false;
            }
            List<FactionPairState> pairs = comp.PairsMutable;
            for (int i = 0; i < pairs.Count; i++)
            {
                FactionPairState p = pairs[i];
                if (!p.TradeBlackoutActive)
                {
                    continue;
                }
                if (p.factionAId == faction.loadID || p.factionBId == faction.loadID)
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<Faction> EligibleFactions()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (IsEligible(all[i]))
                {
                    yield return all[i];
                }
            }
        }

        private static bool IsEligible(Faction f)
        {
            if (f == null || f.IsPlayer || f.defeated || f.def == null)
            {
                return false;
            }
            if (f.def.hidden || !f.def.humanlikeFaction)
            {
                return false;
            }
            return SettlementCount(f) >= 1;
        }

        private static int SettlementCount(Faction f)
        {
            int n = 0;
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i]?.Faction == f)
                {
                    n++;
                }
            }
            return n;
        }

        private static Settlement PickSettlementOf(Faction f)
        {
            if (f == null)
            {
                return null;
            }
            return Find.WorldObjects.Settlements
                .Where(s => s?.Faction == f)
                .RandomElementWithFallback();
        }

        private static Settlement PickSettlementNear(Faction a, Faction b)
        {
            return PickSettlementOf(a) ?? PickSettlementOf(b);
        }
    }
}

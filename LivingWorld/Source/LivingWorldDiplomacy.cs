using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    public static class LivingWorldDiplomacy
    {
        private const int PairCooldownTicks = 60000 * 3; // ~3 days between resolves on a pair

        /// <summary>
        /// Creates pairs for new factions and drops pairs for factions that left.
        /// Skipped unless settlements or factions changed since the last run.
        /// </summary>
        public static void EnsurePairs(GameComponent_LivingWorld comp, bool force = false)
        {
            int signature = LivingWorldWorldStats.Signature();
            if (!force && !comp.PairsDirty && signature == comp.PairSignature)
            {
                return;
            }
            comp.PairSignature = signature;
            comp.PairsDirty = false;

            factionBuffer.Clear();
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (IsEligible(all[i]))
                {
                    factionBuffer.Add(all[i]);
                }
            }
            List<Faction> factions = factionBuffer;
            for (int i = 0; i < factions.Count; i++)
            {
                for (int j = i + 1; j < factions.Count; j++)
                {
                    comp.GetOrCreatePair(factions[i], factions[j]);
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
                    // Publish once when a tracked NPC faction leaves the rim.
                    if (!IsEligible(a))
                    {
                        MaybePublishFactionCollapse(comp, a);
                    }
                    if (!IsEligible(b))
                    {
                        MaybePublishFactionCollapse(comp, b);
                    }
                    comp.RemovePairAt(i);
                }
            }
        }

        private static readonly List<Faction> factionBuffer = new List<Faction>();
        private static readonly List<FactionPairState> candidateBuffer = new List<FactionPairState>();

        /// <summary>
        /// Major Collapse when an NPC faction is defeated or has no settlements left.
        /// Deduped via GameComponent collapsed-faction ids.
        /// </summary>
        public static void MaybePublishFactionCollapse(
            GameComponent_LivingWorld comp,
            Faction faction,
            Faction victor = null,
            Settlement lastSite = null)
        {
            if (comp == null || faction == null || faction.IsPlayer)
            {
                return;
            }
            if (!faction.defeated && SettlementCount(faction) > 0)
            {
                return;
            }
            if (!comp.TryMarkFactionCollapsed(faction))
            {
                return;
            }

            WorldEvent ev = WorldEvent.Create(
                WorldEventKind.Collapse,
                NewsSeverity.Major,
                faction,
                victor,
                lastSite);
            if (lastSite == null && string.IsNullOrEmpty(ev.settlementLabel))
            {
                ev.settlementLabel = faction.Name;
            }
            comp.RecordAndPublish(ev);
        }

        public static bool TryResolveRandom(GameComponent_LivingWorld comp)
        {
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (settings == null || !settings.diplomacyEnabled)
            {
                return false;
            }

            EnsurePairs(comp);
            int now = Find.TickManager.TicksGame;
            List<FactionPairState> pairs = comp.PairsMutable;
            candidateBuffer.Clear();
            for (int i = 0; i < pairs.Count; i++)
            {
                FactionPairState p = pairs[i];
                if (now - p.lastResolveTick >= PairCooldownTicks || p.lastResolveTick <= 0)
                {
                    candidateBuffer.Add(p);
                }
            }
            if (candidateBuffer.Count == 0)
            {
                return false;
            }

            FactionPairState pair = candidateBuffer.RandomElement();
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

            if (tone == FactionRelationTone.Alliance && !CanAlly(a, b))
            {
                return false;
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
            // Rare formal pact, never between factions vanilla keeps at odds.
            if (roll < 0.04f * rate && CanAlly(a, b))
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
            // Cool off toward peace. Factions vanilla keeps hostile stay at least tense.
            if (roll < 0.28f)
            {
                pair.intensity = System.Math.Max(a.HostileTo(b) ? 1 : 0, pair.intensity - 1);
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
            if (!CanAlly(a, b))
            {
                // Vanilla turned them against each other: the pact quietly ends.
                pair.tone = FactionRelationTone.Tension;
                pair.intensity = 1;
                return true;
            }
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
            Strength(comp, a, out int countA, out int prosA);
            Strength(comp, b, out int countB, out int prosB);
            bool aWins = Rand.Chance(LivingWorldRules.BattleOddsA(countA, countB, prosA, prosB));
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
            if (settings != null && settings.warSitesEnabled && sev == NewsSeverity.Major && site != null)
            {
                LivingWorldWarSites.TrySpawnNear(site.Tile, winner, loser);
            }

            MaybeEnqueueFallout(comp, pair, loser, winner, site, sev, ev.seenByPlayer);

            // Hard-fought wars move borders: settlements only change hands here.
            int loserCount = aWins ? countB : countA;
            if (LivingWorldRules.ConquestAllowed(pair.intensity, loserCount, IsProtected(loser))
                && Rand.Chance(LivingWorldRules.ConquestChance)
                && comp.HasMorphBudget()
                && LivingWorldMorph.TryConquest(comp, winner, loser))
            {
                comp.ConsumeMorphBudget();
            }
            return true;
        }

        /// <summary>Settlement count and summed prosperity of a faction.</summary>
        private static void Strength(GameComponent_LivingWorld comp, Faction f, out int count, out int prosperity)
        {
            count = 0;
            prosperity = 0;
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                Settlement s = all[i];
                if (s?.Faction != f)
                {
                    continue;
                }
                count++;
                prosperity += comp.TryGetMood(s)?.prosperity ?? 0;
            }
        }

        /// <summary>
        /// Factions the world needs: the Empire (royal titles) and anyone involved in
        /// an open quest. They can lose settlements but never their last one.
        /// </summary>
        internal static bool IsProtected(Faction f)
        {
            if (f == null)
            {
                return true;
            }
            if (f.def.HasRoyalTitles)
            {
                return true;
            }
            List<Quest> quests = Find.QuestManager.QuestsListForReading;
            for (int i = 0; i < quests.Count; i++)
            {
                Quest q = quests[i];
                if (q == null || (q.State != QuestState.Ongoing && q.State != QuestState.NotYetAccepted))
                {
                    continue;
                }
                foreach (Faction inv in q.InvolvedFactions)
                {
                    if (inv == f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// No pacts between factions vanilla keeps hostile, permanent enemies, or
        /// natural enemies such as pirates.
        /// </summary>
        internal static bool CanAlly(Faction a, Faction b)
        {
            if (a?.def == null || b?.def == null)
            {
                return false;
            }
            if (a.def.naturalEnemy || b.def.naturalEnemy)
            {
                return false;
            }
            if (a.def.PermanentlyHostileTo(b.def) || b.def.PermanentlyHostileTo(a.def))
            {
                return false;
            }
            return !a.HostileTo(b);
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
                Where = site?.Tile ?? PlanetTile.Invalid,
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
                    Where = site?.Tile ?? PlanetTile.Invalid,
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

        private static int SettlementCount(Faction f) => LivingWorldWorldStats.SettlementCount(f);

        private static Settlement PickSettlementOf(Faction f)
        {
            if (f == null)
            {
                return null;
            }
            int count = SettlementCount(f);
            if (count == 0)
            {
                return null;
            }
            // Pick the k-th settlement of this faction without building a list.
            int k = Rand.Range(0, count);
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i]?.Faction == f && k-- == 0)
                {
                    return all[i];
                }
            }
            return null;
        }

        private static Settlement PickSettlementNear(Faction a, Faction b)
        {
            return PickSettlementOf(a) ?? PickSettlementOf(b);
        }
    }
}

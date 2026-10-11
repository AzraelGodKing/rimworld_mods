using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// AZR-329 — enqueue debug mutations; drain on GameComponentTick (same MP-safer
    /// pattern as DeepColonyPlayerCommand; no Multiplayer.API dependency).
    /// </summary>
    public static class LivingWorldDebugCommand
    {
        private enum Kind : byte
        {
            RandomMorph = 0,
            MorphKind = 1,
            DiplomacyResolve = 2,
            ForceTone = 3,
            ForceWarBattle = 4,
            ForceRefugees = 5,
            FakeSkirmish = 6,
            SpawnTraffic = 7,
        }

        private struct Entry
        {
            public Kind kind;
            public int morphKind;
            public int tone;
            public int loserId;
            public int winnerId;
        }

        private static readonly List<Entry> queue = new List<Entry>();

        public static void EnqueueRandomMorph()
        {
            queue.Add(new Entry { kind = Kind.RandomMorph });
        }

        public static void EnqueueMorphKind(LivingWorldMorph.MorphKind morphKind)
        {
            queue.Add(new Entry { kind = Kind.MorphKind, morphKind = (int)morphKind });
        }

        public static void EnqueueDiplomacyResolve()
        {
            queue.Add(new Entry { kind = Kind.DiplomacyResolve });
        }

        public static void EnqueueForceTone(FactionRelationTone tone)
        {
            queue.Add(new Entry { kind = Kind.ForceTone, tone = (int)tone });
        }

        public static void EnqueueForceWarBattle()
        {
            queue.Add(new Entry { kind = Kind.ForceWarBattle });
        }

        public static void EnqueueForceRefugees(Faction loser, Faction winner)
        {
            queue.Add(new Entry
            {
                kind = Kind.ForceRefugees,
                loserId = loser?.loadID ?? -1,
                winnerId = winner?.loadID ?? -1,
            });
        }

        public static void EnqueueFakeSkirmish(Faction a, Faction b)
        {
            queue.Add(new Entry
            {
                kind = Kind.FakeSkirmish,
                loserId = a?.loadID ?? -1,
                winnerId = b?.loadID ?? -1,
            });
        }

        public static void EnqueueSpawnTraffic()
        {
            queue.Add(new Entry { kind = Kind.SpawnTraffic });
        }

        public static void Drain()
        {
            if (queue.Count == 0)
            {
                return;
            }
            List<Entry> batch = new List<Entry>(queue);
            queue.Clear();
            for (int i = 0; i < batch.Count; i++)
            {
                Apply(batch[i]);
            }
        }

        private static void Apply(Entry e)
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }

            switch (e.kind)
            {
                case Kind.RandomMorph:
                {
                    bool ok = LivingWorldMorph.TryResolveRandom(comp)
                        || LivingWorldMorph.TryForce(comp, LivingWorldMorph.MorphKind.ProsperityDrift);
                    Messages.Message(ok
                            ? "[Living World] Forced a morph resolution."
                            : "[Living World] Morph failed (no eligible settlements / budget).",
                        ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                        historical: false);
                    break;
                }
                case Kind.MorphKind:
                {
                    var morphKind = (LivingWorldMorph.MorphKind)e.morphKind;
                    bool ok = LivingWorldMorph.TryForce(comp, morphKind);
                    Messages.Message(ok
                            ? $"[Living World] Forced {morphKind}."
                            : $"[Living World] {morphKind} failed.",
                        ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                        historical: false);
                    break;
                }
                case Kind.DiplomacyResolve:
                {
                    bool ok = LivingWorldDiplomacy.TryResolveRandom(comp);
                    Messages.Message(ok
                            ? "[Living World] Forced a diplomacy resolution."
                            : "[Living World] Diplomacy resolve failed (no pairs / cool).",
                        ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                        historical: false);
                    break;
                }
                case Kind.ForceTone:
                {
                    var tone = (FactionRelationTone)e.tone;
                    bool ok = LivingWorldDiplomacy.ForceTone(comp, tone);
                    Messages.Message(ok
                            ? $"[Living World] Forced pair → {tone}."
                            : $"[Living World] Force {tone} failed.",
                        ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                        historical: false);
                    break;
                }
                case Kind.ForceWarBattle:
                {
                    bool ok = LivingWorldDiplomacy.ForceTone(comp, FactionRelationTone.War, thenBattle: true);
                    Messages.Message(ok
                            ? "[Living World] Forced war + battle."
                            : "[Living World] Force battle failed.",
                        ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                        historical: false);
                    break;
                }
                case Kind.ForceRefugees:
                {
                    ApplyForceRefugees(comp, e.loserId, e.winnerId);
                    break;
                }
                case Kind.FakeSkirmish:
                {
                    Faction a = FindFaction(e.loserId);
                    Faction b = FindFaction(e.winnerId);
                    WorldEvent ev = WorldEvent.Create(WorldEventKind.Skirmish, NewsSeverity.Normal, a, b);
                    comp.RecordAndPublish(ev);
                    Messages.Message("[Living World] Published fake skirmish.", MessageTypeDefOf.NeutralEvent,
                        historical: false);
                    break;
                }
                case Kind.SpawnTraffic:
                {
                    bool ok = LivingWorldTraffic.TrySpawn(comp);
                    Messages.Message(ok
                            ? "[Living World] Spawned a traffic caravan."
                            : "[Living World] Traffic spawn failed (cap / no friendly route).",
                        ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                        historical: false);
                    break;
                }
            }
        }

        private static void ApplyForceRefugees(GameComponent_LivingWorld comp, int loserId, int winnerId)
        {
            Map map = Find.AnyPlayerHomeMap;
            Faction loser = FindFaction(loserId);
            if (comp == null || map == null || loser == null)
            {
                Messages.Message("[Living World] No faction for refugees.", MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

            Faction winner = FindFaction(winnerId);
            comp.EnqueueFallout(new PendingFallout
            {
                loserFactionId = loser.loadID,
                winnerFactionId = winner?.loadID ?? -1,
                enqueueTick = Find.TickManager.TicksGame - 40000,
                kind = FalloutKind.Refugees,
                settlementLabel = "debug front",
            });

            IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail("LivingWorld_Refugees");
            if (def == null)
            {
                Messages.Message("[Living World] LivingWorld_Refugees def missing.", MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            parms.forced = true;
            bool ok = def.Worker.TryExecute(parms);
            Messages.Message(ok
                    ? "[Living World] Forced refugees."
                    : "[Living World] Refugee incident failed.",
                ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                historical: false);
        }

        private static Faction FindFaction(int loadId)
        {
            if (loadId < 0 || Find.FactionManager == null)
            {
                return null;
            }
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].loadID == loadId)
                {
                    return all[i];
                }
            }
            return null;
        }
    }
}

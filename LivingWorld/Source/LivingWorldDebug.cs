using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace LivingWorld
{
    [StaticConstructorOnStartup]
    public static class LivingWorldDebug
    {
        private const string Cat = "Living World";

        static LivingWorldDebug() { }

        [DebugAction(Cat, "Dump chronicle (last 20)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DumpChronicle()
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                Messages.Message("[Living World] No game component.", MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }
            Log.Message(comp.DumpChronicle());
            Messages.Message("[Living World] Chronicle dumped to log.", MessageTypeDefOf.NeutralEvent,
                historical: false);
        }

        [DebugAction(Cat, "Dump faction pairs",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DumpPairs()
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }
            Log.Message(comp.DumpPairs());
            Messages.Message("[Living World] Faction pairs dumped to log.", MessageTypeDefOf.NeutralEvent,
                historical: false);
        }

        [DebugAction(Cat, "Force random morph",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceRandomMorph()
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }
            bool ok = LivingWorldMorph.TryResolveRandom(comp)
                || LivingWorldMorph.TryForce(comp, LivingWorldMorph.MorphKind.ProsperityDrift);
            Messages.Message(ok
                    ? "[Living World] Forced a morph resolution."
                    : "[Living World] Morph failed (no eligible settlements / budget).",
                ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                historical: false);
        }

        [DebugAction(Cat, "Force ownership flip",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceOwnershipFlip()
        {
            ForceKind(LivingWorldMorph.MorphKind.OwnershipFlip);
        }

        [DebugAction(Cat, "Force abandon settlement",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceAbandon()
        {
            ForceKind(LivingWorldMorph.MorphKind.Abandon);
        }

        [DebugAction(Cat, "Force outpost",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceOutpost()
        {
            ForceKind(LivingWorldMorph.MorphKind.Outpost);
        }

        [DebugAction(Cat, "Force prosperity drift",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceProsperity()
        {
            ForceKind(LivingWorldMorph.MorphKind.ProsperityDrift);
        }

        [DebugAction(Cat, "Force diplomacy resolve",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceDiplomacy()
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }
            bool ok = LivingWorldDiplomacy.TryResolveRandom(comp);
            Messages.Message(ok
                    ? "[Living World] Forced a diplomacy resolution."
                    : "[Living World] Diplomacy resolve failed (no pairs / cool).",
                ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                historical: false);
        }

        [DebugAction(Cat, "Force tension (random pair)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceTension()
        {
            ForceTone(FactionRelationTone.Tension);
        }

        [DebugAction(Cat, "Force war (random pair)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceWar()
        {
            ForceTone(FactionRelationTone.War);
        }

        [DebugAction(Cat, "Force war + battle (random pair)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceWarBattle()
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }
            bool ok = LivingWorldDiplomacy.ForceTone(comp, FactionRelationTone.War, thenBattle: true);
            Messages.Message(ok
                    ? "[Living World] Forced war + battle."
                    : "[Living World] Force battle failed.",
                ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                historical: false);
        }

        [DebugAction(Cat, "Force refugees (enqueue + fire)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceRefugees()
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            Map map = Find.AnyPlayerHomeMap;
            if (comp == null || map == null)
            {
                return;
            }

            Faction loser = Find.FactionManager.AllFactionsVisible
                .FirstOrDefault(f => !f.IsPlayer && !f.defeated && f.def.humanlikeFaction);
            Faction winner = Find.FactionManager.AllFactionsVisible
                .FirstOrDefault(f => !f.IsPlayer && !f.defeated && f.def.humanlikeFaction && f != loser);
            if (loser == null)
            {
                Messages.Message("[Living World] No faction for refugees.", MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

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

        [DebugAction(Cat, "Force skirmish letter (fake event)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceSkirmishLetter()
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }
            Faction a = Find.FactionManager.AllFactionsVisible
                .FirstOrDefault(f => !f.IsPlayer && !f.defeated && f.def.humanlikeFaction);
            Faction b = Find.FactionManager.AllFactionsVisible
                .FirstOrDefault(f => !f.IsPlayer && !f.defeated && f.def.humanlikeFaction && f != a);
            WorldEvent ev = WorldEvent.Create(WorldEventKind.Skirmish, NewsSeverity.Normal, a, b);
            comp.RecordAndPublish(ev);
            Messages.Message("[Living World] Published fake skirmish.", MessageTypeDefOf.NeutralEvent,
                historical: false);
        }

        private static void ForceTone(FactionRelationTone tone)
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }
            bool ok = LivingWorldDiplomacy.ForceTone(comp, tone);
            Messages.Message(ok
                    ? $"[Living World] Forced pair → {tone}."
                    : $"[Living World] Force {tone} failed.",
                ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                historical: false);
        }

        private static void ForceKind(LivingWorldMorph.MorphKind kind)
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }
            bool ok = LivingWorldMorph.TryForce(comp, kind);
            Messages.Message(ok
                    ? $"[Living World] Forced {kind}."
                    : $"[Living World] {kind} failed.",
                ok ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                historical: false);
        }
    }
}

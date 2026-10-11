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
            // AZR-329 — enqueue; apply on next GameComponentTick.
            LivingWorldDebugCommand.EnqueueRandomMorph();
            Messages.Message("[Living World] Morph queued for next tick.", MessageTypeDefOf.NeutralEvent,
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
            LivingWorldDebugCommand.EnqueueDiplomacyResolve();
            Messages.Message("[Living World] Diplomacy resolve queued for next tick.",
                MessageTypeDefOf.NeutralEvent, historical: false);
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
            LivingWorldDebugCommand.EnqueueForceWarBattle();
            Messages.Message("[Living World] War + battle queued for next tick.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Force refugees (enqueue + fire)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceRefugees()
        {
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
            LivingWorldDebugCommand.EnqueueForceRefugees(loser, winner);
            Messages.Message("[Living World] Refugees queued for next tick.", MessageTypeDefOf.NeutralEvent,
                historical: false);
        }

        [DebugAction(Cat, "Force skirmish letter (fake event)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceSkirmishLetter()
        {
            Faction a = Find.FactionManager.AllFactionsVisible
                .FirstOrDefault(f => !f.IsPlayer && !f.defeated && f.def.humanlikeFaction);
            Faction b = Find.FactionManager.AllFactionsVisible
                .FirstOrDefault(f => !f.IsPlayer && !f.defeated && f.def.humanlikeFaction && f != a);
            LivingWorldDebugCommand.EnqueueFakeSkirmish(a, b);
            Messages.Message("[Living World] Fake skirmish queued for next tick.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Spawn traffic caravan",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnTraffic()
        {
            LivingWorldDebugCommand.EnqueueSpawnTraffic();
            Messages.Message("[Living World] Traffic caravan queued for next tick.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        private static void ForceTone(FactionRelationTone tone)
        {
            LivingWorldDebugCommand.EnqueueForceTone(tone);
            Messages.Message($"[Living World] Force {tone} queued for next tick.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        private static void ForceKind(LivingWorldMorph.MorphKind kind)
        {
            LivingWorldDebugCommand.EnqueueMorphKind(kind);
            Messages.Message($"[Living World] Force {kind} queued for next tick.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }
}

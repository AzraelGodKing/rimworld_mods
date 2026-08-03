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

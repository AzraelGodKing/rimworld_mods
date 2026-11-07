using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Soft event bus. Sibling mods register fail-open; Living World never depends on them.
    /// <para>
    /// Handlers run inside the sim tick, so they must be deterministic (Multiplayer) and cheap.
    /// Listen for <see cref="StableHookKinds"/> only; other kinds may change meaning between versions.
    /// Handlers always receive ground truth: <see cref="WorldEvent.kind"/> equals
    /// <see cref="WorldEvent.trueKind"/> and faction names are the real ones, even when the player
    /// heard a distorted rumour (<see cref="WorldEvent.distorted"/> is then true). A later
    /// <see cref="WorldEventKind.Correction"/> is raised when the player's record is fixed.
    /// Prefer <see cref="LivingWorldCompatApi"/> for queries.
    /// </para>
    /// </summary>
    public static class LivingWorldSignals
    {
        public delegate void WorldEventHandler(WorldEvent ev);

        /// <summary>
        /// Kinds sibling mods can rely on (Homesteader HS1, Nemesis N3, Deep Colony DC1).
        /// Enum members are saved by name: never rename or remove one.
        /// </summary>
        public static readonly IReadOnlyList<WorldEventKind> StableHookKinds = new[]
        {
            // Settlement life.
            WorldEventKind.ProsperityRise,
            WorldEventKind.ProsperityFall,
            WorldEventKind.FamineRumor,
            WorldEventKind.OwnershipFlip,
            WorldEventKind.SettlementAbandoned,
            WorldEventKind.OutpostFounded,
            // Faction crushed / gone from the rim (Major).
            WorldEventKind.Collapse,
            // Diplomacy and war.
            WorldEventKind.Skirmish,
            WorldEventKind.DecisiveVictory,
            WorldEventKind.WhitePeace,
            WorldEventKind.Alliance,
            WorldEventKind.Betrayal,
            WorldEventKind.TradeBlackout,
            // Player-facing fallout and traffic.
            WorldEventKind.RefugeeFlight,
            WorldEventKind.WarbandPass,
            WorldEventKind.TradeCaravan,
            WorldEventKind.CaravanDiverted,
        };

        // Field name read by older Azrael hub builds: keep "handlers".
        private static readonly List<WorldEventHandler> handlers = new List<WorldEventHandler>();

        internal static bool IsRegistered(Type declaringType)
        {
            for (int i = 0; i < handlers.Count; i++)
            {
                if (handlers[i].Method?.DeclaringType == declaringType)
                {
                    return true;
                }
            }
            return false;
        }

        public static void Register(WorldEventHandler handler)
        {
            if (handler == null || handlers.Contains(handler))
            {
                return;
            }
            handlers.Add(handler);
        }

        public static void Unregister(WorldEventHandler handler)
        {
            if (handler != null)
            {
                handlers.Remove(handler);
            }
        }

        internal static void Raise(WorldEvent ev)
        {
            if (ev == null || handlers.Count == 0)
            {
                return;
            }
            // Consumers react to what really happened, not to the rumour.
            WorldEvent truth = ev.distorted ? ev.TruthView() : ev;
            for (int i = 0; i < handlers.Count; i++)
            {
                try
                {
                    handlers[i](truth);
                }
                catch (Exception e)
                {
                    // One warning per broken handler, not one per event.
                    MethodInfo m = handlers[i].Method;
                    Log.WarningOnce(
                        "[Living World] Soft consumer " + m?.DeclaringType?.FullName + "." + m?.Name
                        + " threw: " + e.Message,
                        0x4C570000 ^ (m?.MetadataToken ?? 0) ^ (m?.Module.MetadataToken ?? 0) << 8);
                }
            }
        }

    }
}

using System;
using System.Collections.Generic;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Soft event bus. Sibling mods register fail-open; Living World never depends on them.
    /// <para>
    /// Handlers run inside the sim tick, so they must be deterministic (Multiplayer) and cheap.
    /// Listen for <see cref="StableHookKinds"/> only; other kinds may change meaning between versions.
    /// Rumours: <see cref="WorldEvent.kind"/> and faction names can be distorted on far news;
    /// read <see cref="WorldEvent.trueKind"/> for ground truth. A later <see cref="WorldEventKind.Correction"/>
    /// is raised when the record is fixed.
    /// </para>
    /// </summary>
    public static class LivingWorldSignals
    {
        public delegate void WorldEventHandler(WorldEvent ev);

        /// <summary>
        /// Kinds sibling mods can rely on (Homesteader HS1, Nemesis N3, Deep Colony DC1).
        /// Enum values are saved and stay fixed.
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

        public static bool IsStableHookKind(WorldEventKind kind)
        {
            for (int i = 0; i < StableHookKinds.Count; i++)
            {
                if (StableHookKinds[i] == kind)
                {
                    return true;
                }
            }
            return false;
        }

        private static readonly List<WorldEventHandler> handlers = new List<WorldEventHandler>();

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
            for (int i = 0; i < handlers.Count; i++)
            {
                try
                {
                    handlers[i](ev);
                }
                catch (Exception e)
                {
                    Log.WarningOnce(
                        "[Living World] Soft consumer threw: " + e.Message,
                        e.GetHashCode());
                }
            }
        }

        internal static void ResetSession()
        {
            // Registrations are process-lifetime; nothing to clear per save.
        }
    }
}

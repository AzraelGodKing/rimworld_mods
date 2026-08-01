using System;
using System.Collections.Generic;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Soft event bus. Sibling mods register fail-open; Living World never depends on them.
    /// </summary>
    public static class LivingWorldSignals
    {
        public delegate void WorldEventHandler(WorldEvent ev);

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

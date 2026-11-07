using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Stable surface for sister mods (Deep Colony, Nemesis, Homesteader, the Azrael
    /// hub). Call by reflection with no project reference; everything here is
    /// fail-open and read-only except <see cref="Register"/>. Members are only added,
    /// never renamed; <see cref="ApiVersion"/> goes up when something is added.
    /// </summary>
    public static class LivingWorldCompatApi
    {
        public const int ApiVersion = 1;

        /// <summary>A game is loaded and its Living World simulation is switched on.</summary>
        public static bool IsActive
        {
            get
            {
                GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
                return comp != null && LivingWorldMod.Settings?.enabled == true;
            }
        }

        /// <summary>
        /// Listen for world news. Handlers get ground truth (see <see cref="LivingWorldSignals"/>);
        /// only rely on <see cref="StableHookKinds"/>.
        /// </summary>
        public static void Register(LivingWorldSignals.WorldEventHandler handler) =>
            LivingWorldSignals.Register(handler);

        public static void Unregister(LivingWorldSignals.WorldEventHandler handler) =>
            LivingWorldSignals.Unregister(handler);

        /// <summary>True if a handler declared on <paramref name="declaringType"/> is registered.</summary>
        public static bool IsHandlerRegistered(Type declaringType) =>
            declaringType != null && LivingWorldSignals.IsRegistered(declaringType);

        public static IReadOnlyList<WorldEventKind> StableHookKinds => LivingWorldSignals.StableHookKinds;

        /// <summary>"Peace", "Tension", "War" or "Alliance" between two NPC factions; null if not tracked.</summary>
        public static string RelationTone(Faction a, Faction b) =>
            GameComponent_LivingWorld.Get?.TryGetPair(a, b)?.tone.ToString();

        public static bool AtWar(Faction a, Faction b) =>
            GameComponent_LivingWorld.Get?.TryGetPair(a, b)?.tone == FactionRelationTone.War;

        /// <summary>0 (calm) to 3 (total war) for the pair; 0 if not tracked.</summary>
        public static int Intensity(Faction a, Faction b) =>
            GameComponent_LivingWorld.Get?.TryGetPair(a, b)?.intensity ?? 0;

        /// <summary>The faction's traders are mostly cut off by a total war.</summary>
        public static bool UnderTradeBlackout(Faction faction) =>
            LivingWorldDiplomacy.FactionUnderTradeBlackout(faction);

        /// <summary>Settlement prosperity, -2 (collapsing) to +2 (thriving); 0 if Living World never touched it.</summary>
        public static int Prosperity(Settlement settlement) =>
            GameComponent_LivingWorld.Get?.TryGetMood(settlement)?.prosperity ?? 0;

        /// <summary>The settlement's epithet as a translated word, or null.</summary>
        public static string Epithet(Settlement settlement)
        {
            string key = GameComponent_LivingWorld.Get?.TryGetMood(settlement)?.epithet;
            return string.IsNullOrEmpty(key) ? null : key.Translate().ToString();
        }
    }
}

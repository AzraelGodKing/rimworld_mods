#nullable enable
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// The save's own gameplay settings (AZR-527, AZR-544). A new game copies the
    /// mod-settings defaults; a pre-2.0 save copies them on its first 2.0 load.
    /// Only changed through <see cref="HomesteaderSync.SetGameplay"/>, which
    /// Multiplayer runs on every client.
    /// </summary>
    public class GameComponent_HomesteaderGameplay : GameComponent
    {
        private static GameComponent_HomesteaderGameplay? instance;

        /// <summary>The running game's component; null at the main menu.</summary>
        internal static GameComponent_HomesteaderGameplay? Instance => Current.Game != null ? instance : null;

        private HomesteaderGameplaySettings values;

        private bool seeded;

        public GameComponent_HomesteaderGameplay(Game game)
        {
            instance = this;
            values = HomesteaderMod.Settings.gameplay.Copy();
            HomesteaderCaches.ResetForNewGame();
        }

        public HomesteaderGameplaySettings Values => values;

        internal void Set(HomesteaderGameplaySettings next)
        {
            next.Clamp();
            values = next;
            HomesteaderGameplayDefs.Apply(values);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref seeded, "hsGameplaySeeded", defaultValue: false);
            Scribe_Deep.Look(ref values, "hsGameplay");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!seeded || values == null)
                {
                    values = HomesteaderMod.Settings.gameplay.Copy();
                }

                values.Clamp();
                seeded = true;
            }
        }

        public override void StartedNewGame()
        {
            seeded = true;
        }

        public override void FinalizeInit()
        {
            instance = this;
            HomesteaderGameplayDefs.Apply(values);
        }
    }

    internal static class HomesteaderGameplay
    {
        /// <summary>The running save's settings, or the new-game defaults at the main menu.</summary>
        internal static HomesteaderGameplaySettings Active
        {
            get
            {
                return GameComponent_HomesteaderGameplay.Instance?.Values ?? HomesteaderMod.Settings.gameplay;
            }
        }
    }

    /// <summary>
    /// Pushes gameplay values into the defs Homesteader owns (thought moods, coop
    /// spawner, Kats incident). Originals are remembered once so factors never stack.
    /// </summary>
    internal static class HomesteaderGameplayDefs
    {
        private static readonly Dictionary<string, float> OriginalThoughtMood = new Dictionary<string, float>();

        private static IntRange? OriginalCoopEggInterval;

        private static float? OriginalKatsChance;

        internal static void Apply(HomesteaderGameplaySettings values)
        {
            ApplyThoughtMood("Homesteader_AteFavoriteFood", values.favoriteFoodMoodFactor);
            ApplyThoughtMood("Homesteader_AteAllergen", values.allergyFlareIntensity);
            ApplyCoopEggInterval(values.coopEggIntervalFactor);
            ApplyKatsChance(values.enableKatsEffect);
        }

        private static void ApplyThoughtMood(string defName, float factor)
        {
            ThoughtDef? def = DefDatabase<ThoughtDef>.GetNamedSilentFail(defName);
            if (def?.stages == null || def.stages.Count == 0)
            {
                return;
            }

            if (!OriginalThoughtMood.TryGetValue(defName, out float original))
            {
                original = def.stages[0].baseMoodEffect;
                OriginalThoughtMood[defName] = original;
            }

            def.stages[0].baseMoodEffect = original * factor;
        }

        private static void ApplyCoopEggInterval(float factor)
        {
            ThingDef? coop = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_ChickenCoop");
            if (coop?.comps == null)
            {
                return;
            }

            for (int i = 0; i < coop.comps.Count; i++)
            {
                if (!(coop.comps[i] is CompProperties_Spawner spawner) || spawner.saveKeysPrefix != "eggs")
                {
                    continue;
                }

                OriginalCoopEggInterval ??= spawner.spawnIntervalRange;
                IntRange original = OriginalCoopEggInterval.Value;
                spawner.spawnIntervalRange = new IntRange(
                    Mathf.Max(1, Mathf.RoundToInt(original.min * factor)),
                    Mathf.Max(1, Mathf.RoundToInt(original.max * factor)));
                return;
            }
        }

        private static void ApplyKatsChance(bool enabled)
        {
            IncidentDef? def = DefDatabase<IncidentDef>.GetNamedSilentFail("Homesteader_KatsEffect");
            if (def == null)
            {
                return;
            }

            OriginalKatsChance ??= def.baseChance;
            def.baseChance = enabled ? OriginalKatsChance.Value : 0f;
        }
    }

    /// <summary>
    /// Static caches keyed by map id or tick survive loading a different save,
    /// where map ids repeat and the tick can go backwards (AZR-527). The game
    /// component constructor runs once per new or loaded game and clears them.
    /// </summary>
    internal static class HomesteaderCaches
    {
        internal static void ResetForNewGame()
        {
            CoolingLookup.Clear();
            PantryUtility.Invalidate();
            PantryMakeUtility.Invalidate();
        }
    }
}

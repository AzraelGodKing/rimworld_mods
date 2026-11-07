using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Public read-only signals for sister mods (for example the Azrael storyteller).
    /// Call through reflection when Deep Colony may be absent; every member is safe
    /// outside a game and returns a neutral value.
    /// </summary>
    public static class DeepColonyApi
    {
        public const int ApiVersion = 1;

        /// <summary>A colony counts as mourning for this many days after a colonist dies.</summary>
        public const int MourningDays = 5;

        public static bool IsActive => true;

        /// <summary>
        /// Share of free colonists on player home maps who carry a Deep Colony trauma,
        /// from 0 (none) to 1 (all). Returns 0 when trauma is disabled or no game is loaded.
        /// </summary>
        public static float ColonyTraumaLoad()
        {
            if (Current.Game == null || !DeepColonySettings.Get.enableTrauma) return 0f;
            int total = 0;
            int hurt = 0;
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome) continue;
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    total++;
                    if (TraumaUtility.HasAnyTrauma(p)) hurt++;
                }
            }
            return total == 0 ? 0f : (float)hurt / total;
        }

        /// <summary>Whole days since the last colonist death, or -1 if none is remembered.</summary>
        public static int DaysSinceLastColonistDeath()
        {
            var entries = GameComp_DeepColony.Instance?.remembranceEntries;
            if (entries == null || entries.Count == 0) return -1;
            int last = entries[entries.Count - 1].deathTick;
            return (Find.TickManager.TicksGame - last) / GenDate.TicksPerDay;
        }

        /// <summary>True for <see cref="MourningDays"/> days after a colonist dies.</summary>
        public static bool IsColonyMourning
        {
            get
            {
                int days = DaysSinceLastColonistDeath();
                return days >= 0 && days < MourningDays;
            }
        }
    }
}

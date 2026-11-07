#nullable enable
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Vanilla spawner plus weather and bloom (AZR-576): rain catches fill faster in
    /// rain and stop during a drought (HS-A13, HS-S02); beehives idle with nothing in
    /// bloom nearby (HS-A08). Runs the vanilla countdown more or less often, so the
    /// saved countdown (by saveKeysPrefix) is unchanged and old saves keep progress.
    /// </summary>
    public class CompProperties_HomesteaderSpawner : CompProperties_Spawner
    {
        /// <summary>Extra speed at full rain: 1 = twice as fast in a downpour. 0 = rain does nothing.</summary>
        public float rainBoost = 0f;

        /// <summary>Stops while a drought condition is active (Odyssey drought).</summary>
        public bool pauseInDrought = false;

        /// <summary>Needs this many flowering plants within bloomRadius. 0 = no bloom check.</summary>
        public int bloomPlantsNeeded = 0;

        public float bloomRadius = 9.9f;

        public CompProperties_HomesteaderSpawner()
        {
            compClass = typeof(CompHomesteaderSpawner);
        }
    }

    public class CompHomesteaderSpawner : CompSpawner
    {
        private const int BloomCheckTicks = 1000;

        private float carry;
        private int bloomCheckedTick = -BloomCheckTicks;
        private bool bloomOk = true;

        private CompProperties_HomesteaderSpawner Props => (CompProperties_HomesteaderSpawner)props;

        public override void CompTick()
        {
            Advance(1);
        }

        public override void CompTickRare()
        {
            Advance(GenTicks.TickRareInterval);
        }

        private void Advance(int interval)
        {
            if (!parent.Spawned || Paused(refresh: true, out _))
            {
                return;
            }

            float speed = Speed();
            carry += speed;
            // Whole steps of the vanilla countdown; the fraction carries to the next tick.
            while (carry >= 1f)
            {
                carry -= 1f;
                if (interval == 1)
                {
                    base.CompTick();
                }
                else
                {
                    base.CompTickRare();
                }
            }
        }

        private float Speed()
        {
            if (Props.rainBoost <= 0f || !HomesteaderGameplay.Active.rainAwareWater)
            {
                return 1f;
            }

            Map map = parent.Map;
            if (parent.Position.Roofed(map))
            {
                return 1f;
            }

            return 1f + Props.rainBoost * Mathf.Clamp01(map.weatherManager.RainRate);
        }

        /// <summary>
        /// refresh is false from the inspect pane: UI must not move the bloom check,
        /// or Multiplayer clients would re-check on different ticks.
        /// </summary>
        private bool Paused(bool refresh, out string? reasonKey)
        {
            reasonKey = null;
            Map map = parent.Map;
            if (Props.pauseInDrought && HomesteaderGameplay.Active.rainAwareWater && DroughtActive(map))
            {
                reasonKey = "Homesteader_SpawnerDrought";
                return true;
            }

            if (Props.bloomPlantsNeeded > 0 && HomesteaderGameplay.Active.beesNeedBloom && !BloomNearby(map, refresh))
            {
                reasonKey = "Homesteader_SpawnerNoBloom";
                return true;
            }

            return false;
        }

        internal static bool DroughtActive(Map? map)
        {
            if (map == null)
            {
                return false;
            }

            GameConditionDef? drought = DefDatabase<GameConditionDef>.GetNamedSilentFail("Drought");
            GameConditionDef? droughtInitial = DefDatabase<GameConditionDef>.GetNamedSilentFail("DroughtInitial");
            return (drought != null && map.gameConditionManager.ConditionIsActive(drought))
                || (droughtInitial != null && map.gameConditionManager.ConditionIsActive(droughtInitial));
        }

        private bool BloomNearby(Map map, bool refresh)
        {
            if (!refresh)
            {
                return bloomOk;
            }

            int now = Find.TickManager.TicksGame;
            if (now - bloomCheckedTick < BloomCheckTicks && now >= bloomCheckedTick)
            {
                return bloomOk;
            }

            bloomCheckedTick = now;
            bloomOk = CountBlooming(map, Props.bloomPlantsNeeded) >= Props.bloomPlantsNeeded;
            return bloomOk;
        }

        /// <summary>
        /// Flowering = a living, leafy food, medicine or decorative plant at least half
        /// grown, in its growing season. Stops counting at <paramref name="enough"/>.
        /// </summary>
        private int CountBlooming(Map map, int enough)
        {
            int found = 0;
            int cells = GenRadial.NumCellsInRadius(Props.bloomRadius);
            IntVec3 center = parent.Position;
            for (int i = 0; i < cells && found < enough; i++)
            {
                IntVec3 c = center + GenRadial.RadialPattern[i];
                if (!c.InBounds(map))
                {
                    continue;
                }

                Plant? plant = c.GetPlant(map);
                if (plant?.def?.plant == null || plant.LeaflessNow || plant.Dying || plant.Growth < 0.5f
                    || plant.def.plant.purpose == PlantPurpose.Misc)
                {
                    continue;
                }

                if (PlantUtility.GrowthSeasonNow(c, map, plant.def))
                {
                    found++;
                }
            }

            return found;
        }

        public override string CompInspectStringExtra()
        {
            string baseText = base.CompInspectStringExtra() ?? string.Empty;
            string? extra = null;
            // A hive has two spawners (honey, wax): only the first one explains a pause.
            if (parent.Spawned && parent.GetComp<CompHomesteaderSpawner>() == this)
            {
                if (Paused(refresh: false, out string? reasonKey) && reasonKey != null)
                {
                    extra = reasonKey.Translate();
                }
                else
                {
                    float speed = Speed();
                    if (speed > 1.05f)
                    {
                        extra = "Homesteader_SpawnerRain".Translate(speed.ToStringPercent());
                    }
                }
            }

            if (extra == null)
            {
                return baseText;
            }

            return baseText.Length == 0 ? extra : baseText + "\n" + extra;
        }
    }
}

using System.Collections.Generic;
using Verse;

namespace WaveSurvivalReforged
{
    public class SupplyDrop
    {
        public ThingDef thing;
        public float chance;
        public IntRange count = IntRange.One;
        // No drop while the colony already holds this many.
        public int maxStockpile = int.MaxValue;
    }

    public class WaveSurvivalConfigDef : Def
    {
        public float daysBeforeFirstWave = 5f;
        public float daysBetweenWaves = 2f;
        // Hours after landing that waves and the daily drop land.
        public int raidHourOffset = 6;

        public int specialWaveInterval = 4;
        public int specialWaveMinWave = 1;

        public float startingThreatPoints = 50f;
        public float threatPointIncreasePerWave = 50f;
        public float siegeBonusPoints = 150f;
        public float doubleRaidChance = 0.25f;

        public float traderChance = 0.2f;

        public float weaponDropChance = 0.2f;
        public FloatRange weaponBudget = new FloatRange(200f, 400f);
        public float weaponBudgetPerWave = 25f;

        public List<SupplyDrop> supplyDrops = new List<SupplyDrop>();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
                yield return error;
            if (daysBetweenWaves <= 0f)
                yield return "daysBetweenWaves must be above 0";
            if (specialWaveInterval < 1)
                yield return "specialWaveInterval must be at least 1";
            for (int i = 0; i < supplyDrops.Count; i++)
            {
                if (supplyDrops[i].thing == null)
                    yield return "supplyDrops[" + i + "] has no thing";
            }
        }
    }
}

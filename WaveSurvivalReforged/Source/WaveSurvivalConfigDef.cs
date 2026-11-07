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
        // Extra days before the wave after a special or boss wave.
        public float breatherDays = 0.5f;
        // A wave that cannot run (no home map, nobody able to attack) tries again this much later.
        public float postponeHours = 3f;

        public int specialWaveInterval = 4;
        public int specialWaveMinWave = 1;
        // 0 turns boss waves off.
        public int bossWaveInterval = 10;

        // Wave number to threat points before the difficulty's threat scale.
        public SimpleCurve pointsByWave;
        // 0: points follow the curve only. 1: points follow colony wealth like a vanilla storyteller.
        public float wealthBlend;
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
            if (daysBeforeFirstWave < 0f)
                yield return "daysBeforeFirstWave must be 0 or more";
            if (daysBetweenWaves <= 0f)
                yield return "daysBetweenWaves must be above 0";
            if (raidHourOffset < 0 || raidHourOffset > 23)
                yield return "raidHourOffset must be 0 to 23";
            if (breatherDays < 0f)
                yield return "breatherDays must be 0 or more";
            if (postponeHours <= 0f)
                yield return "postponeHours must be above 0";
            if (specialWaveInterval < 1)
                yield return "specialWaveInterval must be at least 1";
            if (specialWaveMinWave < 1)
                yield return "specialWaveMinWave must be at least 1";
            if (bossWaveInterval < 0)
                yield return "bossWaveInterval must be 0 (off) or more";
            if (pointsByWave == null || pointsByWave.PointsCount == 0)
                yield return "pointsByWave needs at least one point";
            if (wealthBlend < 0f || wealthBlend > 1f)
                yield return "wealthBlend must be 0 to 1";
            foreach (string error in ChanceErrors("doubleRaidChance", doubleRaidChance))
                yield return error;
            foreach (string error in ChanceErrors("traderChance", traderChance))
                yield return error;
            foreach (string error in ChanceErrors("weaponDropChance", weaponDropChance))
                yield return error;
            if (weaponBudget.min < 0f || weaponBudget.max < weaponBudget.min)
                yield return "weaponBudget must be a range of 0 or more";
            for (int i = 0; i < supplyDrops.Count; i++)
            {
                SupplyDrop drop = supplyDrops[i];
                if (drop.thing == null)
                    yield return "supplyDrops[" + i + "] has no thing";
                foreach (string error in ChanceErrors("supplyDrops[" + i + "].chance", drop.chance))
                    yield return error;
                if (drop.count.min < 1 || drop.count.max < drop.count.min)
                    yield return "supplyDrops[" + i + "].count must be a range of 1 or more";
                if (drop.maxStockpile < 0)
                    yield return "supplyDrops[" + i + "].maxStockpile must be 0 or more";
            }
        }

        private static IEnumerable<string> ChanceErrors(string field, float value)
        {
            if (value < 0f || value > 1f)
                yield return field + " must be 0 to 1";
        }
    }
}

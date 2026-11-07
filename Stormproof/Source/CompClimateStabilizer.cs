using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_ClimateStabilizer : CompProperties_HazardDefender
    {
        public CompProperties_ClimateStabilizer()
        {
            compClass = typeof(CompClimateStabilizer);
            activePowerConsumption = 1800f;
        }
    }

    public class CompClimateStabilizer : CompHazardDefender
    {
        // Optional DLC defs are null when the DLC is absent; AnyConditionActive skips nulls.
        // Built on first use, after DefOfs are bound.
        private static GameConditionDef[] hazards;

        protected override GameConditionDef[] Hazards => hazards ?? (hazards = new[]
            {
                GameConditionDefOf.HeatWave,
                GameConditionDefOf.ColdSnap,
                GameConditionDefOf.VolcanicWinter,
                StormproofDefOf.Stormproof_HeatDome,
                StormproofDefOf.Stormproof_PolarFront,
                StormproofDefOf.DeepFreeze
            });

        protected override bool AllowedInSettings => StormproofMod.Settings.allowClimateStabilizer;

        protected override string StandbyKey => "Stormproof_ClimateStabilizer_Standby";

        protected override string ProtectingKey => "Stormproof_ClimateStabilizer_Protecting";

        protected override string DownKey => "Stormproof_ClimateStabilizer_Down";

        protected override void Register() => StormproofRegistry.ClimateStabilizers.Add(this);

        protected override void Unregister(Map map) => StormproofRegistry.ClimateStabilizers.Remove(this, map);
    }
}

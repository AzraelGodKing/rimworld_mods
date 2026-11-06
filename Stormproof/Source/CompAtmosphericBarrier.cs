using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_AtmosphericBarrier : CompProperties_HazardDefender
    {
        public CompProperties_AtmosphericBarrier()
        {
            compClass = typeof(CompAtmosphericBarrier);
            activePowerConsumption = 2000f;
        }
    }

    public class CompAtmosphericBarrier : CompHazardDefender
    {
        // Optional DLC defs are null when the DLC is absent; AnyConditionActive skips nulls.
        // Built on first use, after DefOfs are bound.
        private static GameConditionDef[] hazards;

        protected override GameConditionDef[] Hazards => hazards ?? (hazards = new[]
            {
                GameConditionDefOf.ToxicFallout,
                StormproofDefOf.Stormproof_ToxicSurge,
                StormproofDefOf.NoxiousHaze,
                StormproofDefOf.VolcanicAsh
            });

        protected override bool AllowedInSettings => StormproofMod.Settings.allowAtmosphericBarrier;

        protected override string StandbyKey => "Stormproof_AtmosphericBarrier_Standby";

        protected override string ProtectingKey => "Stormproof_AtmosphericBarrier_Protecting";

        protected override string DownKey => "Stormproof_AtmosphericBarrier_Down";

        protected override void Register() => StormproofRegistry.AtmosphericBarriers.Add(this);

        protected override void Unregister(Map map) => StormproofRegistry.AtmosphericBarriers.Remove(this, map);
    }
}

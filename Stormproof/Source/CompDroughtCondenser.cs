using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_DroughtCondenser : CompProperties_HazardDefender
    {
        public CompProperties_DroughtCondenser()
        {
            compClass = typeof(CompDroughtCondenser);
            activePowerConsumption = 1200f;
        }
    }

    public class CompDroughtCondenser : CompHazardDefender
    {
        // Optional DLC defs are null when the DLC is absent; AnyConditionActive skips nulls.
        // Built on first use, after DefOfs are bound.
        private static GameConditionDef[] hazards;

        protected override GameConditionDef[] Hazards => hazards ?? (hazards = new[]
            {
                StormproofDefOf.Drought,
                StormproofDefOf.DroughtInitial
            });

        protected override bool AllowedInSettings => StormproofMod.Settings.allowDroughtCondenser;

        protected override string StandbyKey => "Stormproof_DroughtCondenser_Standby";

        protected override string ProtectingKey => "Stormproof_DroughtCondenser_Protecting";

        protected override string DownKey => "Stormproof_DroughtCondenser_Down";

        protected override void Register() => StormproofRegistry.DroughtCondensers.Add(this);

        protected override void Unregister(Map map) => StormproofRegistry.DroughtCondensers.Remove(this, map);
    }
}

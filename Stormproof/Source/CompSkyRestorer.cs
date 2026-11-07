using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_SkyRestorer : CompProperties_HazardDefender
    {
        public CompProperties_SkyRestorer()
        {
            compClass = typeof(CompSkyRestorer);
            activePowerConsumption = 1500f;
        }
    }

    public class CompSkyRestorer : CompHazardDefender
    {
        // Optional DLC defs are null when the DLC is absent; AnyConditionActive skips nulls.
        // Built on first use, after DefOfs are bound.
        private static GameConditionDef[] hazards;

        protected override GameConditionDef[] Hazards => hazards ?? (hazards = new[]
            {
                GameConditionDefOf.Eclipse,
                GameConditionDefOf.VolcanicWinter,
                StormproofDefOf.VolcanicAsh,
                StormproofDefOf.DarkenedSkies
            });

        protected override bool AllowedInSettings => StormproofMod.Settings.allowSkyRestorer;

        protected override string StandbyKey => "Stormproof_SkyRestorer_Standby";

        protected override string ProtectingKey => "Stormproof_SkyRestorer_Protecting";

        protected override string DownKey => "Stormproof_SkyRestorer_Down";

        protected override void Register() => StormproofRegistry.SkyRestorers.Add(this);

        protected override void Unregister(Map map) => StormproofRegistry.SkyRestorers.Remove(this, map);
    }
}

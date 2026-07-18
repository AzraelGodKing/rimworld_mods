using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_EmpDampener : CompProperties
    {
        public float protectRadius = 14.9f;

        public CompProperties_EmpDampener()
        {
            compClass = typeof(CompEmpDampener);
        }
    }

    public class CompEmpDampener : ThingComp
    {
        private CompPowerTrader powerComp;

        public CompProperties_EmpDampener Props => (CompProperties_EmpDampener)props;

        public bool Active =>
            parent.Spawned &&
            !parent.Destroyed &&
            powerComp != null &&
            powerComp.PowerOn;

        public bool Covers(Thing t) =>
            Active &&
            t.Map == parent.Map &&
            t.Position.DistanceTo(parent.Position) <= Props.protectRadius;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            StormproofRegistry.Dampeners.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.Dampeners.Remove(this);
        }

        public override string CompInspectStringExtra()
        {
            return Active
                ? "Stormproof_EmpDampener_Active".Translate()
                : "Stormproof_EmpDampener_Inactive".Translate();
        }
    }
}

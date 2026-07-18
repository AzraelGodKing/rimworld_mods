using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_SurgeProtector : CompProperties
    {
        public int cooldownTicks = 60000; // one in-game day
        public float selfDamagePerAbsorb = 30f;

        public CompProperties_SurgeProtector()
        {
            compClass = typeof(CompSurgeProtector);
        }
    }

    public class CompSurgeProtector : ThingComp
    {
        private CompPowerTrader powerComp;
        private int lastAbsorbTick = -999999;

        public CompProperties_SurgeProtector Props => (CompProperties_SurgeProtector)props;

        public bool ReadyToAbsorb =>
            parent.Spawned &&
            !parent.Destroyed &&
            powerComp != null &&
            powerComp.PowerOn &&
            Find.TickManager.TicksGame >= lastAbsorbTick + Props.cooldownTicks;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            StormproofRegistry.SurgeProtectors.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.SurgeProtectors.Remove(this);
        }

        public void Absorb()
        {
            lastAbsorbTick = Find.TickManager.TicksGame;
            FleckMaker.ThrowMicroSparks(parent.DrawPos, parent.Map);
            FleckMaker.ThrowSmoke(parent.DrawPos, parent.Map, 1.2f);
            parent.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Props.selfDamagePerAbsorb));
            Messages.Message(
                "Stormproof_SurgeProtector_Absorbed".Translate(parent.LabelShort),
                parent, MessageTypeDefOf.PositiveEvent);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastAbsorbTick, "stormproof_lastAbsorbTick", -999999);
        }

        public override string CompInspectStringExtra()
        {
            if (ReadyToAbsorb)
            {
                return "Stormproof_SurgeProtector_Ready".Translate();
            }
            int remaining = lastAbsorbTick + Props.cooldownTicks - Find.TickManager.TicksGame;
            if (remaining > 0)
            {
                return "Stormproof_RechargingReadyIn".Translate(remaining.ToStringTicksToPeriod());
            }
            return "Stormproof_OfflineNeedsPower".Translate();
        }
    }
}

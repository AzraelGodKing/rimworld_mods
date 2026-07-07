using System.Linq;
using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_StaticPylon : CompProperties
    {
        public float dischargeRadius = 6.9f;
        public int dischargeIntervalTicks = 180; // every 3 seconds
        public int stunTicks = 120;              // 2 second stun
        public float energyPerShock = 50f;       // Wd drained from capacitors per pawn zapped
        public float damagePerShock = 6f;        // burn damage dealt per zap

        public CompProperties_StaticPylon()
        {
            compClass = typeof(CompStaticPylon);
        }
    }

    // Runs exclusively on bottled lightning: every shock drains stored energy
    // from a storm capacitor bank on the same power net. No capacitor charge,
    // no zap - the grid itself can't feed it, so keep your spires fed.
    public class CompStaticPylon : ThingComp
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;

        public CompProperties_StaticPylon Props => (CompProperties_StaticPylon)props;

        public bool Active =>
            parent.Spawned &&
            !parent.Destroyed &&
            (flickComp == null || flickComp.SwitchIsOn) &&
            powerComp != null &&
            powerComp.PowerOn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            flickComp = parent.GetComp<CompFlickable>();
        }

        private float CapacitorCharge =>
            StormproofRegistry
                .On(StormproofRegistry.Capacitors, parent.Map)
                .Where(c => c.Net == powerComp.PowerNet)
                .Sum(c => c.StoredEnergy);

        // Drains up to `amount` of stored strike energy from capacitor banks on
        // our net. Returns true if the full amount was available and taken.
        private bool TryDrainCapacitors(float amount)
        {
            foreach (CompStormCapacitor capacitor in StormproofRegistry
                         .On(StormproofRegistry.Capacitors, parent.Map)
                         .Where(c => c.Net == powerComp.PowerNet)
                         .OrderByDescending(c => c.StoredEnergy))
            {
                if (amount <= 0f)
                {
                    break;
                }
                amount -= capacitor.DrainEnergy(amount);
            }
            return amount <= 0f;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Props.dischargeIntervalTicks) || !Active)
            {
                return;
            }
            Map map = parent.Map;
            bool discharged = false;
            // Snapshot: dealing lethal damage can despawn a pawn and mutate the
            // spawned-pawns list mid-iteration.
            var targets = map.mapPawns.AllPawnsSpawned
                .Where(p => p.HostileTo(Faction.OfPlayer) &&
                            !p.Dead &&
                            p.Position.DistanceTo(parent.Position) <= Props.dischargeRadius)
                .ToList();
            foreach (Pawn pawn in targets)
            {
                // Out of bottled lightning: stop mid-volley.
                if (!TryDrainCapacitors(Props.energyPerShock))
                {
                    break;
                }
                pawn.stances?.stunner?.StunFor(Props.stunTicks, parent, addBattleLog: false);
                if (Props.damagePerShock > 0f)
                {
                    pawn.TakeDamage(new DamageInfo(
                        DamageDefOf.Burn, Props.damagePerShock, 0.5f, -1f, parent));
                }
                FleckMaker.ThrowMicroSparks(pawn.DrawPos, map);
                discharged = true;
            }
            if (discharged)
            {
                FleckMaker.ThrowLightningGlow(parent.DrawPos, map, 1.2f);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!Active)
            {
                return "Offline: needs power.";
            }
            float charge = CapacitorCharge;
            return charge >= Props.energyPerShock
                ? "Armed: " + charge.ToString("F0") + " Wd of stored lightning available (" +
                  Props.energyPerShock.ToString("F0") + " Wd per shock)."
                : "Standby: no stored lightning. Needs a charged storm capacitor bank on this power net.";
        }
    }
}

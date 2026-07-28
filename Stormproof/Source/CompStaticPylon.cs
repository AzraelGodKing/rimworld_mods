using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

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

        // Drains `amount` of stored strike energy from capacitor banks on our net.
        // Returns true only if the full amount was available; otherwise drains nothing.
        private bool TryDrainCapacitors(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            var banks = StormproofRegistry
                .On(StormproofRegistry.Capacitors, parent.Map)
                .Where(c => c.Net == powerComp.PowerNet)
                .OrderByDescending(c => c.StoredEnergy)
                .ToList();

            float available = 0f;
            foreach (CompStormCapacitor capacitor in banks)
            {
                available += capacitor.StoredEnergy;
                if (available >= amount)
                {
                    break;
                }
            }
            if (available < amount)
            {
                return false;
            }

            float remaining = amount;
            foreach (CompStormCapacitor capacitor in banks)
            {
                if (remaining <= 0f)
                {
                    break;
                }
                remaining -= capacitor.DrainEnergy(remaining);
            }
            return remaining <= 0.01f;
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
                            !p.Downed &&
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
                PlayStrikeEffects(pawn, map);
                discharged = true;
            }
            if (discharged)
            {
                FleckMaker.ThrowLightningGlow(parent.DrawPos, map, 1.2f);
            }
        }

        // Visible arc from the pylon to the victim, sparks and a flash on the
        // pawn, plus the EMP-disabled effecter (crackling electricity overlay)
        // maintained on them for the stun duration.
        private void PlayStrikeEffects(Pawn pawn, Map map)
        {
            FleckMaker.ConnectingLine(
                parent.DrawPos, pawn.DrawPos, FleckDefOf.LineEMP, map, 1.4f);
            FleckMaker.ThrowMicroSparks(pawn.DrawPos, map);
            FleckMaker.ThrowLightningGlow(pawn.DrawPos, map, 0.6f);
            Effecter effecter = EffecterDefOf.DisabledByEMP.Spawn();
            map.effecterMaintainer.AddEffecterToMaintain(effecter, pawn, Props.stunTicks);
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(pawn.Position, map));
        }

        public override string CompInspectStringExtra()
        {
            if (!Active)
            {
                return "Stormproof_OfflineNeedsPower".Translate();
            }
            float charge = CapacitorCharge;
            return charge >= Props.energyPerShock
                ? "Stormproof_StaticPylon_Armed".Translate(charge.ToString("F0"), Props.energyPerShock.ToString("F0"))
                : "Stormproof_StaticPylon_Standby".Translate();
        }
    }
}

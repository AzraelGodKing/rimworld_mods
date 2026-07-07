using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_StormCapacitor : CompProperties
    {
        public float storedEnergyMax = 3000f;   // Wd
        public float maxDischargeWatts = 2000f;

        public CompProperties_StormCapacitor()
        {
            compClass = typeof(CompStormCapacitor);
        }
    }

    // A lightning-only battery. It is NOT a CompPowerBattery, which is the
    // whole point: the grid can't charge it (only spire-caught strikes can),
    // it never self-discharges, and "Zzzt!" surges can't see it because the
    // short circuit incident only drains real batteries.
    //
    // Discharge works by acting as a generator that covers exactly the grid's
    // current deficit, so it kicks in when production drops (eclipse, flare,
    // night) and stays quiet while the grid is self-sufficient.
    public class CompStormCapacitor : ThingComp
    {
        private const int UpdateIntervalTicks = 30;

        private CompPowerTrader powerComp;
        private float storedEnergy;

        public CompProperties_StormCapacitor Props => (CompProperties_StormCapacitor)props;

        public PowerNet Net => powerComp?.PowerNet;

        public float StoredEnergy => storedEnergy;

        public float AmountCanAccept =>
            parent.Spawned && !parent.Destroyed
                ? Props.storedEnergyMax - storedEnergy
                : 0f;

        public void AddEnergy(float amount)
        {
            storedEnergy = UnityEngine.Mathf.Min(storedEnergy + amount, Props.storedEnergyMax);
        }

        // Removes up to `amount` from storage and returns how much was taken.
        public float DrainEnergy(float amount)
        {
            float taken = UnityEngine.Mathf.Min(amount, storedEnergy);
            storedEnergy -= taken;
            return taken;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            StormproofRegistry.Capacitors.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.Capacitors.Remove(this);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(UpdateIntervalTicks) || powerComp == null)
            {
                return;
            }
            // Drain what we produced since the last update.
            if (powerComp.PowerOutput > 0f)
            {
                storedEnergy -= powerComp.PowerOutput * CompPower.WattsToWattDaysPerTick * UpdateIntervalTicks;
                if (storedEnergy < 0f)
                {
                    storedEnergy = 0f;
                }
            }
            PowerNet net = powerComp.PowerNet;
            if (net == null || storedEnergy <= 0f)
            {
                powerComp.PowerOutput = 0f;
                return;
            }
            // Grid gain rate excluding our own contribution: only cover the
            // deficit, never feed the grid's batteries from the capacitor.
            float gainWatts = net.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick - powerComp.PowerOutput;
            float wanted = gainWatts < 0f ? UnityEngine.Mathf.Min(-gainWatts, Props.maxDischargeWatts) : 0f;
            powerComp.PowerOutput = wanted;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref storedEnergy, "stormproof_capacitorEnergy", 0f);
        }

        public override string CompInspectStringExtra()
        {
            string s = "Stored: " + storedEnergy.ToString("F0") + " / " +
                       Props.storedEnergyMax.ToString("F0") + " Wd (lightning only, no self-discharge)";
            if (powerComp != null && powerComp.PowerOutput > 0f)
            {
                s += "\nDischarging: " + powerComp.PowerOutput.ToString("F0") + " W to cover grid deficit.";
            }
            return s;
        }
    }
}

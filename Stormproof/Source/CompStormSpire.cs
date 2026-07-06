using System.Linq;
using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_StormSpire : CompProperties
    {
        public float attractRadius = 30f;
        public float energyPerStrike = 1500f; // Wd fed into connected batteries
        public float zzztChancePerStrike = 0.25f;

        public CompProperties_StormSpire()
        {
            compClass = typeof(CompStormSpire);
        }
    }

    public class CompStormSpire : ThingComp
    {
        private CompPowerTrader powerComp;

        public CompProperties_StormSpire Props => (CompProperties_StormSpire)props;

        public bool Attracting => parent.Spawned && !parent.Destroyed;

        public bool GridConnected => powerComp != null && powerComp.PowerNet != null;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            StormproofRegistry.Spires.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.Spires.Remove(this);
        }

        // Called by the lightning patch after a redirected bolt lands on us.
        public void Notify_Struck()
        {
            if (!GridConnected)
            {
                return; // grounded rod: safe, but no harvest
            }
            float remaining = Props.energyPerStrike;
            foreach (CompPowerBattery battery in powerComp.PowerNet.batteryComps
                         .OrderByDescending(b => b.AmountCanAccept))
            {
                if (remaining <= 0f)
                {
                    break;
                }
                float accept = System.Math.Min(battery.AmountCanAccept, remaining);
                if (accept > 0f)
                {
                    battery.AddEnergy(accept);
                    remaining -= accept;
                }
            }
            float harvested = Props.energyPerStrike - remaining;
            if (harvested > 0f)
            {
                Messages.Message(
                    parent.LabelShort + " caught a lightning strike: +" +
                    harvested.ToString("F0") + " Wd stored.",
                    parent, MessageTypeDefOf.PositiveEvent);
            }
            if (Rand.Chance(Props.zzztChancePerStrike))
            {
                IncidentDef zzzt = DefDatabase<IncidentDef>.GetNamedSilentFail("ShortCircuit");
                if (zzzt != null)
                {
                    IncidentParms parms = new IncidentParms { target = parent.Map };
                    zzzt.Worker.TryExecute(parms);
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!GridConnected)
            {
                return "Grounded: attracts lightning safely. Connect to a power grid to harvest strikes.";
            }
            return "Grid-connected: strikes charge batteries (+" + Props.energyPerStrike.ToString("F0") +
                   " Wd), " + (Props.zzztChancePerStrike * 100f).ToString("F0") + "% surge risk per strike.";
        }
    }
}

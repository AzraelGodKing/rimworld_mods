using System.Linq;
using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_StormSpire : CompProperties
    {
        public float attractRadius = 30f;
        public float energyPerStrike = 1500f; // Wd fed into connected batteries
        public float zzztChancePerStrike = 0.05f;

        public CompProperties_StormSpire()
        {
            compClass = typeof(CompStormSpire);
        }
    }

    public class CompStormSpire : ThingComp
    {
        private CompPowerTrader powerComp;

        private int suppressFiresUntilTick = -1;

        // Vanilla lightning explosions have a ~1.9 cell flame radius; cover it
        // with a little margin.
        private const float FireSuppressRadius = 3f;

        // The strike's flame explosion expands over several ticks after
        // DoStrike returns, so fires appear well after the strike itself.
        // Keep sweeping long enough for the wave (and any instant spread)
        // to finish. 300 ticks = 5 in-game seconds.
        private const int FireSuppressDurationTicks = 300;

        public CompProperties_StormSpire Props => (CompProperties_StormSpire)props;

        public bool Attracting => parent.Spawned && !parent.Destroyed;

        public bool GridConnected => powerComp != null && powerComp.PowerNet != null;

        public bool SurgeRiskEliminated =>
            StormproofDefOf.Stormproof_PerfectGrounding != null &&
            StormproofDefOf.Stormproof_PerfectGrounding.IsFinished;

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
        // The strike's flame explosion is not instantaneous - it expands over
        // the following ticks - so a single immediate sweep would run before
        // any fire has spawned. Instead we open a short suppression window and
        // repeatedly snuff out fires around the spire while it plays out.
        public void StartFireSuppression()
        {
            suppressFiresUntilTick = Find.TickManager.TicksGame + FireSuppressDurationTicks;
            ExtinguishNearbyFires();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (suppressFiresUntilTick < 0)
            {
                return;
            }
            if (Find.TickManager.TicksGame > suppressFiresUntilTick)
            {
                suppressFiresUntilTick = -1;
                return;
            }
            if (parent.IsHashIntervalTick(10))
            {
                ExtinguishNearbyFires();
            }
        }

        private void ExtinguishNearbyFires()
        {
            Map map = parent.Map;
            if (map == null)
            {
                return;
            }
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(parent.Position, FireSuppressRadius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                var things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    if (things[i] is Fire fire && !fire.Destroyed)
                    {
                        fire.Destroy();
                    }
                }
            }
        }

        public void Notify_Struck()
        {
            if (!GridConnected)
            {
                return; // grounded rod: safe, but no harvest
            }
            float remaining = Props.energyPerStrike;
            // Storm capacitors get first pick: they exist to store strike energy.
            foreach (CompStormCapacitor capacitor in StormproofRegistry
                         .On(StormproofRegistry.Capacitors, parent.Map)
                         .Where(c => c.Net == powerComp.PowerNet)
                         .OrderByDescending(c => c.AmountCanAccept))
            {
                if (remaining <= 0f)
                {
                    break;
                }
                float acceptCap = System.Math.Min(capacitor.AmountCanAccept, remaining);
                if (acceptCap > 0f)
                {
                    capacitor.AddEnergy(acceptCap);
                    remaining -= acceptCap;
                }
            }
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
                    "Stormproof_StormSpire_StrikeHarvested".Translate(parent.LabelShort, harvested.ToString("F0")),
                    parent, MessageTypeDefOf.PositiveEvent);
            }
            if (!SurgeRiskEliminated && Rand.Chance(Props.zzztChancePerStrike))
            {
                IncidentDef zzzt = DefDatabase<IncidentDef>.GetNamedSilentFail("ShortCircuit");
                if (zzzt != null)
                {
                    IncidentParms parms = new IncidentParms { target = parent.Map };
                    zzzt.Worker.TryExecute(parms);
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref suppressFiresUntilTick, "stormproof_suppressFiresUntilTick", -1);
        }

        public override string CompInspectStringExtra()
        {
            if (!GridConnected)
            {
                return "Stormproof_StormSpire_Grounded".Translate();
            }
            string surge = SurgeRiskEliminated
                ? "Stormproof_StormSpire_SurgeRiskNone".Translate()
                : "Stormproof_StormSpire_SurgeRiskPercent".Translate((Props.zzztChancePerStrike * 100f).ToString("F0"));
            return "Stormproof_StormSpire_GridConnected".Translate(
                Props.energyPerStrike.ToString("F0"), surge);
        }
    }
}

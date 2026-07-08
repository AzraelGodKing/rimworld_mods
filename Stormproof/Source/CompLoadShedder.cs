using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    [StaticConstructorOnStartup]
    internal static class LoadShedderTex
    {
        public static readonly Texture2D CutoffLower = ContentFinder<Texture2D>.Get("UI/Commands/TempLower");
        public static readonly Texture2D CutoffRaise = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise");
    }

    // The breaker building itself only reports whether it currently transmits;
    // all the decision-making lives in CompLoadShedder.
    public class Building_LoadShedder : Building
    {
        private CompLoadShedder shedderComp;

        public override bool TransmitsPowerNow =>
            shedderComp == null || shedderComp.BreakerClosed;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            shedderComp = GetComp<CompLoadShedder>();
            base.SpawnSetup(map, respawningAfterLoad);
        }
    }

    public class CompProperties_LoadShedder : CompProperties
    {
        public int checkIntervalTicks = 60;
        public float defaultCutoffFraction = 0.20f;
        // Reconnect this far above the cutoff so the breaker doesn't chatter.
        public float reconnectMargin = 0.20f;

        public CompProperties_LoadShedder()
        {
            compClass = typeof(CompLoadShedder);
        }
    }

    // An automatic breaker: wire it between the main grid and a low-priority
    // sub-grid. While the supply side's batteries hold charge it transmits like
    // a conduit; when they fall below the cutoff it trips open, shedding the
    // sub-grid so life support outlasts the lamps. Reconnects on its own once
    // the batteries recover.
    public class CompLoadShedder : ThingComp
    {
        private CompPower transmitterComp;
        private bool breakerClosed = true;
        private float cutoffFraction;

        public CompProperties_LoadShedder Props => (CompProperties_LoadShedder)props;

        public bool BreakerClosed => breakerClosed;

        private float ReconnectFraction =>
            Mathf.Min(cutoffFraction + Props.reconnectMargin, 0.95f);

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            cutoffFraction = Props.defaultCutoffFraction;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            transmitterComp = parent.GetComp<CompPower>();
        }

        // The grid we watch. Closed: our own net (both sides are one net).
        // Open: we belong to no net, so of the nets touching our cardinal
        // neighbors, the one with the most battery capacity is the supply.
        private PowerNet SupplyNet()
        {
            if (breakerClosed && transmitterComp.PowerNet != null)
            {
                return transmitterComp.PowerNet;
            }
            PowerNet best = null;
            float bestCapacity = -1f;
            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(parent))
            {
                if (!cell.InBounds(parent.Map))
                {
                    continue;
                }
                PowerNet net = parent.Map.powerNetGrid.TransmittedPowerNetAt(cell);
                if (net == null || net == best)
                {
                    continue;
                }
                float capacity = net.batteryComps.Sum(b => b.Props.storedEnergyMax);
                if (capacity > bestCapacity)
                {
                    bestCapacity = capacity;
                    best = net;
                }
            }
            return best;
        }

        private static float StoredFraction(PowerNet net)
        {
            float capacity = net.batteryComps.Sum(b => b.Props.storedEnergyMax);
            return capacity <= 0f
                ? -1f
                : net.batteryComps.Sum(b => b.StoredEnergy) / capacity;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Props.checkIntervalTicks) || !parent.Spawned)
            {
                return;
            }
            PowerNet supply = SupplyNet();
            if (supply == null)
            {
                SetBreaker(true, quiet: true);
                return;
            }
            float fraction = StoredFraction(supply);
            if (fraction < 0f)
            {
                // No batteries anywhere: nothing to protect, act like a conduit.
                SetBreaker(true, quiet: true);
            }
            else if (breakerClosed && fraction < cutoffFraction)
            {
                SetBreaker(false);
            }
            else if (!breakerClosed && fraction >= ReconnectFraction)
            {
                SetBreaker(true);
            }
        }

        private void SetBreaker(bool closed, bool quiet = false)
        {
            if (breakerClosed == closed)
            {
                return;
            }
            breakerClosed = closed;
            if (parent.Spawned)
            {
                parent.Map.powerNetManager.Notfiy_TransmitterTransmitsPowerNowChanged(transmitterComp);
                FleckMaker.ThrowMicroSparks(parent.DrawPos, parent.Map);
                if (!quiet)
                {
                    Messages.Message(
                        closed
                            ? parent.LabelShort + ": batteries recovered, sub-grid reconnected."
                            : parent.LabelShort + ": batteries below " +
                              cutoffFraction.ToStringPercent() + ", sub-grid shed.",
                        parent,
                        closed ? MessageTypeDefOf.SilentInput : MessageTypeDefOf.CautionInput);
                }
            }
        }

        public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            yield return new Command_Action
            {
                defaultLabel = "Cutoff -5%",
                defaultDesc = "Trip the breaker when supply batteries fall below a lower charge level.",
                icon = LoadShedderTex.CutoffLower,
                action = () => cutoffFraction = Mathf.Max(0.05f, cutoffFraction - 0.05f),
            };
            yield return new Command_Action
            {
                defaultLabel = "Cutoff +5%",
                defaultDesc = "Trip the breaker when supply batteries fall below a higher charge level.",
                icon = LoadShedderTex.CutoffRaise,
                action = () => cutoffFraction = Mathf.Min(0.45f, cutoffFraction + 0.05f),
            };
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref breakerClosed, "stormproof_breakerClosed", true);
            Scribe_Values.Look(ref cutoffFraction, "stormproof_cutoffFraction", 0.20f);
        }

        public override string CompInspectStringExtra()
        {
            string state = breakerClosed
                ? "Breaker closed: transmitting."
                : "Breaker OPEN: sub-grid shed.";
            PowerNet supply = parent.Spawned ? SupplyNet() : null;
            if (supply != null)
            {
                float fraction = StoredFraction(supply);
                state += fraction < 0f
                    ? "\nNo batteries on the supply grid."
                    : "\nSupply batteries: " + fraction.ToStringPercent() +
                      " (trips below " + cutoffFraction.ToStringPercent() +
                      ", reconnects at " + ReconnectFraction.ToStringPercent() + ").";
            }
            return state;
        }
    }
}

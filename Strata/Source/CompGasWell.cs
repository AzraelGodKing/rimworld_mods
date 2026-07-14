using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    [DefOf]
    public static class StrataThingDefOf
    {
        public static ThingDef Strata_DeepGasVent;

        public static ThingDef Strata_GasWell;

        public static ThingDef Strata_DeepGasCanister;

        static StrataThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataThingDefOf));
        }
    }

    // The natural fissure at the heart of a gas chamber. An ordinary emitter
    // on the deep-gas channel - it seeps forever, so an uncapped vent in your
    // base is a permanent hazard and a capped one is a permanent income.
    public class CompDeepGasVent : CompExhaust
    {
        public bool Capped
        {
            get
            {
                List<Thing> things = parent.Position.GetThingList(parent.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] != parent && things[i] is ThingWithComps twc
                        && twc.GetComp<CompGasWell>() != null)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override bool Active => parent.Spawned && !Capped;

        public override string CompInspectStringExtra()
        {
            return Capped
                ? "Capped by a gas well."
                : "Seeping deep gas into the room. Cap it with a gas well, vent the room, or wall it off.";
        }
    }

    public class CompProperties_GasWell : CompProperties
    {
        // Full-uptime ticks to fill one canister (20000 = 3 per day).
        public int ticksPerCanister = 20000;

        // Pipe-network units delivered instead of one canister when a gas
        // pipe framework (Vanilla Helixien Gas Expanded) is connected.
        public float netUnitsPerCanister = 15f;

        public CompProperties_GasWell()
        {
            compClass = typeof(CompGasWell);
        }
    }

    // Built over a deep gas vent: caps the seep and pumps the gas into
    // canisters - or straight into a Helixien gas pipe network when that mod
    // is loaded and a pipe is connected.
    public class CompGasWell : ThingComp
    {
        private float progress;

        private CompPowerTrader powerCached;

        public CompProperties_GasWell Props => (CompProperties_GasWell)props;

        private CompPowerTrader Power => powerCached ??= parent.GetComp<CompPowerTrader>();

        public bool OnVent
        {
            get
            {
                List<Thing> things = parent.Position.GetThingList(parent.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i].def == StrataThingDefOf.Strata_DeepGasVent)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned || (Power != null && !Power.PowerOn) || !OnVent)
            {
                return;
            }
            progress += 1f;
            if (progress < Props.ticksPerCanister)
            {
                return;
            }
            progress = 0f;
            if (GasNetAdapter.TryPush(parent, Props.netUnitsPerCanister))
            {
                return;
            }
            Thing canister = ThingMaker.MakeThing(StrataThingDefOf.Strata_DeepGasCanister);
            GenPlace.TryPlaceThing(canister, parent.Position, parent.Map, ThingPlaceMode.Near);
        }

        public override string CompInspectStringExtra()
        {
            if (!OnVent)
            {
                return "No deep gas vent below - producing nothing.";
            }
            string line = $"Extraction: {Mathf.FloorToInt(progress / Props.ticksPerCanister * 100f)}%";
            if (GasNetAdapter.IsConnected(parent))
            {
                line += "\nFeeding the gas pipe network.";
            }
            return line;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref progress, "gasWellProgress");
        }
    }

    // Gas wells only make sense over a deep vent (mirrors the vanilla
    // geothermal-on-geyser rule).
    public class PlaceWorker_OnDeepGasVent : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot,
            Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            List<Thing> things = loc.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def == StrataThingDefOf.Strata_DeepGasVent)
                {
                    return true;
                }
            }
            return "Must be placed on a deep gas vent.";
        }

        public override bool ForceAllowPlaceOver(BuildableDef otherDef)
        {
            return otherDef == StrataThingDefOf.Strata_DeepGasVent;
        }
    }
}

using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_GravshipOxygenTank : CompProperties
    {
        public float maxStored = 40f;
        /// <summary>O₂ fraction added to the room atmosphere each rare tick while releasing.</summary>
        public float releasePerRareTick = 0.012f;
        /// <summary>Storage drawn per rare tick of release.</summary>
        public float consumePerRareTick = 0.08f;
        /// <summary>Passive refill while an open shaft umbilical feeds host air (landed, non-vacuum).</summary>
        public float umbilicalRefillPerRareTick = 0.05f;

        public CompProperties_GravshipOxygenTank()
        {
            compClass = typeof(CompGravshipOxygenTank);
        }
    }

    /// <summary>
    /// Compressed O₂ store for gravship decks (VGE tank+pusher analog without PipeSystem).
    /// Powered release into the room; refill from chemfuel refuelable and/or open shaft umbilical.
    /// </summary>
    public class CompGravshipOxygenTank : ThingComp
    {
        public CompProperties_GravshipOxygenTank Props => (CompProperties_GravshipOxygenTank)props;

        private float stored;

        public float Stored => stored;
        public float MaxStored => Props.maxStored;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref stored, "strataO2Stored", 0f);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && stored <= 0f)
            {
                stored = Props.maxStored * 0.5f;
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (parent?.Map == null)
            {
                return;
            }

            TryUmbilicalRefill();
            TryChemfuelRefill();

            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            CompFlickable flick = parent.GetComp<CompFlickable>();
            if (power != null && !power.PowerOn)
            {
                return;
            }
            if (flick != null && !flick.SwitchIsOn)
            {
                return;
            }
            if (stored < Props.consumePerRareTick * 0.5f)
            {
                return;
            }

            Room room = parent.GetRoom();
            if (room == null || room.UsesOutdoorTemperature)
            {
                return;
            }

            AtmosphereMapComponent atmo = parent.Map.GetComponent<AtmosphereMapComponent>();
            StrataGasDef o2 = StrataGasDefOf.Strata_Oxygen;
            if (atmo == null || o2 == null || !StrataMod.Settings.NaturalGasesActive)
            {
                return;
            }

            atmo.AddGasToRoomPublic(room, o2, Props.releasePerRareTick, parent.Position);
            // Prefer breath-grid pump path on underground decks for even pressurization.
            if (StrataMapUtility.IsUnderground(parent.Map))
            {
                // EmitOxygenPump is internal — room cloud path above is enough with AddGasToRoomPublic.
            }
            stored = Mathf.Max(0f, stored - Props.consumePerRareTick);
        }

        private void TryChemfuelRefill()
        {
            CompRefuelable fuel = parent.GetComp<CompRefuelable>();
            if (fuel == null || !fuel.HasFuel || stored >= Props.maxStored - 0.01f)
            {
                return;
            }
            // Convert a trickle of fuel into stored O₂.
            float need = Props.maxStored - stored;
            float take = Mathf.Min(need * 0.15f, 0.2f);
            if (take <= 0f)
            {
                return;
            }
            fuel.ConsumeFuel(take);
            stored = Mathf.Min(Props.maxStored, stored + take * 2.5f);
        }

        private void TryUmbilicalRefill()
        {
            if (stored >= Props.maxStored - 0.01f)
            {
                return;
            }
            Map map = parent.Map;
            if (!StrataGravshipLifeSupport.IsLifeSupportDeck(map))
            {
                return;
            }
            Map host = StrataGravshipUtility.FindGravshipStackRoot(map);
            if (host == null || StrataGravshipLifeSupport.HostIsVacuum(host))
            {
                return;
            }
            // Any open (unsealed) gravship portal on this pocket → host air available.
            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (t is not IStrataGravshipPortal || t is not MapPortal portal)
                {
                    continue;
                }
                if (portal is Building_StairsDown down && down.Sealed)
                {
                    continue;
                }
                if (portal is Building_StairsUp up
                    && up.DownEntrance != null && up.DownEntrance.Sealed)
                {
                    continue;
                }
                stored = Mathf.Min(Props.maxStored, stored + Props.umbilicalRefillPerRareTick);
                return;
            }
        }

        public void AcceptFromReclaimer(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }
            stored = Mathf.Min(Props.maxStored, stored + amount);
        }

        public override string CompInspectStringExtra()
        {
            return "Strata_GravshipOxygenStored".Translate(
                stored.ToString("F1"), Props.maxStored.ToString("F0"));
        }
    }
}

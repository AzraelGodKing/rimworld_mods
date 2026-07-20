using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // One end of a power tie that runs through a shaft. It's an ordinary power
    // trader on its own level's grid; the controller end (top) drives both this
    // node and its partner each period to move energy across the shaft.
    //
    // Demand-driven: a grid asks for its running deficit plus a battery-
    // equalization trickle, and the other side gives its live surplus plus
    // whatever its batteries can discharge — no flat watt cap.
    public class CompPowerShaft : CompPowerTrader
    {
        // Watts moved per unit of stored-energy (Wd) imbalance between the grids.
        private const float TransferGain = 4f;

        private float BaseLoad
        {
            get
            {
                // Auto-spawned lower junctions match stairwell landings: zero
                // draw so a dark level can bootstrap from the level above.
                if (parent is Building_ShaftConduit conduit && conduit.IsAutoSpawned)
                {
                    return 0f;
                }
                if (parent.TryGetComp<CompShaftFluidJunctionLink>()?.IsAutoSpawned == true)
                {
                    return 0f;
                }
                return Props.PowerConsumption;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            // Stay switched on whenever wired so the tie can feed a dark level
            // and 2x2 stairwell landings actually join their local grid.
            if (PowerNet != null && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown())
            {
                PowerOn = true;
            }
        }

        // Shaft transmitters are tie points, not appliances. Vanilla still
        // toggles PowerOn during tight grids and draws the blinking NeedsPower
        // bolt even while cross-level transfer keeps working.
        public override void PostDraw()
        {
            if (ShouldSuppressNeedsPowerOverlay())
            {
                CompFlickable flick = parent.GetComp<CompFlickable>();
                if (flick != null && !flick.SwitchIsOn && !parent.IsBrokenDown())
                {
                    parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.PowerOff);
                }
                return;
            }
            base.PostDraw();
        }

        private bool ShouldSuppressNeedsPowerOverlay()
        {
            return Props.transmitsPower && PowerNet != null
                && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown();
        }

        // Called on the TOP node with the bottom node. Sets both nodes' output
        // so energy flows to whichever grid needs it, limited by each side's
        // available surplus and battery discharge (not a fixed watt ceiling).
        public void DriveTie(CompPowerShaft bottom)
        {
            PowerNet topNet = PowerNet;
            PowerNet botNet = bottom.PowerNet;
            if (topNet == null || botNet == null || topNet == botNet
                || !FlickUtility.WantsToBeOn(parent) || parent.IsBrokenDown()
                || !FlickUtility.WantsToBeOn(bottom.parent) || bottom.parent.IsBrokenDown())
            {
                Idle(bottom);
                return;
            }

            bottom.PowerOn = true;

            // Each grid's balance in watts with this tie's own flow removed, so
            // the previous transfer doesn't feed back into this one.
            float topBalance = topNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick
                - PowerOutput - BaseLoad;
            float botBalance = botNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick
                - bottom.PowerOutput - bottom.BaseLoad;

            float botWant = Mathf.Max(0f, -botBalance) + EqualizeWant(botNet, topNet);
            float topWant = Mathf.Max(0f, -topBalance) + EqualizeWant(topNet, botNet);

            float down = Mathf.Min(botWant, Supply(topNet, topBalance));
            float up = Mathf.Min(topWant, Supply(botNet, botBalance));
            float transfer = down - up; // + = down

            // Negative output = drawing from the local grid; positive = feeding
            // it. The two outputs always sum to the base loads: the tie moves
            // energy, it never creates it.
            PowerOutput = -BaseLoad - Mathf.Max(0f, transfer) + Mathf.Max(0f, -transfer);
            bottom.PowerOutput = -bottom.BaseLoad + Mathf.Max(0f, transfer) - Mathf.Max(0f, -transfer);
        }

        private void Idle(CompPowerShaft bottom)
        {
            PowerOutput = -BaseLoad;
            bottom.PowerOutput = -bottom.BaseLoad;
        }

        // Live production surplus, then battery discharge headroom if needed.
        private static float Supply(PowerNet net, float balance)
        {
            float spare = Mathf.Max(0f, balance);
            if (spare > 0f)
            {
                return spare;
            }
            if (net.CurrentStoredEnergy() <= 1f)
            {
                return 0f;
            }
            return BatteryDischargeWatts(net);
        }

        private static float BatteryDischargeWatts(PowerNet net)
        {
            // Scaled to stored charge; the demand-side Min() caps actual draw.
            return net.CurrentStoredEnergy() * TransferGain;
        }

        // Battery-equalization trickle: charge toward the other grid's stored
        // level while there is room. Stops on its own once the levels meet, so
        // the tie can't drain one side forever.
        private static float EqualizeWant(PowerNet mine, PowerNet other)
        {
            float myStored = mine.CurrentStoredEnergy();
            float otherStored = other.CurrentStoredEnergy();
            if (otherStored <= myStored || !AnyBatteryFreeSpace(mine))
            {
                return 0f;
            }
            return (otherStored - myStored) * TransferGain;
        }

        private static bool AnyBatteryFreeSpace(PowerNet net)
        {
            List<CompPowerBattery> batteries = net.batteryComps;
            for (int i = 0; i < batteries.Count; i++)
            {
                if (batteries[i].AmountCanAccept > 1f)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

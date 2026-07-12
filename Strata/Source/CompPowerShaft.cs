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
    // equalization trickle, and the other side gives only that much, up to the
    // wattage cap. Pushing a flat maximum instead would drain the source grid,
    // get the tie shed by vanilla brownout logic as the biggest consumer, and
    // lock it off until the grid could afford the full draw again - which reads
    // in game as "power never transfers".
    public class CompPowerShaft : CompPowerTrader
    {
        // Watts moved per unit of stored-energy (Wd) imbalance between the grids.
        private const float TransferGain = 4f;

        private float BaseLoad => Props.PowerConsumption;

        // Called on the TOP node with the bottom node. Sets both nodes' output
        // so energy flows to whichever grid needs it, capped at capWatts.
        public void DriveTie(CompPowerShaft bottom, float capWatts)
        {
            PowerNet topNet = PowerNet;
            PowerNet botNet = bottom.PowerNet;
            if (topNet == null || botNet == null || topNet == botNet
                || !FlickUtility.WantsToBeOn(parent) || parent.IsBrokenDown())
            {
                Idle(bottom);
                return;
            }
            // Both ends must be switched on by their nets for any transfer to
            // count in the power ledger. While either is off, present only the
            // tiny base load so its net can afford to switch it back on.
            if (!PowerOn || !bottom.PowerOn)
            {
                Idle(bottom);
                return;
            }

            // Each grid's balance in watts with this tie's own flow removed, so
            // the previous transfer doesn't feed back into this one.
            float topBalance = topNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick
                - PowerOutput - BaseLoad;
            float botBalance = botNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick
                - bottom.PowerOutput - bottom.BaseLoad;

            float botWant = Mathf.Max(0f, -botBalance) + EqualizeWant(botNet, topNet);
            float topWant = Mathf.Max(0f, -topBalance) + EqualizeWant(topNet, botNet);

            float down = Mathf.Min(capWatts, botWant, Supply(topNet, topBalance, capWatts));
            float up = Mathf.Min(capWatts, topWant, Supply(botNet, botBalance, capWatts));
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

        // What a grid can give: its running surplus, or the full cap once it
        // has stored energy to draw on.
        private static float Supply(PowerNet net, float balance, float capWatts)
        {
            return net.CurrentStoredEnergy() > 1f ? capWatts : Mathf.Max(0f, balance);
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

using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // One end of a power tie that runs through a shaft. It's an ordinary power
    // trader on its own level's grid; the controller end (top) drives both this
    // node and its partner each period to move energy across the shaft. Modelled
    // as a lossless tie that equalizes the two levels' stored battery energy up
    // to a wattage cap - so keep a battery on each level for it to pool across.
    public class CompPowerShaft : CompPowerTrader
    {
        // Watts moved per unit of stored-energy (Wd) imbalance between the grids.
        private const float TransferGain = 4f;

        private float BaseLoad => Props.PowerConsumption;

        // Called on the TOP node with the bottom node. Sets both nodes' output so
        // surplus flows to whichever grid is shorter, capped at capWatts.
        public void DriveTie(CompPowerShaft bottom, float capWatts)
        {
            PowerNet topNet = PowerNet;
            PowerNet botNet = bottom.PowerNet;
            if (topNet == null || botNet == null)
            {
                PowerOutput = -BaseLoad;
                bottom.PowerOutput = -bottom.BaseLoad;
                return;
            }

            float diff = topNet.CurrentStoredEnergy() - botNet.CurrentStoredEnergy();
            float transfer = Mathf.Clamp(diff * TransferGain, -capWatts, capWatts); // + = down
            float down = Mathf.Max(0f, transfer);
            float up = Mathf.Max(0f, -transfer);

            // Negative output = drawing from the local grid; positive = feeding it.
            PowerOutput = -BaseLoad - down + up;
            bottom.PowerOutput = -bottom.BaseLoad + down - up;
        }
    }
}

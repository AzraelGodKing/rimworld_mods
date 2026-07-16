using UnityEngine;
using Verse;

namespace Strata
{
    // Cross-level pipe tie for one resource network (water, helixien gas, coolant, etc.).
    // Each backend reads/writes its framework's per-map net through reflection — no
    // network merging across maps, same demand-driven shape as CompPowerShaft.
    public abstract class ShaftFluidBackend
    {
        public abstract string ChannelId { get; }

        public abstract string Label { get; }

        public abstract bool IsActive { get; }

        public abstract object GetNetFromJunction(Thing junction);

        public abstract float MaxTransferPerPulse(object net);

        public abstract float NetBalance(object net);

        public abstract float NetSupply(object net, float balance);

        public abstract bool Transfer(object fromNet, object toNet, float amount, CompShaftFluidTie fromTie = null, CompShaftFluidTie toTie = null);

        public virtual void DriveTie(object topNet, object bottomNet, CompShaftFluidTie topTie = null, CompShaftFluidTie bottomTie = null)
        {
            if (topNet == null || bottomNet == null || ReferenceEquals(topNet, bottomNet))
            {
                return;
            }

            float topBalance = NetBalance(topNet);
            float botBalance = NetBalance(bottomNet);

            float botWant = Mathf.Max(0f, -botBalance);
            float topWant = Mathf.Max(0f, -topBalance);

            float down = Mathf.Min(botWant, NetSupply(topNet, topBalance));
            float up = Mathf.Min(topWant, NetSupply(bottomNet, botBalance));
            float transfer = down - up;
            if (transfer > 0.001f)
            {
                Transfer(topNet, bottomNet, transfer, topTie, bottomTie);
            }
            else if (transfer < -0.001f)
            {
                Transfer(bottomNet, topNet, -transfer, bottomTie, topTie);
            }
        }
    }
}

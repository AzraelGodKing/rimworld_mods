using UnityEngine;
using Verse;

namespace Strata
{
    // Cross-level pipe tie for one resource network (water, helixien gas, coolant, etc.).
    // Each backend reads/writes its framework's per-map net through reflection — no
    // network merging across maps, same demand-driven shape as CompPowerShaft.
    //
    // When the destination has demand but little tank room, leftovers park in
    // CompShaftFluidTie.ShaftResourceBuffer so tankless pockets still feel supplied.
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

        // How much the net can accept into storage this pulse (0 = tanks full / absent).
        public virtual float NetStorageRoom(object net)
        {
            return float.MaxValue;
        }

        // Push amount into net storage; returns unstored remainder.
        public virtual float PushIntoNet(object net, float amount)
        {
            return amount;
        }

        public virtual void DriveTie(object topNet, object bottomNet, CompShaftFluidTie topTie = null, CompShaftFluidTie bottomTie = null)
        {
            if (topNet == null || bottomNet == null || ReferenceEquals(topNet, bottomNet))
            {
                return;
            }

            FlushBufferIntoNet(topTie, topNet);
            FlushBufferIntoNet(bottomTie, bottomNet);

            float topBalance = NetBalance(topNet);
            float botBalance = NetBalance(bottomNet);

            float botWant = Want(bottomNet, botBalance, bottomTie);
            float topWant = Want(topNet, topBalance, topTie);

            float down = Mathf.Min(botWant, NetSupply(topNet, topBalance) + BufferSupply(topTie));
            float up = Mathf.Min(topWant, NetSupply(bottomNet, botBalance) + BufferSupply(bottomTie));
            float transfer = down - up;
            if (transfer > 0.001f)
            {
                TransferWithBuffer(topNet, bottomNet, transfer, topTie, bottomTie);
            }
            else if (transfer < -0.001f)
            {
                TransferWithBuffer(bottomNet, topNet, -transfer, bottomTie, topTie);
            }
        }

        // Live deficit, plus shaft-buffer fill when tanks are missing / full.
        protected float Want(object net, float balance, CompShaftFluidTie tie)
        {
            float deficit = Mathf.Max(0f, -balance);
            if (tie == null)
            {
                return deficit;
            }
            float room = NetStorageRoom(net);
            if (room >= 0.001f)
            {
                return deficit;
            }
            // Tankless (or full): keep a capped junction buffer so host surplus
            // can still feed pocket consumers without a local tower/tank.
            return Mathf.Max(deficit, Mathf.Min(tie.ShaftResourceBufferRoom, MaxTransferPerPulse(net)));
        }

        protected void TransferWithBuffer(
            object fromNet,
            object toNet,
            float amount,
            CompShaftFluidTie fromTie,
            CompShaftFluidTie toTie)
        {
            float fromBuffer = fromTie != null ? fromTie.TakeShaftResourceBuffer(amount) : 0f;
            float stillNeed = amount - fromBuffer;
            if (stillNeed > 0.001f)
            {
                Transfer(fromNet, toNet, stillNeed, fromTie, toTie);
            }
            if (fromBuffer > 0.001f)
            {
                float leftover = PushIntoNet(toNet, fromBuffer);
                if (leftover > 0.001f && toTie != null)
                {
                    toTie.AddShaftResourceBuffer(leftover);
                }
            }
        }

        protected void FlushBufferIntoNet(CompShaftFluidTie tie, object net)
        {
            if (tie == null || net == null || tie.ShaftResourceBuffer < 0.001f)
            {
                return;
            }
            // Always attempt push — some nets accept without local tanks (DBH
            // PlumbingNet.PushWater); leftovers stay in the junction buffer.
            float take = tie.ShaftResourceBuffer;
            float leftover = PushIntoNet(net, take);
            float accepted = take - leftover;
            if (accepted > 0.001f)
            {
                tie.TakeShaftResourceBuffer(accepted);
            }
        }

        protected static float BufferSupply(CompShaftFluidTie tie)
        {
            return tie?.ShaftResourceBuffer ?? 0f;
        }

        // Park unstored remainder on the destination junction (capped).
        protected static void ParkLeftover(CompShaftFluidTie toTie, float leftover)
        {
            if (toTie != null && leftover > 0.001f)
            {
                toTie.AddShaftResourceBuffer(leftover);
            }
        }
    }
}

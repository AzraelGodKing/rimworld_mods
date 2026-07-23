using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Strata
{
    // Rimefeller crude-oil and chemfuel pipe nets (PipelineNet + CompPipe).
    public sealed class RimefellerPipelineBackend : ShaftFluidBackend
    {
        private const string PackageId = "dubwise.rimefeller";

        private readonly string channelId;

        private readonly string label;

        private readonly string modeName;

        private readonly bool fuelMode;

        private Type compPipeType;

        private Type pipelineNetType;

        private PropertyInfo pipeNetProp;

        private PropertyInfo modeProp;

        private PropertyInfo storedProp;

        private MethodInfo pushMethod;

        private MethodInfo pullMethod;

        private bool bindLogged;

        public RimefellerPipelineBackend(string channelId, string label, string modeName, bool fuelMode)
        {
            this.channelId = channelId;
            this.label = label;
            this.modeName = modeName;
            this.fuelMode = fuelMode;
        }

        public override string ChannelId => channelId;

        public override string Label => label;

        public override bool IsActive => ModLister.GetActiveModWithIdentifier(PackageId) != null && TryBind();

        public override object GetNetFromJunction(Thing junction)
        {
            if (!TryBind())
            {
                return null;
            }
            return ShaftFluidAdjacentNet.FirstNet(junction, twc => GetNetFromComp(FindComp(twc)));
        }

        public override float MaxTransferPerPulse(object net) => 500f;

        public override float NetBalance(object net)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            return NetStored(net);
        }

        public override float NetSupply(object net, float balance)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            return Mathf.Max(0f, NetStored(net));
        }

        // Rimefeller Push* typically returns unstored remainder when tanks are full.
        public override float NetStorageRoom(object net)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            // No reliable capacity API — treat empty stored as "may need buffer"
            // and positive stored as having somewhere to go. Soft room for flush.
            return NetStored(net) > 0.001f ? MaxTransferPerPulse(net) : 0f;
        }

        public override float PushIntoNet(object net, float amount)
        {
            if (!TryBind() || net == null || amount <= 0f || pushMethod == null)
            {
                return amount;
            }
            object result = pushMethod.Invoke(net, new object[] { amount });
            if (result is float leftover)
            {
                return Mathf.Max(0f, leftover);
            }
            return 0f;
        }

        public override bool Transfer(object fromNet, object toNet, float amount, CompShaftFluidTie fromTie = null, CompShaftFluidTie toTie = null)
        {
            if (!TryBind() || fromNet == null || toNet == null || amount <= 0f)
            {
                return false;
            }
            amount = Mathf.Min(amount, MaxTransferPerPulse(fromNet), NetStored(fromNet));
            if (amount <= 0.001f || pushMethod == null)
            {
                return false;
            }
            float moved = amount;
            if (pullMethod != null)
            {
                object pulled = pullMethod.Invoke(fromNet, new object[] { amount });
                if (pulled is float f)
                {
                    moved = f;
                }
            }
            if (moved <= 0.001f)
            {
                return false;
            }
            float leftover = PushIntoNet(toNet, moved);
            ParkLeftover(toTie, leftover);
            return true;
        }

        private float NetStored(object net)
        {
            if (storedProp == null)
            {
                return 0f;
            }
            object value = storedProp.GetValue(net, null);
            return value is float f ? Mathf.Max(0f, f) : 0f;
        }

        private object FindComp(ThingWithComps thing)
        {
            foreach (ThingComp comp in thing.AllComps)
            {
                if (comp == null || !compPipeType.IsInstanceOfType(comp))
                {
                    continue;
                }
                object mode = modeProp.GetValue(comp, null);
                if (mode != null && string.Equals(mode.ToString(), modeName, StringComparison.OrdinalIgnoreCase))
                {
                    return comp;
                }
            }
            return null;
        }

        private object GetNetFromComp(object comp)
        {
            if (comp == null)
            {
                return null;
            }
            object net = pipeNetProp.GetValue(comp, null);
            return net != null && pipelineNetType.IsInstanceOfType(net) ? net : null;
        }

        private bool TryBind()
        {
            if (compPipeType != null)
            {
                return true;
            }
            Assembly asm = ReflectionUtil.FindAssembly("Rimefeller");
            compPipeType = ReflectionUtil.TypeIn("Rimefeller.CompPipe", asm);
            pipelineNetType = ReflectionUtil.TypeIn("Rimefeller.PipelineNet", asm);
            if (compPipeType == null || pipelineNetType == null)
            {
                LogBindOnce("Rimefeller.dll types not found.");
                return false;
            }

            pipeNetProp = compPipeType.GetProperty("pipeNet", BindingFlags.Instance | BindingFlags.Public);
            modeProp = compPipeType.GetProperty("mode", BindingFlags.Instance | BindingFlags.Public);
            string storedName = fuelMode ? "TotalFuelNow" : "UsableOilStorage";
            storedProp = pipelineNetType.GetProperty(storedName, BindingFlags.Instance | BindingFlags.Public)
                ?? pipelineNetType.GetProperty(fuelMode ? "TotalFuel" : "TotalOil", BindingFlags.Instance | BindingFlags.Public);
            pushMethod = pipelineNetType.GetMethod(fuelMode ? "PushFuel" : "PushCrude", BindingFlags.Instance | BindingFlags.Public);
            pullMethod = pipelineNetType.GetMethod(fuelMode ? "PullFuel" : "PullOil", BindingFlags.Instance | BindingFlags.Public);

            if (pipeNetProp == null || modeProp == null || storedProp == null || pushMethod == null)
            {
                LogBindOnce("Rimefeller reflection bind incomplete.");
                compPipeType = null;
                return false;
            }
            return true;
        }

        private void LogBindOnce(string message)
        {
            if (bindLogged)
            {
                return;
            }
            bindLogged = true;
            Log.Warning($"[Strata] Rimefeller adapter ({channelId}): " + message);
        }
    }
}

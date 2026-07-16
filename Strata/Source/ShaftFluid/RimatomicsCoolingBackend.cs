using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Strata
{
    // Rimatomics coolant loops recompute CoolingCapacity and CoolingLoopRatio every
    // tick from coolers and turbines. The shaft tie pools spare capacity into each
    // junction's buffer and a Harmony postfix applies that bonus to CoolingLoopRatio.
    public sealed class RimatomicsCoolingBackend : ShaftFluidBackend
    {
        private const string PackageId = "dubwise.rimatomics";

        private const string RimatomicsChannel = "rimatomics_coolant";

        private Type compPipeType;

        private Type coolingNetType;

        private PropertyInfo netProp;

        private PropertyInfo modeProp;

        private FieldInfo coolingCapacityField;

        private FieldInfo coolingLoopRatioField;

        private FieldInfo turbinesField;

        private FieldInfo pipedThingsField;

        private bool bindLogged;

        public override string ChannelId => RimatomicsChannel;

        public override string Label => "Rimatomics coolant";

        public override bool IsActive => ModLister.GetActiveModWithIdentifier(PackageId) != null && TryBind();

        public override object GetNetFromJunction(Thing junction)
        {
            if (!TryBind())
            {
                return null;
            }
            return ShaftFluidAdjacentNet.FirstNet(junction, twc =>
            {
                object comp = FindComp(twc);
                object net = comp == null ? null : netProp.GetValue(comp, null);
                return net != null && coolingNetType.IsInstanceOfType(net) ? net : null;
            });
        }

        public override float MaxTransferPerPulse(object net) => 1000f;

        public override float NetBalance(object net)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            float capacity = (float)coolingCapacityField.GetValue(net);
            float load = SumTurbineLoad(net);
            return capacity + SumShaftBuffersOnNet(net) - load;
        }

        public override float NetSupply(object net, float balance)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            float spare = Mathf.Max(0f, balance);
            if (spare > 0f)
            {
                return spare;
            }
            return (float)coolingCapacityField.GetValue(net) + SumShaftBuffersOnNet(net);
        }

        public override bool Transfer(object fromNet, object toNet, float amount, CompShaftFluidTie fromTie = null, CompShaftFluidTie toTie = null)
        {
            if (!TryBind() || fromNet == null || toNet == null || amount <= 0f || fromTie == null || toTie == null)
            {
                return false;
            }
            amount = Mathf.Min(amount, MaxTransferPerPulse(fromNet));
            float liveSpare = Mathf.Max(0f, (float)coolingCapacityField.GetValue(fromNet) - SumTurbineLoad(fromNet));
            float available = fromTie.ShaftCoolingBuffer + liveSpare;
            float moved = Mathf.Min(amount, available);
            if (moved <= 0f)
            {
                return false;
            }
            fromTie.TakeShaftCoolingBuffer(moved);
            toTie.AddShaftCoolingBuffer(moved);
            return true;
        }

        internal void RecalculateLoopRatio(object net)
        {
            if (!TryBind() || net == null || !coolingNetType.IsInstanceOfType(net))
            {
                return;
            }
            float capacity = (float)coolingCapacityField.GetValue(net);
            if (capacity <= 1f)
            {
                return;
            }
            float load = SumTurbineLoad(net);
            float effectiveCapacity = capacity + SumShaftBuffersOnNet(net);
            if (effectiveCapacity <= 1f)
            {
                return;
            }
            float spareRatio = (effectiveCapacity - load) / effectiveCapacity;
            coolingLoopRatioField.SetValue(net, 1f - spareRatio);
        }

        private float SumShaftBuffersOnNet(object net)
        {
            if (!TryBind() || net == null)
            {
                return 0f;
            }
            IList pipedThings = pipedThingsField.GetValue(net) as IList;
            if (pipedThings == null)
            {
                return 0f;
            }
            float sum = 0f;
            for (int i = 0; i < pipedThings.Count; i++)
            {
                if (pipedThings[i] is Thing thing)
                {
                    CompShaftFluidTie tie = thing.TryGetComp<CompShaftFluidTie>();
                    if (tie != null && tie.Props.channel == RimatomicsChannel)
                    {
                        sum += tie.ShaftCoolingBuffer;
                    }
                }
            }
            return sum;
        }

        private float SumTurbineLoad(object net)
        {
            IList turbines = turbinesField.GetValue(net) as IList;
            if (turbines == null)
            {
                return 0f;
            }
            float sum = 0f;
            for (int i = 0; i < turbines.Count; i++)
            {
                object turbine = turbines[i];
                if (turbine == null)
                {
                    continue;
                }
                sum += ReflectionUtil.GetFloat(turbine, "UncappedPowerGeneration", "GenerationCapacity");
            }
            return sum;
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
                if (mode != null && string.Equals(mode.ToString(), "Cooling", StringComparison.OrdinalIgnoreCase))
                {
                    return comp;
                }
            }
            return null;
        }

        private bool TryBind()
        {
            if (compPipeType != null)
            {
                return true;
            }
            Assembly asm = ReflectionUtil.FindAssembly("Rimatomics");
            compPipeType = ReflectionUtil.TypeIn("Rimatomics.CompPipe", asm);
            coolingNetType = ReflectionUtil.TypeIn("Rimatomics.CoolingNet", asm);
            if (compPipeType == null || coolingNetType == null)
            {
                LogBindOnce("Rimatomics.dll types not found.");
                return false;
            }

            netProp = compPipeType.GetProperty("net", BindingFlags.Instance | BindingFlags.Public);
            modeProp = compPipeType.GetProperty("mode", BindingFlags.Instance | BindingFlags.Public);
            coolingCapacityField = coolingNetType.GetField("CoolingCapacity", BindingFlags.Instance | BindingFlags.Public);
            coolingLoopRatioField = coolingNetType.GetField("CoolingLoopRatio", BindingFlags.Instance | BindingFlags.Public);
            turbinesField = coolingNetType.GetField("Turbines", BindingFlags.Instance | BindingFlags.Public);
            pipedThingsField = coolingNetType.GetField("PipedThings", BindingFlags.Instance | BindingFlags.Public)
                ?? coolingNetType.BaseType?.GetField("PipedThings", BindingFlags.Instance | BindingFlags.Public);

            if (netProp == null || modeProp == null || coolingCapacityField == null
                || coolingLoopRatioField == null || turbinesField == null || pipedThingsField == null)
            {
                LogBindOnce("Rimatomics reflection bind incomplete.");
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
            Log.Warning("[Strata] Rimatomics coolant adapter: " + message);
        }
    }
}

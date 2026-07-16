using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Strata
{
    // Dubs Central Heating: hot-water (Heating) and air-con (Air) nets share
    // DubsCentralHeating.PlumbingNet with HeatStore HeaterTemp as stored energy.
    public sealed class DchPlumbingBackend : ShaftFluidBackend
    {
        private const string PackageId = "dubwise.dubscentralheating";

        private readonly string channelId;

        private readonly string label;

        private readonly string modeName;

        private Type compPipeType;

        private Type plumbingNetType;

        private Type compHeatStoreType;

        private PropertyInfo pipeNetProp;

        private PropertyInfo modeProp;

        private FieldInfo heatStoresField;

        private PropertyInfo heaterTempProp;

        private PropertyInfo storeCapacityProp;

        private FieldInfo heatingCapField;

        private FieldInfo coolingCapField;

        private bool bindLogged;

        public DchPlumbingBackend(string channelId, string label, string modeName)
        {
            this.channelId = channelId;
            this.label = label;
            this.modeName = modeName;
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

        public override float MaxTransferPerPulse(object net) => 250f;

        public override float NetBalance(object net)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            if (modeName == "Air")
            {
                float cap = (float)coolingCapField.GetValue(net);
                return cap - 0.5f;
            }
            return SumHeatStored(net) - SumHeatCapacity(net) * 0.35f;
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
            return modeName == "Air"
                ? Mathf.Max(0f, (float)coolingCapField.GetValue(net))
                : SumHeatStored(net);
        }

        public override bool Transfer(object fromNet, object toNet, float amount, CompShaftFluidTie fromTie = null, CompShaftFluidTie toTie = null)
        {
            if (!TryBind() || fromNet == null || toNet == null || amount <= 0f)
            {
                return false;
            }
            if (modeName == "Air")
            {
                float give = Mathf.Min(amount / 500f, Mathf.Max(0f, (float)coolingCapField.GetValue(fromNet)));
                coolingCapField.SetValue(fromNet, (float)coolingCapField.GetValue(fromNet) - give);
                coolingCapField.SetValue(toNet, Mathf.Min(1f, (float)coolingCapField.GetValue(toNet) + give));
                return give > 0f;
            }

            float remaining = Mathf.Min(amount, MaxTransferPerPulse(fromNet));
            IList stores = heatStoresField.GetValue(fromNet) as IList;
            IList dest = heatStoresField.GetValue(toNet) as IList;
            if (stores == null || dest == null)
            {
                return false;
            }
            float moved = 0f;
            for (int i = 0; i < stores.Count && remaining > 0.001f; i++)
            {
                object src = stores[i];
                if (src == null)
                {
                    continue;
                }
                float temp = (float)heaterTempProp.GetValue(src, null);
                float take = Mathf.Min(remaining, temp * GetCapacity(src));
                if (take <= 0f)
                {
                    continue;
                }
                heaterTempProp.SetValue(src, temp - take / Mathf.Max(1f, GetCapacity(src)), null);
                AddHeat(dest, take);
                remaining -= take;
                moved += take;
            }
            return moved > 0f;
        }

        private void AddHeat(IList destStores, float amount)
        {
            for (int i = 0; i < destStores.Count && amount > 0.001f; i++)
            {
                object store = destStores[i];
                if (store == null)
                {
                    continue;
                }
                float cap = GetCapacity(store);
                float temp = (float)heaterTempProp.GetValue(store, null);
                float room = (1f - temp) * cap;
                float add = Mathf.Min(amount, room);
                if (add <= 0f)
                {
                    continue;
                }
                heaterTempProp.SetValue(store, temp + add / Mathf.Max(1f, cap), null);
                amount -= add;
            }
        }

        private float GetCapacity(object heatStore)
        {
            object cap = storeCapacityProp.GetValue(heatStore, null);
            return cap is float f ? Mathf.Max(1f, f) : 1f;
        }

        private float SumHeatStored(object net)
        {
            IList stores = heatStoresField.GetValue(net) as IList;
            if (stores == null)
            {
                return 0f;
            }
            float sum = 0f;
            for (int i = 0; i < stores.Count; i++)
            {
                object store = stores[i];
                if (store == null)
                {
                    continue;
                }
                float temp = (float)heaterTempProp.GetValue(store, null);
                sum += temp * GetCapacity(store);
            }
            return sum;
        }

        private float SumHeatCapacity(object net)
        {
            IList stores = heatStoresField.GetValue(net) as IList;
            if (stores == null)
            {
                return 0f;
            }
            float sum = 0f;
            for (int i = 0; i < stores.Count; i++)
            {
                object store = stores[i];
                if (store != null)
                {
                    sum += GetCapacity(store);
                }
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
                if (mode != null && string.Equals(mode.ToString(), modeName, StringComparison.OrdinalIgnoreCase))
                {
                    return comp;
                }
            }
            return null;
        }

        private object GetNetFromComp(object comp)
        {
            return comp == null ? null : pipeNetProp.GetValue(comp, null);
        }

        private bool TryBind()
        {
            if (compPipeType != null)
            {
                return true;
            }
            Assembly asm = ReflectionUtil.FindAssembly("DubsCentralHeating");
            compPipeType = ReflectionUtil.TypeIn("DubsCentralHeating.CompPipe", asm);
            plumbingNetType = ReflectionUtil.TypeIn("DubsCentralHeating.PlumbingNet", asm);
            compHeatStoreType = ReflectionUtil.TypeIn("DubsCentralHeating.CompHeatStore", asm);
            if (compPipeType == null || plumbingNetType == null)
            {
                LogBindOnce("DubsCentralHeating.dll types not found.");
                return false;
            }

            pipeNetProp = compPipeType.GetProperty("pipeNet", BindingFlags.Instance | BindingFlags.Public);
            modeProp = compPipeType.GetProperty("mode", BindingFlags.Instance | BindingFlags.Public);
            heatStoresField = plumbingNetType.GetField("HeatStores", BindingFlags.Instance | BindingFlags.Public);
            heatingCapField = plumbingNetType.GetField("HeatingCap", BindingFlags.Instance | BindingFlags.Public);
            coolingCapField = plumbingNetType.GetField("CoolingCap", BindingFlags.Instance | BindingFlags.Public);
            if (compHeatStoreType != null)
            {
                heaterTempProp = compHeatStoreType.GetProperty("HeaterTemp", BindingFlags.Instance | BindingFlags.Public);
                storeCapacityProp = compHeatStoreType.GetProperty("GetStoreCapacity", BindingFlags.Instance | BindingFlags.Public);
            }

            if (pipeNetProp == null || modeProp == null || heatStoresField == null
                || heatingCapField == null || coolingCapField == null
                || (modeName != "Air" && (heaterTempProp == null || storeCapacityProp == null)))
            {
                LogBindOnce("DubsCentralHeating reflection bind incomplete.");
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
            Log.Warning($"[Strata] DCH adapter ({channelId}): " + message);
        }
    }
}

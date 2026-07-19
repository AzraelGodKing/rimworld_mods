using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Strata
{
    // Vanilla Temperature Expanded AC ducts use AcPipeNet: efficiency is
    // Production − Consumption on one map, and requires exactly one controller.
    // Stored-resource transfer cannot bridge floors — this backend pairs surface
    // compressors/controllers with underground AC units via shaft junctions.
    public sealed class VteAcPipeBackend : ShaftFluidBackend
    {
        public const string Channel = "vte_efficiency";

        public const string PipeNetDefName = "VTE_EfficiencyNet";

        private const float MinEff = 0f;

        private const float MaxEff = 1.5f;

        private const float BaseEff = 1f;

        private static readonly Dictionary<object, object> pairedNets = new Dictionary<object, object>();

        private Type acPipeNetType;

        private Type pipeNetManagerType;

        private Type pipeNetDefType;

        private Type compResourceType;

        private Type compResourceTraderType;

        private FieldInfo efficiencyField;

        private FieldInfo controllerListField;

        private FieldInfo receiversField;

        private FieldInfo producersField;

        private FieldInfo receiversOffField;

        private FieldInfo producersOffField;

        private PropertyInfo productionProp;

        private PropertyInfo consumptionProp;

        private PropertyInfo pipeNetProp;

        private PropertyInfo resourceOnProp;

        private FieldInfo powerCompField;

        private MethodInfo canBeOnMethod;

        private MethodInfo getPipeNetAtMethod;

        private Def cachedNetDef;

        private bool bindLogged;

        public override string ChannelId => Channel;

        public override string Label => "VTE air duct";

        public override bool IsActive =>
            ModsConfig.IsActive("VanillaExpanded.Temperature")
            && ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core")
            && TryBind()
            && GetNetDef() != null;

        public override object GetNetFromJunction(Thing junction)
        {
            if (!TryBind() || junction?.Map == null)
            {
                return null;
            }

            object manager = junction.Map.GetComponent(pipeNetManagerType);
            Def netDef = GetNetDef();
            if (manager != null && netDef != null && getPipeNetAtMethod != null)
            {
                foreach (IntVec3 cell in junction.OccupiedRect())
                {
                    object net = getPipeNetAtMethod.Invoke(manager, new object[] { cell, netDef });
                    if (net != null)
                    {
                        return net;
                    }
                }
            }

            return ShaftFluidAdjacentNet.FirstNet(junction, GetNetFromThing);
        }

        public override float MaxTransferPerPulse(object net) => 0f;

        public override float NetBalance(object net)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            return ProductionOf(net) - ConsumptionOf(net);
        }

        public override float NetSupply(object net, float balance) => Mathf.Max(0f, balance);

        public override bool Transfer(object fromNet, object toNet, float amount, CompShaftFluidTie fromTie = null, CompShaftFluidTie toTie = null)
        {
            // Efficiency is not a transferable stored fluid.
            return false;
        }

        public override void DriveTie(object topNet, object bottomNet, CompShaftFluidTie topTie = null, CompShaftFluidTie bottomTie = null)
        {
            if (!TryBind() || topNet == null || bottomNet == null || ReferenceEquals(topNet, bottomNet))
            {
                return;
            }
            if (!IsAcPipeNet(topNet) || !IsAcPipeNet(bottomNet))
            {
                return;
            }
            RegisterPair(topNet, bottomNet);
            SyncPair(topNet, bottomNet);
        }

        internal void OnPipeNetTicked(object net)
        {
            if (!TryBind() || net == null || !IsAcPipeNet(net))
            {
                return;
            }
            if (!pairedNets.TryGetValue(net, out object partner) || partner == null)
            {
                return;
            }
            SyncPair(net, partner);
        }

        private void SyncPair(object netA, object netB)
        {
            float production = ProductionOf(netA) + ProductionOf(netB);
            float consumption = ConsumptionOf(netA) + ConsumptionOf(netB);
            float efficiency = CalculateEfficiency(production, consumption);
            SetEfficiency(netA, efficiency);
            SetEfficiency(netB, efficiency);

            int controllers = ActiveControllerCount(netA) + ActiveControllerCount(netB);
            bool healthy = controllers == 1 && efficiency > MinEff && !WouldOverloadCombined(netA, netB);

            if (!healthy)
            {
                ShutDownTraders(netA);
                ShutDownTraders(netB);
                ApplyControllerState(netA, stable: false);
                ApplyControllerState(netB, stable: false);
                return;
            }

            TurnOnEligible(ReceiversOff(netA));
            TurnOnEligible(ReceiversOff(netB));
            TurnOnEligible(ProducersOff(netA));
            TurnOnEligible(ProducersOff(netB));

            // Recompute after turning devices on (mirrors VTE's own tick order).
            production = ProductionOf(netA) + ProductionOf(netB);
            consumption = ConsumptionOf(netA) + ConsumptionOf(netB);
            efficiency = CalculateEfficiency(production, consumption);
            SetEfficiency(netA, efficiency);
            SetEfficiency(netB, efficiency);

            if (efficiency <= MinEff)
            {
                ShutDownTraders(netA);
                ShutDownTraders(netB);
                ApplyControllerState(netA, stable: false);
                ApplyControllerState(netB, stable: false);
                return;
            }

            ApplyControllerState(netA, stable: true);
            ApplyControllerState(netB, stable: true);
        }

        private bool WouldOverloadCombined(object netA, object netB)
        {
            float production = ProductionOf(netA) + ProductionOf(netB)
                + PotentialProducerBoost(netA) + PotentialProducerBoost(netB);
            float consumption = ConsumptionOf(netA) + ConsumptionOf(netB)
                + PotentialReceiverDraw(netA) + PotentialReceiverDraw(netB);
            return CalculateEfficiency(production, consumption) <= MinEff;
        }

        private float PotentialProducerBoost(object net)
        {
            // VTE: Production + -Sum(producersOff.Consumption) for CanBeOn offs.
            float sum = 0f;
            foreach (object trader in Enumerate(ProducersOff(net)))
            {
                if (CanBeOn(trader))
                {
                    sum -= ReflectionUtil.GetFloat(trader, "Consumption");
                }
            }
            return sum;
        }

        private float PotentialReceiverDraw(object net)
        {
            float sum = 0f;
            foreach (object trader in Enumerate(ReceiversOff(net)))
            {
                if (CanBeOn(trader))
                {
                    sum += ReflectionUtil.GetFloat(trader, "Consumption");
                }
            }
            return sum;
        }

        private static float CalculateEfficiency(float production, float consumption)
        {
            if (production <= 0f)
            {
                return 0f;
            }
            float factor = production - consumption;
            if (factor < 0f)
            {
                return Mathf.Max(MinEff, BaseEff - 0.05f * Mathf.Abs(factor));
            }
            return Mathf.Min(MaxEff, BaseEff + 0.01f * Mathf.Abs(factor));
        }

        private void TurnOnEligible(IList list)
        {
            if (list == null)
            {
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                object trader = list[i];
                if (!CanBeOn(trader))
                {
                    continue;
                }
                resourceOnProp.SetValue(trader, true, null);
                UpdateTraderOverlay(trader);
            }
        }

        private void ShutDownTraders(object net)
        {
            foreach (object trader in Enumerate(Receivers(net)))
            {
                resourceOnProp.SetValue(trader, false, null);
                object powerComp = powerCompField?.GetValue(trader);
                if (powerComp != null)
                {
                    object props = ReflectionUtil.GetMember(powerComp, "Props");
                    float basePower = ReflectionUtil.GetFloat(props, "PowerConsumption");
                    PropertyInfo powerOut = AccessTools.Property(powerComp.GetType(), "PowerOutput");
                    if (powerOut != null && powerOut.CanWrite)
                    {
                        powerOut.SetValue(powerComp, -basePower, null);
                    }
                }
                UpdateTraderOverlay(trader);
            }
            foreach (object trader in Enumerate(Producers(net)))
            {
                resourceOnProp.SetValue(trader, false, null);
                UpdateTraderOverlay(trader);
            }
        }

        private void ApplyControllerState(object net, bool stable)
        {
            IList controllers = controllerListField.GetValue(net) as IList;
            if (controllers == null)
            {
                return;
            }
            int canBeOnCount = 0;
            for (int i = 0; i < controllers.Count; i++)
            {
                object resourceComp = ReflectionUtil.GetMember(controllers[i], "resourceComp");
                if (CanBeOn(resourceComp))
                {
                    canBeOnCount++;
                }
            }
            for (int i = 0; i < controllers.Count; i++)
            {
                object controller = controllers[i];
                object resourceComp = ReflectionUtil.GetMember(controller, "resourceComp");
                if (resourceComp != null && stable && canBeOnCount == 1 && CanBeOn(resourceComp))
                {
                    resourceOnProp.SetValue(resourceComp, true, null);
                }
                ReflectionUtil.InvokeVoid(resourceComp, "UpdateOverlayHandle");
            }
        }

        private int ActiveControllerCount(object net)
        {
            IList controllers = controllerListField.GetValue(net) as IList;
            if (controllers == null)
            {
                return 0;
            }
            int count = 0;
            for (int i = 0; i < controllers.Count; i++)
            {
                object resourceComp = ReflectionUtil.GetMember(controllers[i], "resourceComp");
                if (resourceComp == null)
                {
                    continue;
                }
                bool on = resourceOnProp != null && (bool)resourceOnProp.GetValue(resourceComp, null);
                if (on || CanBeOn(resourceComp))
                {
                    count++;
                }
            }
            return count;
        }

        private void UpdateTraderOverlay(object trader)
        {
            ReflectionUtil.InvokeVoid(trader, "UpdateOverlayHandle");
        }

        private bool CanBeOn(object trader)
        {
            if (trader == null || canBeOnMethod == null)
            {
                return false;
            }
            return (bool)canBeOnMethod.Invoke(trader, null);
        }

        private float ProductionOf(object net) => (float)productionProp.GetValue(net, null);

        private float ConsumptionOf(object net) => (float)consumptionProp.GetValue(net, null);

        private void SetEfficiency(object net, float value) => efficiencyField.SetValue(net, value);

        private IList Receivers(object net) => receiversField.GetValue(net) as IList;

        private IList Producers(object net) => producersField.GetValue(net) as IList;

        private IList ReceiversOff(object net) => receiversOffField.GetValue(net) as IList;

        private IList ProducersOff(object net) => producersOffField.GetValue(net) as IList;

        private static IEnumerable<object> Enumerate(IList list)
        {
            if (list == null)
            {
                yield break;
            }
            for (int i = 0; i < list.Count; i++)
            {
                object item = list[i];
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        private static void RegisterPair(object a, object b)
        {
            pairedNets[a] = b;
            pairedNets[b] = a;
        }

        private bool IsAcPipeNet(object net) => acPipeNetType.IsInstanceOfType(net);

        private object GetNetFromThing(ThingWithComps thing)
        {
            foreach (ThingComp comp in thing.AllComps)
            {
                if (comp == null || !compResourceType.IsInstanceOfType(comp))
                {
                    continue;
                }
                object props = ReflectionUtil.GetMember(comp, "Props");
                object pipeNetDef = ReflectionUtil.GetMember(props, "pipeNet");
                if (pipeNetDef is Def def && def.defName != PipeNetDefName)
                {
                    continue;
                }
                if (pipeNetDef != null && !(pipeNetDef is Def)
                    && ReflectionUtil.GetMember(pipeNetDef, "defName") as string != PipeNetDefName)
                {
                    continue;
                }
                object net = pipeNetProp.GetValue(comp, null);
                if (net != null)
                {
                    return net;
                }
            }
            return null;
        }

        private Def GetNetDef()
        {
            if (cachedNetDef != null)
            {
                return cachedNetDef;
            }
            if (pipeNetDefType == null)
            {
                return null;
            }
            Type databaseType = typeof(DefDatabase<>).MakeGenericType(pipeNetDefType);
            MethodInfo getNamed = AccessTools.Method(databaseType, "GetNamedSilentFail");
            cachedNetDef = getNamed?.Invoke(null, new object[] { PipeNetDefName }) as Def;
            return cachedNetDef;
        }

        private bool TryBind()
        {
            if (acPipeNetType != null)
            {
                return true;
            }

            acPipeNetType = ReflectionUtil.TypeIn("VanillaTemperatureExpanded.AcPipeNet");
            Assembly pipeSystem = ReflectionUtil.FindAssembly("PipeSystem")
                ?? ReflectionUtil.FindAssembly("VEF");
            Type pipeNetType = ReflectionUtil.TypeIn("PipeSystem.PipeNet", pipeSystem);
            pipeNetManagerType = ReflectionUtil.TypeIn("PipeSystem.PipeNetManager", pipeSystem);
            pipeNetDefType = ReflectionUtil.TypeIn("PipeSystem.PipeNetDef", pipeSystem);
            compResourceType = ReflectionUtil.TypeIn("PipeSystem.CompResource", pipeSystem);
            compResourceTraderType = ReflectionUtil.TypeIn("PipeSystem.CompResourceTrader", pipeSystem);

            if (acPipeNetType == null || pipeNetType == null || pipeNetManagerType == null
                || compResourceType == null || compResourceTraderType == null)
            {
                LogBindOnce("VTE / PipeSystem types not found.");
                return false;
            }

            efficiencyField = AccessTools.Field(acPipeNetType, "Efficiency");
            controllerListField = AccessTools.Field(acPipeNetType, "ControllerList");
            receiversField = AccessTools.Field(pipeNetType, "receivers");
            producersField = AccessTools.Field(pipeNetType, "producers");
            receiversOffField = AccessTools.Field(pipeNetType, "receiversOff");
            producersOffField = AccessTools.Field(pipeNetType, "producersOff");
            productionProp = AccessTools.Property(pipeNetType, "Production");
            consumptionProp = AccessTools.Property(pipeNetType, "Consumption");
            pipeNetProp = AccessTools.Property(compResourceType, "PipeNet");
            resourceOnProp = AccessTools.Property(compResourceTraderType, "ResourceOn");
            powerCompField = AccessTools.Field(compResourceTraderType, "powerComp");
            canBeOnMethod = AccessTools.Method(compResourceTraderType, "CanBeOn");
            getPipeNetAtMethod = AccessTools.Method(pipeNetManagerType, "GetPipeNetAt");

            if (efficiencyField == null || controllerListField == null || receiversField == null
                || producersField == null || receiversOffField == null || producersOffField == null
                || productionProp == null || consumptionProp == null || pipeNetProp == null
                || resourceOnProp == null || canBeOnMethod == null || getPipeNetAtMethod == null)
            {
                LogBindOnce("VTE AC pipe reflection bind incomplete.");
                acPipeNetType = null;
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
            Log.Warning("[Strata] VTE AC shaft adapter: " + message);
        }
    }
}

using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Vanilla Expanded Framework PipeSystem (Helixien gas and other VEF pipe nets).
    public sealed class VefPipeSystemBackend : ShaftFluidBackend
    {
        private readonly string pipeNetDefName;

        private Type compResourceType;

        private Type pipeNetManagerType;

        private Type pipeNetType;

        private Type pipeNetDefType;

        private PropertyInfo pipeNetProp;

        private PropertyInfo storedProp;

        private PropertyInfo consumptionProp;

        private PropertyInfo productionProp;

        private PropertyInfo availableCapacityProp;

        private PropertyInfo overflowProp;

        private PropertyInfo refillableAmountProp;

        private MethodInfo getPipeNetAtMethod;

        private MethodInfo drawAmongStorageMethod;

        private MethodInfo distributeAmongStorageMethod;

        private Def cachedNetDef;

        private bool bindLogged;

        public VefPipeSystemBackend(string channelId, string label, string pipeNetDefName)
        {
            ChannelId = channelId;
            Label = label;
            this.pipeNetDefName = pipeNetDefName;
        }

        public override string ChannelId { get; }

        public override string Label { get; }

        public override bool IsActive =>
            ModsConfig.IsActive("VanillaExpanded.HelixienGas")
            && ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core")
            && TryBind();

        public override object GetNetFromJunction(Thing junction)
        {
            if (!TryBind() || junction?.Map == null)
            {
                return null;
            }

            object manager = GetPipeNetManager(junction.Map);
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

        public override void DriveTie(object topNet, object bottomNet, CompShaftFluidTie topTie = null, CompShaftFluidTie bottomTie = null)
        {
            if (!TryBind() || topNet == null || bottomNet == null || ReferenceEquals(topNet, bottomNet))
            {
                return;
            }

            float topSpare = DrawableAmount(topNet);
            float botSpare = DrawableAmount(bottomNet);
            float topNeed = ImportNeed(topNet);
            float botNeed = ImportNeed(bottomNet);

            float down = Mathf.Min(botNeed, topSpare);
            float up = Mathf.Min(topNeed, botSpare);
            float transfer = down - up;
            if (transfer > 0.001f)
            {
                Transfer(topNet, bottomNet, transfer);
            }
            else if (transfer < -0.001f)
            {
                Transfer(bottomNet, topNet, -transfer);
            }
        }

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
                if (pipeNetDef == null || !PipeNetDefMatches(pipeNetDef, pipeNetDefName))
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

        private static bool PipeNetDefMatches(object pipeNetDef, string expectedDefName)
        {
            if (pipeNetDef is Def def)
            {
                return def.defName == expectedDefName;
            }

            string defName = ReflectionUtil.GetMember(pipeNetDef, "defName") as string;
            return defName == expectedDefName;
        }

        public override float MaxTransferPerPulse(object net)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            object def = ReflectionUtil.GetMember(net, "def");
            float cap = ReflectionUtil.GetFloat(def, "transferAmount");
            return cap > 0f ? cap : 250f;
        }

        public override float NetBalance(object net)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            return DrawableAmount(net) - ImportNeed(net);
        }

        public override float NetSupply(object net, float balance)
        {
            return DrawableAmount(net);
        }

        public override bool Transfer(object fromNet, object toNet, float amount, CompShaftFluidTie fromTie = null, CompShaftFluidTie toTie = null)
        {
            if (!TryBind() || fromNet == null || toNet == null || amount <= 0f)
            {
                return false;
            }
            amount = Mathf.Min(amount, MaxTransferPerPulse(fromNet));
            object[] drawArgs = { amount, 0f, null, true };
            drawAmongStorageMethod.Invoke(fromNet, drawArgs);
            float moved = (float)drawArgs[1];
            if (moved <= 0.001f)
            {
                return false;
            }
            int distributeParamCount = distributeAmongStorageMethod.GetParameters().Length;
            object[] storeArgs = distributeParamCount == 4
                ? new object[] { moved, 0f, null, true }
                : new object[] { moved, 0f };
            distributeAmongStorageMethod.Invoke(toNet, storeArgs);
            float stored = (float)storeArgs[1];
            return stored > 0.001f;
        }

        private float DrawableAmount(object net)
        {
            float stored = (float)storedProp.GetValue(net, null);
            float overflow = (float)overflowProp.GetValue(net, null);
            return Mathf.Max(0f, stored + overflow);
        }

        private float ImportNeed(object net)
        {
            float room = (float)availableCapacityProp.GetValue(net, null);
            float refill = (float)refillableAmountProp.GetValue(net, null);
            float consumption = (float)consumptionProp.GetValue(net, null);
            float drawable = DrawableAmount(net);
            float consumerShortfall = Mathf.Max(0f, consumption * 10f - drawable);
            return room + refill + consumerShortfall;
        }

        private object GetPipeNetManager(Map map)
        {
            return map.GetComponent(pipeNetManagerType);
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
            cachedNetDef = getNamed?.Invoke(null, new object[] { pipeNetDefName }) as Def;
            return cachedNetDef;
        }

        private bool TryBind()
        {
            if (compResourceType != null)
            {
                return true;
            }
            Assembly vef = ReflectionUtil.FindAssembly("VEF");
            compResourceType = ReflectionUtil.TypeIn("PipeSystem.CompResource", vef);
            pipeNetManagerType = ReflectionUtil.TypeIn("PipeSystem.PipeNetManager", vef);
            pipeNetType = ReflectionUtil.TypeIn("PipeSystem.PipeNet", vef);
            pipeNetDefType = ReflectionUtil.TypeIn("PipeSystem.PipeNetDef", vef);
            if (compResourceType == null || pipeNetManagerType == null || pipeNetType == null)
            {
                LogBindOnce("PipeSystem types not found — is Vanilla Expanded Framework loaded?");
                return false;
            }

            pipeNetProp = compResourceType.GetProperty("PipeNet", BindingFlags.Instance | BindingFlags.Public);
            storedProp = pipeNetType.GetProperty("Stored", BindingFlags.Instance | BindingFlags.Public);
            consumptionProp = pipeNetType.GetProperty("Consumption", BindingFlags.Instance | BindingFlags.Public);
            productionProp = pipeNetType.GetProperty("Production", BindingFlags.Instance | BindingFlags.Public);
            availableCapacityProp = pipeNetType.GetProperty("AvailableCapacity", BindingFlags.Instance | BindingFlags.Public);
            overflowProp = pipeNetType.GetProperty("OverflowAmount", BindingFlags.Instance | BindingFlags.Public);
            refillableAmountProp = pipeNetType.GetProperty("RefillableAmount", BindingFlags.Instance | BindingFlags.Public);
            getPipeNetAtMethod = pipeNetManagerType.GetMethod("GetPipeNetAt", BindingFlags.Instance | BindingFlags.Public);
            drawAmongStorageMethod = FindPipeNetOutMethod("DrawAmongStorage", 4);
            distributeAmongStorageMethod = FindPipeNetOutMethod("DistributeAmongStorage", 2)
                ?? FindPipeNetOutMethod("DistributeAmongStorage", 4);

            if (pipeNetProp == null || storedProp == null || overflowProp == null
                || availableCapacityProp == null || refillableAmountProp == null
                || getPipeNetAtMethod == null
                || drawAmongStorageMethod == null || distributeAmongStorageMethod == null)
            {
                LogBindOnce("PipeSystem reflection bind incomplete.");
                compResourceType = null;
                return false;
            }
            return true;
        }

        private MethodInfo FindPipeNetOutMethod(string name, int paramCount)
        {
            MethodInfo[] methods = pipeNetType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != name)
                {
                    continue;
                }
                ParameterInfo[] parms = method.GetParameters();
                if (parms.Length == paramCount && parms[1].ParameterType.IsByRef)
                {
                    return method;
                }
            }
            return null;
        }

        private void LogBindOnce(string message)
        {
            if (bindLogged)
            {
                return;
            }
            bindLogged = true;
            Log.Warning("[Strata] VEF pipe adapter: " + message);
        }
    }
}

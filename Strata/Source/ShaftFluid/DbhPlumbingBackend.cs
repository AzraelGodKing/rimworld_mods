using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Strata
{
    // Dubs Bad Hygiene plumbing (PipeType.Sewage): shared water/sewage nets use
    // PlumbingNet.WaterStorage and PushWater between tower storage.
    public sealed class DbhPlumbingBackend : ShaftFluidBackend
    {
        private const string PackageId = "dubwise.dubsbadhygiene";

        private Type compPipeType;

        private Type plumbingNetType;

        private Type compWaterStorageType;

        private PropertyInfo pipeNetProp;

        private PropertyInfo modeProp;

        private PropertyInfo waterStorageNetProp;

        private FieldInfo waterTowersField;

        private FieldInfo towerWaterField;

        private PropertyInfo towerSpaceProp;

        private MethodInfo pushWaterMethod;

        private bool bindLogged;

        public override string ChannelId => "dbh_plumbing";

        public override string Label => "DBH plumbing";

        public override bool IsActive => ModLister.GetActiveModWithIdentifier(PackageId) != null && TryBind();

        public override object GetNetFromJunction(Thing junction)
        {
            if (!TryBind())
            {
                return null;
            }
            return ShaftFluidAdjacentNet.FirstNet(junction, twc => GetNetFromComp(FindComp(twc, "Sewage")));
        }

        public override float MaxTransferPerPulse(object net) => 500f;

        public override float NetBalance(object net)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            float stored = (float)waterStorageNetProp.GetValue(net, null);
            float importNeed = SumTowerSpace(GetTowers(net));
            return stored - importNeed;
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
            return (float)waterStorageNetProp.GetValue(net, null);
        }

        public override bool Transfer(object fromNet, object toNet, float amount, CompShaftFluidTie fromTie = null, CompShaftFluidTie toTie = null)
        {
            if (!TryBind() || fromNet == null || toNet == null || amount <= 0f)
            {
                return false;
            }
            amount = Mathf.Min(amount, MaxTransferPerPulse(fromNet));
            float remaining = amount;
            IList towers = GetTowers(fromNet);
            for (int i = 0; i < towers.Count && remaining > 0.001f; i++)
            {
                object tower = towers[i];
                float have = (float)towerWaterField.GetValue(tower);
                float draw = Mathf.Min(remaining, have);
                if (draw <= 0f)
                {
                    continue;
                }
                towerWaterField.SetValue(tower, have - draw);
                remaining -= draw;
            }
            float moved = amount - remaining;
            if (moved <= 0f)
            {
                return false;
            }
            object leftover = pushWaterMethod.Invoke(toNet, new object[] { moved });
            if (leftover is float f && f > 0.001f)
            {
                pushWaterMethod.Invoke(fromNet, new object[] { f });
            }
            return true;
        }

        private object FindComp(ThingWithComps thing, string modeName)
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

        private IList GetTowers(object net)
        {
            return waterTowersField.GetValue(net) as IList;
        }

        private float SumTowerSpace(IList towers)
        {
            if (towers == null)
            {
                return 0f;
            }
            float sum = 0f;
            for (int i = 0; i < towers.Count; i++)
            {
                object tower = towers[i];
                if (tower == null)
                {
                    continue;
                }
                sum += (float)towerSpaceProp.GetValue(tower, null);
            }
            return sum;
        }

        private bool TryBind()
        {
            if (compPipeType != null)
            {
                return true;
            }
            Assembly asm = ReflectionUtil.FindAssembly("BadHygiene");
            compPipeType = ReflectionUtil.TypeIn("DubsBadHygiene.CompPipe", asm);
            plumbingNetType = ReflectionUtil.TypeIn("DubsBadHygiene.PlumbingNet", asm);
            compWaterStorageType = ReflectionUtil.TypeIn("DubsBadHygiene.CompWaterStorage", asm);
            if (compPipeType == null || plumbingNetType == null || compWaterStorageType == null)
            {
                LogBindOnce("BadHygiene.dll types not found.");
                return false;
            }

            pipeNetProp = compPipeType.GetProperty("pipeNet", BindingFlags.Instance | BindingFlags.Public);
            modeProp = compPipeType.GetProperty("mode", BindingFlags.Instance | BindingFlags.Public);
            waterStorageNetProp = plumbingNetType.GetProperty("WaterStorage", BindingFlags.Instance | BindingFlags.Public);
            waterTowersField = plumbingNetType.GetField("WaterTowers", BindingFlags.Instance | BindingFlags.Public);
            towerWaterField = compWaterStorageType.GetField("WaterStorage", BindingFlags.Instance | BindingFlags.Public);
            towerSpaceProp = compWaterStorageType.GetProperty("space", BindingFlags.Instance | BindingFlags.Public);
            pushWaterMethod = plumbingNetType.GetMethod("PushWater", BindingFlags.Instance | BindingFlags.Public);

            if (pipeNetProp == null || modeProp == null || waterStorageNetProp == null
                || waterTowersField == null || towerWaterField == null || towerSpaceProp == null
                || pushWaterMethod == null)
            {
                LogBindOnce("BadHygiene reflection bind incomplete.");
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
            Log.Warning("[Strata] DBH plumbing adapter: " + message);
        }
    }
}

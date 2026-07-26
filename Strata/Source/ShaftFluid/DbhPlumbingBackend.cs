using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Strata
{
    // Dubs Bad Hygiene plumbing (PipeType.Sewage). Cross-level flow mirrors
    // AsAboveSoBelow.DBHWaterBridge: fill equalize at 0.5 damping via PullWater/PushWater.
    // Junctions carry CompWaterStorage (ShaftFluid_Comps.xml) so tankless floors still
    // appear in WaterTowers; leftovers park in CompShaftFluidTie.ShaftResourceBuffer.
    public sealed class DbhPlumbingBackend : ShaftFluidBackend
    {
        private const string PackageId = "dubwise.dubsbadhygiene";

        private const float EqualizeDamping = 0.5f;

        private const float MinTransfer = 0.05f;

        private Type compPipeType;

        private Type plumbingNetType;

        private Type compWaterStorageType;

        private Type contaminationLevelType;

        private PropertyInfo pipeNetProp;

        private PropertyInfo modeProp;

        private PropertyInfo waterStorageNetProp;

        private FieldInfo waterTowersField;

        private FieldInfo towerWaterField;

        private PropertyInfo towerSpaceProp;

        private MethodInfo pushWaterMethod;

        private MethodInfo pullWaterMethod;

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
            float stored = TowerWater(net);
            float importNeed = SumTowerSpace(GetTowers(net));
            return stored - importNeed;
        }

        public override float NetSupply(object net, float balance)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            return TowerWater(net);
        }

        public override float NetStorageRoom(object net)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            return SumTowerSpace(GetTowers(net));
        }

        public override float PushIntoNet(object net, float amount)
        {
            if (net == null || !TryBind() || amount <= 0f)
            {
                return amount;
            }
            object leftover = pushWaterMethod.Invoke(net, new object[] { amount });
            return leftover is float f ? Mathf.Max(0f, f) : 0f;
        }

        public override void DriveTie(object topNet, object bottomNet, CompShaftFluidTie topTie = null, CompShaftFluidTie bottomTie = null)
        {
            if (!TryBind() || topNet == null || bottomNet == null || ReferenceEquals(topNet, bottomNet))
            {
                return;
            }

            FlushBuffer(topTie, topNet);
            FlushBuffer(bottomTie, bottomNet);
            EqualizeNets(topNet, bottomNet);
        }

        public override bool Transfer(object fromNet, object toNet, float amount, CompShaftFluidTie fromTie = null, CompShaftFluidTie toTie = null)
        {
            if (!TryBind() || fromNet == null || toNet == null || amount <= 0f)
            {
                return false;
            }
            amount = Mathf.Min(amount, MaxTransferPerPulse(fromNet));
            if (!TryPullWater(fromNet, amount))
            {
                return false;
            }
            float leftover = PushIntoNet(toNet, amount);
            if (leftover > 0.001f)
            {
                // Prefer returning to the source net (AASB); park only what source cannot take.
                float still = PushIntoNet(fromNet, leftover);
                ParkLeftover(toTie, still);
            }
            return true;
        }

        private void EqualizeNets(object netA, object netB)
        {
            float capA = TowerCapacity(netA);
            float capB = TowerCapacity(netB);
            if (capA <= 0f || capB <= 0f)
            {
                return;
            }

            float waterA = TowerWater(netA);
            float waterB = TowerWater(netB);
            // AASB: 0.5 * (WA*CB - WB*CA) / (CA+CB)
            float transfer = EqualizeDamping * (waterA * capB - waterB * capA) / (capA + capB);
            object rich;
            object poor;
            float amount;
            if (transfer > MinTransfer)
            {
                rich = netA;
                poor = netB;
                amount = transfer;
            }
            else if (transfer < -MinTransfer)
            {
                rich = netB;
                poor = netA;
                amount = -transfer;
            }
            else
            {
                return;
            }

            amount = Mathf.Min(amount, MaxTransferPerPulse(rich));
            if (!TryPullWater(rich, amount))
            {
                return;
            }
            float leftover = PushIntoNet(poor, amount);
            if (leftover > 0.001f)
            {
                PushIntoNet(rich, leftover);
            }
        }

        private void FlushBuffer(CompShaftFluidTie tie, object net)
        {
            if (tie == null || net == null || tie.ShaftResourceBuffer < 0.001f)
            {
                return;
            }
            float take = tie.ShaftResourceBuffer;
            float leftover = PushIntoNet(net, take);
            float accepted = take - leftover;
            if (accepted > 0.001f)
            {
                tie.TakeShaftResourceBuffer(accepted);
            }
        }

        private bool TryPullWater(object net, float amount)
        {
            if (amount <= 0f || pullWaterMethod == null)
            {
                return false;
            }
            object contam = contaminationLevelType != null
                ? Activator.CreateInstance(contaminationLevelType)
                : null;
            object[] args = new object[] { amount, contam };
            object result = pullWaterMethod.Invoke(net, args);
            return result is bool ok && ok;
        }

        private float TowerWater(object net)
        {
            IList towers = GetTowers(net);
            if (towers == null)
            {
                return 0f;
            }
            float sum = 0f;
            for (int i = 0; i < towers.Count; i++)
            {
                object tower = towers[i];
                if (tower != null)
                {
                    sum += (float)towerWaterField.GetValue(tower);
                }
            }
            return sum;
        }

        private float TowerCapacity(object net)
        {
            IList towers = GetTowers(net);
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
                sum += (float)towerWaterField.GetValue(tower);
                sum += (float)towerSpaceProp.GetValue(tower, null);
            }
            return sum;
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
            contaminationLevelType = ReflectionUtil.TypeIn("DubsBadHygiene.ContaminationLevel", asm);
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
            pullWaterMethod = plumbingNetType.GetMethod("PullWater", BindingFlags.Instance | BindingFlags.Public);

            if (pipeNetProp == null || modeProp == null || waterStorageNetProp == null
                || waterTowersField == null || towerWaterField == null || towerSpaceProp == null
                || pushWaterMethod == null || pullWaterMethod == null)
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

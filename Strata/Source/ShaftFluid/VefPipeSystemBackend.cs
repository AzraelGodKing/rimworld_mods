using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Vanilla Expanded Framework PipeSystem (Helixien gas and other VEF pipe nets).
    // Same demand-driven shape as CompPowerShaft / DBH shaft junctions:
    //   - Junction CompResourceStorage = mini-tank (like DBH CompWaterStorage / power batteries)
    //   - Want = online deficit + offline demand + storage equalize/charge
    //   - Supply = live production surplus + drawable storage
    //   - ExtraProduction/ExtraConsumption = CompPowerShaft.PowerOutput (applied in PipeSystemTick Prefix)
    //   - BootstrapReceivers = CompPowerShaft.BootstrapConsumers
    public sealed class VefPipeSystemBackend : ShaftFluidBackend
    {
        private const float EqualizeDamping = 0.5f;

        private const float EqualizeMinTransfer = 0.5f;

        private const float FeedMin = 0.01f;

        private static readonly Dictionary<object, object> pairedNets = new Dictionary<object, object>();

        private readonly string pipeNetDefName;

        private Type compResourceType;

        private Type pipeNetManagerType;

        private Type pipeNetType;

        private Type pipeNetDefType;

        private Type compResourceTraderType;

        private PropertyInfo pipeNetProp;

        private PropertyInfo storedProp;

        private PropertyInfo consumptionProp;

        private PropertyInfo productionProp;

        private PropertyInfo availableCapacityProp;

        private PropertyInfo overflowProp;

        private PropertyInfo refillableAmountProp;

        private PropertyInfo extraProductionProp;

        private PropertyInfo extraConsumptionProp;

        private PropertyInfo traderResourceOnProp;

        private PropertyInfo traderConsumptionProp;

        private MethodInfo traderCanBeOnMethod;

        private FieldInfo receiversField;

        private FieldInfo receiversDirtyField;

        private MethodInfo getPipeNetAtMethod;

        private MethodInfo currentStoredMethod;

        private MethodInfo drawAmongStorageMethod;

        private MethodInfo distributeAmongStorageMethod;

        private MethodInfo drawFromOverflowMethod;

        private MethodInfo distributeToOverflowMethod;

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
            ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core")
            && TryBind()
            && GetNetDef() != null;

        public override object GetNetFromJunction(Thing junction)
        {
            if (!TryBind() || junction?.Map == null)
            {
                return null;
            }

            // Prefer the junction's own CompResource net (same as CompPowerShaft.PowerNet) —
            // GetPipeNetAt can miss before networkGrid marks the cell.
            if (junction is ThingWithComps twc)
            {
                object own = GetNetFromThing(twc);
                if (own != null)
                {
                    return own;
                }
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
            // Online balance only — offline demand is added in DriveTie Want (power-shaft style).
            return OnBalance(net);
        }

        public override float NetSupply(object net, float balance)
        {
            return Supply(net, balance);
        }

        public override float NetStorageRoom(object net)
        {
            if (net == null || !TryBind())
            {
                return 0f;
            }
            float room = (float)availableCapacityProp.GetValue(net, null);
            float refill = (float)refillableAmountProp.GetValue(net, null);
            return Mathf.Max(0f, room + refill);
        }

        public override float PushIntoNet(object net, float amount)
        {
            if (!TryBind() || net == null || amount <= 0f)
            {
                return amount;
            }
            float stored = InvokeDistribute(net, amount);
            float leftover = Mathf.Max(0f, amount - stored);
            if (leftover > 0.001f && distributeToOverflowMethod != null)
            {
                object overflowed = distributeToOverflowMethod.Invoke(net, new object[] { leftover });
                if (overflowed is float accepted)
                {
                    leftover = Mathf.Max(0f, leftover - accepted);
                }
                else
                {
                    leftover = 0f;
                }
            }
            return leftover;
        }

        public override void DriveTie(object topNet, object bottomNet, CompShaftFluidTie topTie = null, CompShaftFluidTie bottomTie = null)
        {
            if (!TryBind() || topNet == null || bottomNet == null || ReferenceEquals(topNet, bottomNet))
            {
                return;
            }

            // Register pair only — DirectFeed + equalize run on PipeSystemTick (Prefix/Postfix)
            // so they see gas after production is deposited into tanks/overflow.
            RegisterPair(topNet, bottomNet);
            FlushBuffer(topTie, topNet);
            FlushBuffer(bottomTie, bottomNet);
        }

        // PipeSystemTick Prefix — Extra* visible to TotalProduction this tick.
        internal void InjectDirectFeedBeforeTick(object net)
        {
            if (!TryBind() || net == null || extraProductionProp == null || extraConsumptionProp == null)
            {
                return;
            }
            if (!NetMatchesChannel(net) || !pairedNets.TryGetValue(net, out object partner) || partner == null)
            {
                return;
            }
            DirectFeed(partner, net);
        }

        // PipeSystemTick Postfix — move stored/overflow after this tick's deposit.
        internal void EqualizeAfterPipeSystemTick(object net)
        {
            if (!TryBind() || net == null || !NetMatchesChannel(net))
            {
                return;
            }
            if (!pairedNets.TryGetValue(net, out object partner) || partner == null)
            {
                return;
            }
            // Only run once per pair per tick (lower NetID / hash wins).
            if (net.GetHashCode() > partner.GetHashCode())
            {
                return;
            }
            EqualizeStorages(net, partner);
            SyncStored(net);
            SyncStored(partner);
            BootstrapReceivers(net, DrawableAmount(net) + LiveExcess(OnBalance(net)));
            BootstrapReceivers(partner, DrawableAmount(partner) + LiveExcess(OnBalance(partner)));
        }

        private bool NetMatchesChannel(object net)
        {
            object def = ReflectionUtil.GetMember(net, "def");
            return PipeNetDefMatches(def, pipeNetDefName);
        }

        // AASB DirectFeed + storage-charge so surplus production fills the other floor's tanks
        // even when no consumers are online yet.
        private void DirectFeed(object fromNet, object toNet)
        {
            float offlineDemand = SumOfflineDemand(toNet);
            float onlineDeficit = Mathf.Max(0f,
                (float)consumptionProp.GetValue(toNet, null)
                - (float)productionProp.GetValue(toNet, null)
                - (float)extraProductionProp.GetValue(toNet, null));
            float storageWant = Mathf.Min(NetStorageRoom(toNet), MaxTransferPerPulse(toNet));
            float need = onlineDeficit + offlineDemand + storageWant;
            if (need <= FeedMin)
            {
                return;
            }

            float surplus = Mathf.Max(0f,
                (float)productionProp.GetValue(fromNet, null)
                - (float)consumptionProp.GetValue(fromNet, null)
                - (float)extraConsumptionProp.GetValue(fromNet, null));
            float available = surplus + CurrentStored(fromNet) + OverflowOf(fromNet);
            float feed = Mathf.Min(need, available, MaxTransferPerPulse(fromNet));
            if (feed <= FeedMin)
            {
                return;
            }

            extraConsumptionProp.SetValue(fromNet,
                (float)extraConsumptionProp.GetValue(fromNet, null) + feed, null);
            extraProductionProp.SetValue(toNet,
                (float)extraProductionProp.GetValue(toNet, null) + feed, null);
        }

        // AASB EqualizeStorages — 0.5 fill damping via Draw/DistributeAmongStorage.
        private void EqualizeStorages(object netA, object netB)
        {
            float storedA = CurrentStored(netA);
            float storedB = CurrentStored(netB);
            float capA = storedA + (float)availableCapacityProp.GetValue(netA, null);
            float capB = storedB + (float)availableCapacityProp.GetValue(netB, null);
            if (capA <= FeedMin || capB <= FeedMin)
            {
                // One side has no tanks — push drawable into the side that has room.
                float roomA = NetStorageRoom(netA);
                float roomB = NetStorageRoom(netB);
                if (roomB > FeedMin && DrawableAmount(netA) > FeedMin)
                {
                    Transfer(netA, netB, Mathf.Min(MaxTransferPerPulse(netA), DrawableAmount(netA), roomB));
                }
                else if (roomA > FeedMin && DrawableAmount(netB) > FeedMin)
                {
                    Transfer(netB, netA, Mathf.Min(MaxTransferPerPulse(netB), DrawableAmount(netB), roomA));
                }
                return;
            }

            float transfer = EqualizeDamping * (storedA * capB - storedB * capA) / (capA + capB);
            object rich;
            object poor;
            float amount;
            if (transfer > EqualizeMinTransfer)
            {
                rich = netA;
                poor = netB;
                amount = transfer;
            }
            else if (transfer < -EqualizeMinTransfer)
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
            float drawn = InvokeDraw(rich, amount, drawFromOverflow: true);
            if (drawn <= FeedMin)
            {
                return;
            }
            float distributed = InvokeDistribute(poor, drawn);
            float leftover = Mathf.Max(0f, drawn - distributed);
            if (leftover > FeedMin)
            {
                InvokeDistribute(rich, leftover);
            }
        }

        public override bool Transfer(object fromNet, object toNet, float amount, CompShaftFluidTie fromTie = null, CompShaftFluidTie toTie = null)
        {
            if (!TryBind() || fromNet == null || toNet == null || amount <= 0f)
            {
                return false;
            }
            amount = Mathf.Min(amount, MaxTransferPerPulse(fromNet));
            // Draw from tanks; fall back to overflow (producers dump here when tanks are full/absent).
            float moved = InvokeDraw(fromNet, amount, drawFromOverflow: false);
            if (moved <= 0.001f)
            {
                moved = InvokeDraw(fromNet, amount, drawFromOverflow: true);
            }
            if (moved <= 0.001f && drawFromOverflowMethod != null)
            {
                object fromOverflow = drawFromOverflowMethod.Invoke(fromNet, new object[] { amount });
                if (fromOverflow is float f)
                {
                    moved = f;
                }
            }
            if (moved <= 0.001f)
            {
                return false;
            }
            float leftover = PushIntoNet(toNet, moved);
            if (leftover > 0.001f)
            {
                float still = PushIntoNet(fromNet, leftover);
                ParkLeftover(toTie, still);
            }
            return true;
        }

        private float OnBalance(object net)
        {
            return (float)productionProp.GetValue(net, null) - (float)consumptionProp.GetValue(net, null);
        }

        private static float LiveExcess(float balance)
        {
            return Mathf.Max(0f, balance);
        }

        private float Supply(object net, float onBalance)
        {
            float spare = LiveExcess(onBalance);
            if (spare > FeedMin)
            {
                return spare + DrawableAmount(net);
            }
            return DrawableAmount(net);
        }

        private float OfflineDemand(object net)
        {
            return SumOfflineDemand(net);
        }

        // Partner has more drawable gas — trickle toward equal fill (incl. overflow).
        private float EqualizeWant(object mine, object other)
        {
            float storedMine = DrawableAmount(mine);
            float storedOther = DrawableAmount(other);
            float capMine = storedMine + (float)availableCapacityProp.GetValue(mine, null);
            float capOther = storedOther + (float)availableCapacityProp.GetValue(other, null);
            // Overflow-only nets have AvailableCapacity 0; still allow export into partner room.
            if (capOther <= 0f && storedOther <= FeedMin)
            {
                return 0f;
            }
            if (capMine <= FeedMin)
            {
                // No local capacity info — ask for a supply-sized charge instead.
                return 0f;
            }
            float transfer = EqualizeDamping * (storedOther * capMine - storedMine * capOther) / Mathf.Max(FeedMin, capMine + capOther);
            return transfer > EqualizeMinTransfer ? transfer : 0f;
        }

        // Partner has live surplus and this net has tank/junction room — charge it.
        private float StorageChargeWant(object mine, float partnerLiveExcess)
        {
            if (partnerLiveExcess < FeedMin)
            {
                return 0f;
            }
            float room = NetStorageRoom(mine);
            if (room < FeedMin)
            {
                return 0f;
            }
            return Mathf.Min(partnerLiveExcess, room);
        }

        private float SumOfflineDemand(object net)
        {
            if (StrataMod.Settings?.performanceModeEnabled == true)
            {
                return 0f;
            }
            float offlineDemand = 0f;
            if (receiversField == null || receiversField.GetValue(net) is not IList receivers)
            {
                return 0f;
            }
            for (int i = 0; i < receivers.Count; i++)
            {
                object trader = receivers[i];
                if (trader == null || IsResourceOn(trader) || !CanBeOn(trader))
                {
                    continue;
                }
                offlineDemand += TraderConsumption(trader);
            }
            return offlineDemand;
        }

        private void BootstrapReceivers(object net, float available)
        {
            if (receiversField == null || traderResourceOnProp == null || available <= FeedMin
                || StrataMod.Settings?.performanceModeEnabled == true)
            {
                return;
            }
            if (receiversField.GetValue(net) is not IList receivers)
            {
                return;
            }

            float remaining = available;
            bool any = false;
            for (int i = 0; i < receivers.Count; i++)
            {
                object trader = receivers[i];
                if (trader == null || IsResourceOn(trader) || !CanBeOn(trader))
                {
                    continue;
                }
                float want = TraderConsumption(trader);
                if (want <= 0f)
                {
                    // Producers / zero-draw traders: just flip on.
                    traderResourceOnProp.SetValue(trader, true, null);
                    any = true;
                    continue;
                }
                if (remaining + 0.0001f < want)
                {
                    continue;
                }
                traderResourceOnProp.SetValue(trader, true, null);
                consumptionProp.SetValue(net, (float)consumptionProp.GetValue(net, null) + want, null);
                remaining -= want;
                any = true;
                if (remaining < FeedMin)
                {
                    break;
                }
            }
            if (any && receiversDirtyField != null)
            {
                receiversDirtyField.SetValue(net, true);
            }
        }

        private void SyncStored(object net)
        {
            if (storedProp == null || !storedProp.CanWrite)
            {
                return;
            }
            storedProp.SetValue(net, CurrentStored(net), null);
        }

        private static void RegisterPair(object a, object b)
        {
            pairedNets[a] = b;
            pairedNets[b] = a;
        }

        private bool IsResourceOn(object trader)
        {
            return traderResourceOnProp != null && (bool)traderResourceOnProp.GetValue(trader, null);
        }

        private bool CanBeOn(object trader)
        {
            if (traderCanBeOnMethod == null)
            {
                return true;
            }
            object can = traderCanBeOnMethod.Invoke(trader, null);
            return can is bool ok && ok;
        }

        private float TraderConsumption(object trader)
        {
            if (traderConsumptionProp == null)
            {
                return 0f;
            }
            return (float)traderConsumptionProp.GetValue(trader, null);
        }

        private float OverflowOf(object net)
        {
            return overflowProp != null ? (float)overflowProp.GetValue(net, null) : 0f;
        }

        private float CurrentStored(object net)
        {
            if (currentStoredMethod != null)
            {
                object value = currentStoredMethod.Invoke(net, null);
                if (value is float f)
                {
                    return f;
                }
            }
            return (float)storedProp.GetValue(net, null);
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

        private float InvokeDraw(object net, float amount, bool drawFromOverflow = false)
        {
            ParameterInfo[] parms = drawAmongStorageMethod.GetParameters();
            object[] args;
            if (parms.Length >= 4)
            {
                args = new object[] { amount, 0f, null, drawFromOverflow };
            }
            else if (parms.Length == 3 && parms[1].ParameterType.IsByRef)
            {
                args = new object[] { amount, 0f, null };
            }
            else if (parms.Length == 2 && parms[1].ParameterType.IsByRef)
            {
                args = new object[] { amount, 0f };
            }
            else
            {
                drawAmongStorageMethod.Invoke(net, new object[] { amount, null });
                return Mathf.Min(amount, DrawableAmount(net));
            }
            drawAmongStorageMethod.Invoke(net, args);
            return args[1] is float drawn ? drawn : 0f;
        }

        private float InvokeDistribute(object net, float amount)
        {
            ParameterInfo[] parms = distributeAmongStorageMethod.GetParameters();
            object[] args;
            if (parms.Length >= 4)
            {
                args = new object[] { amount, 0f, null, false };
            }
            else if (parms.Length == 2 && parms[1].ParameterType.IsByRef)
            {
                args = new object[] { amount, 0f };
            }
            else
            {
                distributeAmongStorageMethod.Invoke(net, new object[] { amount });
                return amount;
            }
            distributeAmongStorageMethod.Invoke(net, args);
            return args[1] is float stored ? stored : 0f;
        }

        private float DrawableAmount(object net)
        {
            return Mathf.Max(0f, CurrentStored(net) + OverflowOf(net));
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
            // PipeSystem lives in PipeSystem.dll (shipped with VEF), not inside VEF.dll.
            Assembly pipeSystem = ReflectionUtil.FindAssembly("PipeSystem")
                ?? ReflectionUtil.FindAssembly("VEF");
            compResourceType = ReflectionUtil.TypeIn("PipeSystem.CompResource", pipeSystem);
            pipeNetManagerType = ReflectionUtil.TypeIn("PipeSystem.PipeNetManager", pipeSystem);
            pipeNetType = ReflectionUtil.TypeIn("PipeSystem.PipeNet", pipeSystem);
            pipeNetDefType = ReflectionUtil.TypeIn("PipeSystem.PipeNetDef", pipeSystem);
            compResourceTraderType = ReflectionUtil.TypeIn("PipeSystem.CompResourceTrader", pipeSystem);
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
            extraProductionProp = pipeNetType.GetProperty("ExtraProductionThisTick", BindingFlags.Instance | BindingFlags.Public);
            extraConsumptionProp = pipeNetType.GetProperty("ExtraConsumptionThisTick", BindingFlags.Instance | BindingFlags.Public);
            receiversField = pipeNetType.GetField("receivers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            receiversDirtyField = pipeNetType.GetField("receiversDirty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            getPipeNetAtMethod = pipeNetManagerType.GetMethod("GetPipeNetAt", BindingFlags.Instance | BindingFlags.Public);
            currentStoredMethod = pipeNetType.GetMethod("CurrentStored", BindingFlags.Instance | BindingFlags.Public);
            drawAmongStorageMethod = FindBestOutFloatMethod("DrawAmongStorage");
            distributeAmongStorageMethod = FindBestOutFloatMethod("DistributeAmongStorage");
            drawFromOverflowMethod = pipeNetType.GetMethod("DrawFromOverflow", BindingFlags.Instance | BindingFlags.Public);
            distributeToOverflowMethod = FindDistributeToOverflow();

            if (compResourceTraderType != null)
            {
                traderResourceOnProp = compResourceTraderType.GetProperty("ResourceOn", BindingFlags.Instance | BindingFlags.Public);
                traderConsumptionProp = compResourceTraderType.GetProperty("Consumption", BindingFlags.Instance | BindingFlags.Public);
                traderCanBeOnMethod = AccessTools.Method(compResourceTraderType, "CanBeOn");
            }

            if (pipeNetProp == null || storedProp == null || overflowProp == null
                || availableCapacityProp == null || refillableAmountProp == null
                || getPipeNetAtMethod == null
                || drawAmongStorageMethod == null || distributeAmongStorageMethod == null)
            {
                LogBindOnce("PipeSystem reflection bind incomplete.");
                compResourceType = null;
                return false;
            }

            // Soft-require DirectFeed pieces; equalize still works without them.
            if (extraProductionProp == null || extraConsumptionProp == null || receiversField == null)
            {
                LogBindOnce("PipeSystem DirectFeed bind partial — storage equalize only.");
            }
            return true;
        }

        private MethodInfo FindBestOutFloatMethod(string name)
        {
            MethodInfo[] methods = pipeNetType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            MethodInfo best = null;
            int bestScore = -1;
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != name)
                {
                    continue;
                }
                ParameterInfo[] parms = method.GetParameters();
                if (parms.Length < 2 || !parms[1].ParameterType.IsByRef)
                {
                    continue;
                }
                // Prefer overloads that can target overflow (4 params) then plain out-float.
                int score = parms.Length;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = method;
                }
            }
            return best;
        }

        private MethodInfo FindDistributeToOverflow()
        {
            MethodInfo[] methods = pipeNetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "DistributeToOverflow" || method.ReturnType != typeof(float))
                {
                    continue;
                }
                ParameterInfo[] parms = method.GetParameters();
                if (parms.Length == 1 && parms[0].ParameterType == typeof(float))
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

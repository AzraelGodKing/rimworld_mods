using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // One end of a power tie that runs through a shaft. It's an ordinary power
    // trader on its own level's grid; the controller end (top) drives both this
    // node and its partner each tick to move energy across the shaft.
    //
    // Demand-driven: a grid asks for its running deficit plus offline appliances
    // that want power (vanilla CurrentEnergyGainRate ignores PowerOn==false),
    // plus a battery-equalization trickle. The other side gives live surplus
    // plus battery discharge so linked floors don't brown out. Players can cap
    // bidirectional watts on the shaft.
    public class CompPowerShaft : CompPowerTrader
    {
        // Watts moved per unit of stored-energy (Wd) imbalance between the grids.
        private const float TransferGain = 4f;

        private const float LimitStep = 100f;

        private const float LimitStepShift = 500f;

        private const float LimitFromUnlimitedDefault = 1000f;

        // -1 = no cap (demand-driven). 0 = block transfer. Synced to partner.
        private float maxTransferWatts = -1f;

        // Last DriveTie transfer (+ = feeding the bottom net).
        private float lastTransferWatts;

        private float BaseLoad
        {
            get
            {
                if (parent is Building_ShaftConduit conduit && conduit.IsAutoSpawned)
                {
                    return 0f;
                }
                if (parent.TryGetComp<CompShaftFluidJunctionLink>()?.IsAutoSpawned == true)
                {
                    return 0f;
                }
                return Props.PowerConsumption;
            }
        }

        public float LastTransferWatts => lastTransferWatts;

        // -1 means unlimited.
        public float MaxTransferWatts => maxTransferWatts;

        public bool HasTransferLimit => maxTransferWatts >= 0f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref maxTransferWatts, "strataMaxTransferWatts", -1f);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (PowerNet != null && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown())
            {
                PowerOn = true;
            }
        }

        public override void PostDraw()
        {
            if (ShouldSuppressNeedsPowerOverlay())
            {
                CompFlickable flick = parent.GetComp<CompFlickable>();
                if (flick != null && !flick.SwitchIsOn && !parent.IsBrokenDown())
                {
                    parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.PowerOff);
                }
                return;
            }
            base.PostDraw();
        }

        private bool ShouldSuppressNeedsPowerOverlay()
        {
            return Props.transmitsPower && PowerNet != null
                && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown();
        }

        public void DriveTie(CompPowerShaft bottom)
        {
            PowerNet topNet = PowerNet;
            PowerNet botNet = bottom.PowerNet;
            if (topNet == null || botNet == null || topNet == botNet
                || !FlickUtility.WantsToBeOn(parent) || parent.IsBrokenDown()
                || !FlickUtility.WantsToBeOn(bottom.parent) || bottom.parent.IsBrokenDown())
            {
                Idle(bottom);
                return;
            }

            PowerOn = true;
            bottom.PowerOn = true;

            // ON balance = true live surplus/deficit. Offline demand is only for
            // Want — otherwise dark local appliances "reserve" generator watts and
            // power never flows upward to the other floor.
            float topOn = OnBalanceWatts(topNet, this);
            float botOn = OnBalanceWatts(botNet, bottom);
            float topOffDemand = OfflineDemandWatts(topNet, this);
            float botOffDemand = OfflineDemandWatts(botNet, bottom);

            // Include the receiving shaft's base load in each side's demand so
            // the transfer covers both the far-side consumers AND the receiving
            // shaft's own maintenance draw. Without this the bottom net runs a
            // permanent deficit equal to bottom.BaseLoad (one small device stays
            // dark even when the top has ample surplus). Symmetric for upward flow.
            float botWant = bottom.BaseLoad + Mathf.Max(0f, -botOn) + botOffDemand
                + EqualizeWant(botNet, topNet)
                + BatteryChargeWant(botNet, LiveExcess(topOn));
            float topWant = BaseLoad + Mathf.Max(0f, -topOn) + topOffDemand
                + EqualizeWant(topNet, botNet)
                + BatteryChargeWant(topNet, LiveExcess(botOn));

            // Live excess first, then battery discharge so linked floors don't
            // brown out when generators dip (night / spikes).
            float down = Mathf.Min(botWant, Supply(topNet, topOn));
            float up = Mathf.Min(topWant, Supply(botNet, botOn));
            float transfer = down - up; // + = down

            float cap = EffectiveCapWatts(bottom);
            if (cap < float.MaxValue)
            {
                transfer = Mathf.Clamp(transfer, -cap, cap);
            }

            lastTransferWatts = transfer;
            bottom.lastTransferWatts = -transfer;

            PowerOutput = -BaseLoad - Mathf.Max(0f, transfer) + Mathf.Max(0f, -transfer);
            bottom.PowerOutput = -bottom.BaseLoad + Mathf.Max(0f, transfer) - Mathf.Max(0f, -transfer);

            if (transfer > 1f)
            {
                BootstrapConsumers(botNet, transfer);
            }
            else if (transfer < -1f)
            {
                BootstrapConsumers(topNet, -transfer);
            }
            else
            {
                // Startup / balanced-grid probe: transfer is near-zero but live surplus
                // exists (fresh connection, or CurrentEnergyGainRate lagged one tick).
                // Boot underground consumers now so they don't wait an extra cycle for
                // botWant to register through the demand scan.
                float topLive = Mathf.Max(0f, topOn) - BaseLoad;
                float botLive = Mathf.Max(0f, botOn) - bottom.BaseLoad;
                if (topLive > 1f)
                {
                    BootstrapConsumers(botNet, topLive);
                }
                else if (botLive > 1f)
                {
                    BootstrapConsumers(topNet, botLive);
                }
            }
        }

        // Player watt cap only (-1 = unlimited). Actual flow is still limited by
        // Supply() (live excess + batteries).
        private float EffectiveCapWatts(CompPowerShaft bottom)
        {
            float a = maxTransferWatts < 0f ? float.MaxValue : maxTransferWatts;
            float b = bottom.maxTransferWatts < 0f ? float.MaxValue : bottom.maxTransferWatts;
            return Mathf.Min(a, b);
        }

        private static float LiveExcess(float balance)
        {
            return Mathf.Max(0f, balance);
        }

        private void Idle(CompPowerShaft bottom)
        {
            lastTransferWatts = 0f;
            bottom.lastTransferWatts = 0f;
            PowerOutput = -BaseLoad;
            bottom.PowerOutput = -bottom.BaseLoad;
        }

        // Live surplus, then battery discharge so linked floors don't brown out.
        private static float Supply(PowerNet net, float balance)
        {
            float spare = LiveExcess(balance);
            if (spare > 0f)
            {
                return spare;
            }
            if (net.CurrentStoredEnergy() <= 1f)
            {
                return 0f;
            }
            return BatteryDischargeWatts(net);
        }

        private static float BatteryDischargeWatts(PowerNet net)
        {
            return net.CurrentStoredEnergy() * TransferGain;
        }

        private static float EqualizeWant(PowerNet mine, PowerNet other)
        {
            float myStored = mine.CurrentStoredEnergy();
            float otherStored = other.CurrentStoredEnergy();
            if (otherStored <= myStored || !AnyBatteryFreeSpace(mine))
            {
                return 0f;
            }
            return (otherStored - myStored) * TransferGain;
        }

        // Partner has live generator surplus and this net has battery room —
        // pull that excess across so linked batteries actually charge.
        private static float BatteryChargeWant(PowerNet mine, float partnerLiveExcess)
        {
            if (partnerLiveExcess < 1f || !AnyBatteryFreeSpace(mine))
            {
                return 0f;
            }
            return Mathf.Min(partnerLiveExcess, BatteryAcceptWatts(mine));
        }

        private static float BatteryAcceptWatts(PowerNet net)
        {
            float accept = 0f;
            List<CompPowerBattery> batteries = net.batteryComps;
            for (int i = 0; i < batteries.Count; i++)
            {
                accept += batteries[i].AmountCanAccept;
            }
            return accept * TransferGain;
        }

        private static bool AnyBatteryFreeSpace(PowerNet net)
        {
            List<CompPowerBattery> batteries = net.batteryComps;
            for (int i = 0; i < batteries.Count; i++)
            {
                if (batteries[i].AmountCanAccept > 1f)
                {
                    return true;
                }
            }
            return false;
        }

        // PowerOn traders only — true live surplus available to export.
        // Prefer PowerNet's cached gain rate (O(1)) instead of scanning every trader.
        private static float OnBalanceWatts(PowerNet net, CompPowerShaft self)
        {
            float watts = net.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick;
            if (self != null && self.PowerOn)
            {
                watts -= self.PowerOutput;
            }
            return watts;
        }

        // Offline appliances that still want power (bootstrap demand).
        private static float OfflineDemandWatts(PowerNet net, CompPowerShaft self)
        {
            // Performance mode skips the O(n) scan — except tankless nets (no
            // batteries): those need host surplus demand registered so A4
            // "one net" wake still works without local storage.
            if (StrataMod.Settings?.performanceModeEnabled == true && NetHasBatteries(net))
            {
                return 0f;
            }
            float watts = 0f;
            List<CompPowerTrader> comps = net.powerComps;
            for (int i = 0; i < comps.Count; i++)
            {
                CompPowerTrader c = comps[i];
                if (c == null || ReferenceEquals(c, self) || c.PowerOn)
                {
                    continue;
                }
                if (!FlickUtility.WantsToBeOn(c.parent) || c.parent.IsBrokenDown())
                {
                    continue;
                }
                float consumption = c.Props.PowerConsumption;
                if (consumption > 0f)
                {
                    watts += consumption;
                }
            }
            return watts;
        }

        private static void BootstrapConsumers(PowerNet net, float availableWatts)
        {
            if (availableWatts < 1f || net?.powerComps == null)
            {
                return;
            }
            // Perf mode may skip bootstrap on battery nets; tankless pockets
            // must still wake from shaft surplus (A4).
            if (StrataMod.Settings?.performanceModeEnabled == true && NetHasBatteries(net))
            {
                return;
            }
            float remaining = availableWatts;
            List<CompPowerTrader> comps = net.powerComps;
            for (int i = 0; i < comps.Count; i++)
            {
                CompPowerTrader c = comps[i];
                if (c == null || c.PowerOn || c is CompPowerShaft
                    || !FlickUtility.WantsToBeOn(c.parent) || c.parent.IsBrokenDown())
                {
                    continue;
                }
                float need = c.Props.PowerConsumption;
                if (need <= 0f)
                {
                    continue;
                }
                if (remaining + 0.01f < need)
                {
                    continue;
                }
                c.PowerOn = true;
                remaining -= need;
                if (remaining < 1f)
                {
                    break;
                }
            }
        }

        private static bool NetHasBatteries(PowerNet net)
        {
            return net?.batteryComps != null && net.batteryComps.Count > 0;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (parent is Building_AncientColonyStairsDown || parent is Building_AncientColonyStairsUp)
            {
                yield break;
            }
            if (parent is Building_ShaftConduit conduit && conduit.IsAutoSpawned)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "Strata_PowerShaft_DecreaseLimit".Translate(),
                defaultDesc = "Strata_PowerShaft_DecreaseLimitDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", reportFailure: false),
                action = DecreaseTransferLimit,
            };
            yield return new Command_Action
            {
                defaultLabel = "Strata_PowerShaft_IncreaseLimit".Translate(),
                defaultDesc = "Strata_PowerShaft_IncreaseLimitDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", reportFailure: false),
                action = IncreaseTransferLimit,
            };
            yield return new Command_Action
            {
                defaultLabel = "Strata_PowerShaft_Unlimited".Translate(),
                defaultDesc = "Strata_PowerShaft_UnlimitedDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect", reportFailure: false),
                action = () => SetTransferLimit(-1f),
                Disabled = !HasTransferLimit,
                disabledReason = "Strata_PowerShaft_AlreadyUnlimited".Translate(),
            };
        }

        private static float LimitStepNow()
        {
            return (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                ? LimitStepShift
                : LimitStep;
        }

        private void DecreaseTransferLimit()
        {
            float step = LimitStepNow();
            float available = LinkedAvailableSupplyWatts();
            if (maxTransferWatts < 0f)
            {
                float start = available >= 1f ? available : LimitFromUnlimitedDefault;
                SetTransferLimit(start);
                return;
            }
            SetTransferLimit(Mathf.Max(0f, maxTransferWatts - step));
        }

        private void IncreaseTransferLimit()
        {
            float step = LimitStepNow();
            float available = LinkedAvailableSupplyWatts();
            if (maxTransferWatts < 0f)
            {
                return;
            }
            float next = maxTransferWatts + step;
            if (available >= 1f)
            {
                next = Mathf.Min(next, available);
            }
            SetTransferLimit(next);
        }

        public void SetTransferLimit(float watts)
        {
            if (watts < 0f)
            {
                maxTransferWatts = -1f;
            }
            else
            {
                float available = LinkedAvailableSupplyWatts();
                maxTransferWatts = available >= 1f
                    ? Mathf.Clamp(watts, 0f, available)
                    : Mathf.Max(0f, watts);
            }
            CompPowerShaft partner = FindPartner();
            if (partner != null && !Mathf.Approximately(partner.maxTransferWatts, maxTransferWatts))
            {
                partner.maxTransferWatts = maxTransferWatts;
            }
        }

        // Live excess or battery discharge headroom on either end of the shaft.
        private float LinkedAvailableSupplyWatts()
        {
            float local = AvailableSupplyWatts(PowerNet, this);
            CompPowerShaft partner = FindPartner();
            if (partner == null)
            {
                return local;
            }
            return Mathf.Max(local, AvailableSupplyWatts(partner.PowerNet, partner));
        }

        private static float AvailableSupplyWatts(PowerNet net, CompPowerShaft self)
        {
            if (net == null || self == null)
            {
                return 0f;
            }
            return Supply(net, OnBalanceWatts(net, self));
        }

        private CompPowerShaft FindPartner()
        {
            if (parent is Building_StairsDown down && down.exit != null && !down.exit.Destroyed)
            {
                return down.exit.TryGetComp<CompPowerShaft>();
            }
            if (parent is Building_StairsUp up)
            {
                if (up.entrance != null && !up.entrance.Destroyed)
                {
                    return up.entrance.TryGetComp<CompPowerShaft>();
                }
                Building_StairsDown dig = up.DownEntrance;
                if (dig != null && !dig.Destroyed)
                {
                    return dig.TryGetComp<CompPowerShaft>();
                }
            }
            if (parent is Building_ShaftConduit conduit)
            {
                return conduit.LinkedPowerShaft();
            }
            return null;
        }

        public override string CompInspectStringExtra()
        {
            string baseText = base.CompInspectStringExtra();
            string limit = HasTransferLimit
                ? "Strata_PowerShaft_LimitCap".Translate(maxTransferWatts.ToString("F0"))
                : "Strata_PowerShaft_LimitUnlimited".Translate();
            string tip = limit;
            if (Mathf.Abs(lastTransferWatts) >= 1f)
            {
                string flow = lastTransferWatts > 0f
                    ? "Strata_PowerShaft_FeedingBelow".Translate(lastTransferWatts.ToString("F0"))
                    : "Strata_PowerShaft_DrawingBelow".Translate((-lastTransferWatts).ToString("F0"));
                tip = tip + "\n" + flow;
            }
            return baseText.NullOrEmpty() ? tip : baseText + "\n" + tip;
        }
    }
}

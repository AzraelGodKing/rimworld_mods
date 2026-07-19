using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    // Charged particles saturate the atmosphere: batteries bleed stored energy,
    // random EMP bursts stun electronics, and overloaded circuits pop extra
    // "Zzzt!" surges. Every Stormproof counter earns its keep at once - EMP
    // dampeners shield nearby batteries and buildings from the static bleed
    // and bursts, surge protectors eat the extra Zzzts, and storm capacitor
    // banks ride out the whole event untouched.
    public class GameCondition_IonStorm : GameCondition
    {
        private const int CheckIntervalTicks = 60;
        private const int LeakIntervalTicks = 150;
        private const float BatteryLeakPerDay = 0.20f;  // fraction of stored charge
        private const float EmpBurstsPerDay = 3f;
        private const float ExtraZzztsPerDay = 1.5f;
        private const float EmpBurstRadius = 2.9f;

        private static readonly SkyColorSet IonSkyColors = new SkyColorSet(
            new Color(0.76f, 0.72f, 0.95f),
            new Color(0.92f, 0.92f, 1.00f),
            new Color(0.66f, 0.62f, 0.88f),
            0.85f);

        private static readonly List<Building> empCandidates = new List<Building>();

        public override SkyTarget? SkyTarget(Map map)
        {
            return new SkyTarget(0.85f, IonSkyColors, 1f, 1f);
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();
            if (Find.TickManager.TicksGame % CheckIntervalTicks != 0)
            {
                return;
            }
            foreach (Map map in AffectedMaps)
            {
                if (Find.TickManager.TicksGame % LeakIntervalTicks < CheckIntervalTicks)
                {
                    LeakBatteries(map);
                }
                if (Rand.MTBEventOccurs(1f / EmpBurstsPerDay, GenDate.TicksPerDay, CheckIntervalTicks))
                {
                    EmpBurst(map);
                }
                if (Rand.MTBEventOccurs(1f / ExtraZzztsPerDay, GenDate.TicksPerDay, CheckIntervalTicks))
                {
                    TriggerZzzt(map);
                }
            }
        }

        private static bool Shielded(Thing t)
        {
            if (t?.Map == null)
            {
                return false;
            }

            foreach (CompEmpDampener dampener in StormproofRegistry.Dampeners)
            {
                if (dampener?.parent == null || !dampener.parent.Spawned
                    || dampener.parent.Map != t.Map)
                {
                    continue;
                }

                if (dampener.Covers(t))
                {
                    return true;
                }
            }

            return false;
        }

        // Static bleed: every charged battery on the map slowly leaks unless an
        // EMP dampener covers it. Storm capacitors aren't CompPowerBattery, so
        // they are naturally immune - as advertised.
        private void LeakBatteries(Map map)
        {
            float leakFraction = BatteryLeakPerDay * LeakIntervalTicks / GenDate.TicksPerDay;
            List<PowerNet> nets = map.powerNetManager.AllNetsListForReading;
            for (int i = 0; i < nets.Count; i++)
            {
                List<CompPowerBattery> batteries = nets[i].batteryComps;
                for (int j = 0; j < batteries.Count; j++)
                {
                    CompPowerBattery battery = batteries[j];
                    if (battery.StoredEnergy <= 0f || Shielded(battery.parent))
                    {
                        continue;
                    }
                    battery.DrawPower(Mathf.Min(battery.StoredEnergy,
                        battery.StoredEnergy * leakFraction));
                    if (Rand.Chance(0.10f))
                    {
                        FleckMaker.ThrowMicroSparks(battery.parent.DrawPos, map);
                    }
                }
            }
        }

        // A localized EMP pop at a random powered colony building. The existing
        // EMP dampener patch protects covered buildings from the stun.
        private void EmpBurst(Map map)
        {
            empCandidates.Clear();
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                CompPowerTrader power = b.TryGetComp<CompPowerTrader>();
                if (power != null && power.PowerOn)
                {
                    empCandidates.Add(b);
                }
            }
            if (empCandidates.Count == 0)
            {
                return;
            }
            Building target = empCandidates.RandomElement();
            GenExplosion.DoExplosion(
                target.Position, map, EmpBurstRadius, DamageDefOf.EMP, null);
            Messages.Message(
                "Stormproof_IonStorm_EmpBurst".Translate(target.LabelShort),
                target, MessageTypeDefOf.CautionInput);
        }

        // Extra short circuits routed through the vanilla incident so a ready
        // surge protector absorbs them exactly like a natural "Zzzt!".
        private void TriggerZzzt(Map map)
        {
            IncidentDef zzzt = DefDatabase<IncidentDef>.GetNamedSilentFail("ShortCircuit");
            if (zzzt == null)
            {
                return;
            }
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(zzzt.category, map);
            if (zzzt.Worker.CanFireNow(parms))
            {
                zzzt.Worker.TryExecute(parms);
            }
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Stormproof
{
    public class MapComponent_Stormproof : MapComponent
    {
        private const int WearInterval = 2500;
        private const float WearChance = 0.08f;
        private const int AlmanacMaxEntries = 80;

        private int wearCursor;
        private WeatherDef lastWeather;
        private int weatherStartedTick;
        private List<AlmanacEntry> almanac = new List<AlmanacEntry>();
        private Dictionary<int, float> brownoutByNetId = new Dictionary<int, float>();
        private int brownoutCachedTick = -1;

        public MapComponent_Stormproof(Map map) : base(map)
        {
        }

        public IReadOnlyList<AlmanacEntry> Almanac => almanac;

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            lastWeather = map.weatherManager?.curWeather;
            weatherStartedTick = Find.TickManager.TicksGame;
        }

        public override void MapComponentTick()
        {
            TickAlmanac();
            TickWear();
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                RefreshBrownoutCache();
            }
        }

        public float BrownoutFor(PowerNet net)
        {
            if (net == null || StormproofMod.Settings == null || !StormproofMod.Settings.enableBrownout)
            {
                return 0f;
            }
            if (brownoutCachedTick != Find.TickManager.TicksGame / 60)
            {
                RefreshBrownoutCache();
            }
            int id = net.GetHashCode();
            return brownoutByNetId.TryGetValue(id, out float v) ? v : 0f;
        }

        public float BrownoutFor(Thing thing)
        {
            CompPower power = thing?.TryGetComp<CompPower>();
            return BrownoutFor(power?.PowerNet);
        }

        private void RefreshBrownoutCache()
        {
            brownoutCachedTick = Find.TickManager.TicksGame / 60;
            brownoutByNetId.Clear();
            if (StormproofMod.Settings == null || !StormproofMod.Settings.enableBrownout || map.powerNetManager == null)
            {
                return;
            }
            float severity = StormproofMod.Settings.brownoutSeverity;
            List<PowerNet> nets = map.powerNetManager.AllNetsListForReading;
            for (int i = 0; i < nets.Count; i++)
            {
                PowerNet net = nets[i];
                if (net == null || net.batteryComps == null || net.batteryComps.Count == 0)
                {
                    continue;
                }
                float cap = 0f;
                float stored = 0f;
                for (int b = 0; b < net.batteryComps.Count; b++)
                {
                    CompPowerBattery bat = net.batteryComps[b];
                    cap += bat.Props.storedEnergyMax;
                    stored += bat.StoredEnergy;
                }
                if (cap <= 0f)
                {
                    continue;
                }
                float fraction = stored / cap;
                float raw = 0f;
                if (fraction < 0.40f)
                {
                    raw = (0.40f - fraction) / 0.40f;
                }
                brownoutByNetId[net.GetHashCode()] = raw * severity;
            }
        }

        private void TickAlmanac()
        {
            if (StormproofMod.Settings == null || !StormproofMod.Settings.enableAlmanac)
            {
                return;
            }
            WeatherDef cur = map.weatherManager.curWeather;
            if (cur != lastWeather)
            {
                CloseWeatherEntry();
                lastWeather = cur;
                weatherStartedTick = Find.TickManager.TicksGame;
                AddEntry("weather", cur?.label ?? "unknown");
            }
        }

        public void NotifyCondition(GameCondition condition, bool started)
        {
            if (StormproofMod.Settings == null || !StormproofMod.Settings.enableAlmanac || condition?.def == null)
            {
                return;
            }
            if (!condition.AffectedMaps.Contains(map))
            {
                return;
            }
            AddEntry(started ? "condition" : "conditionEnd", condition.def.label);
        }

        private void CloseWeatherEntry()
        {
            if (almanac.Count == 0 || lastWeather == null)
            {
                return;
            }
            AlmanacEntry last = almanac[almanac.Count - 1];
            if (last.kind == "weather" && last.durationTicks <= 0)
            {
                last.durationTicks = Find.TickManager.TicksGame - weatherStartedTick;
            }
        }

        private void AddEntry(string kind, string label)
        {
            int abs = Find.TickManager.TicksAbs;
            float longLat = Find.WorldGrid.LongLatOf(map.Tile).x;
            almanac.Add(new AlmanacEntry
            {
                kind = kind,
                label = label,
                year = GenDate.Year(abs, longLat),
                quadrum = (int)GenDate.Quadrum(abs, longLat),
                startTick = Find.TickManager.TicksGame
            });
            while (almanac.Count > AlmanacMaxEntries)
            {
                almanac.RemoveAt(0);
            }
        }

        private void TickWear()
        {
            if (StormproofMod.Settings == null || !StormproofMod.Settings.enableStormWear)
            {
                return;
            }
            if (!map.IsHashIntervalTick(WearInterval))
            {
                return;
            }
            if (!Storming())
            {
                return;
            }

            List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            if (list == null || list.Count == 0)
            {
                return;
            }
            int checkedCount = 0;
            int i = wearCursor % list.Count;
            int n = list.Count;
            while (checkedCount < 40 && checkedCount < n)
            {
                Thing thing = list[i];
                i = (i + 1) % n;
                checkedCount++;
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }
                if (!IsUnhardenedPowerKit(thing))
                {
                    continue;
                }
                if (!Rand.Chance(WearChance))
                {
                    continue;
                }
                int floor = UnityEngine.Mathf.Max(1, (int)(thing.MaxHitPoints * 0.15f));
                if (thing.HitPoints > floor)
                {
                    thing.HitPoints--;
                    thing.Map.listerBuildingsRepairable.Notify_BuildingTookDamage((Building)thing);
                }
            }
            wearCursor = i;
        }

        private bool Storming()
        {
            WeatherDef weather = map.weatherManager.curWeather;
            if (CompWeatherForecaster.BringsLightning(weather))
            {
                return true;
            }
            if (HazardProtection.ConditionActive(map, StormproofDefOf.Stormproof_IonStorm))
            {
                return true;
            }
            return map.windManager.WindSpeed >= 1.2f;
        }

        private static bool IsUnhardenedPowerKit(Thing thing)
        {
            if (thing.def.defName == "Stormproof_ArmoredConduit"
                || thing.def.defName == "Stormproof_StormCapacitor")
            {
                return false;
            }
            if (thing.def.defName == "PowerConduit")
            {
                return true;
            }
            if (thing.TryGetComp<CompStormCapacitor>() != null)
            {
                return false;
            }
            CompPowerBattery battery = thing.TryGetComp<CompPowerBattery>();
            return battery != null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref almanac, "stormproofAlmanac", LookMode.Deep);
            Scribe_Defs.Look(ref lastWeather, "stormproofAlmanacWeather");
            Scribe_Values.Look(ref weatherStartedTick, "stormproofAlmanacWeatherStart");
            Scribe_Values.Look(ref wearCursor, "stormproofWearCursor");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && almanac == null)
            {
                almanac = new List<AlmanacEntry>();
            }
        }
    }

    public class AlmanacEntry : IExposable
    {
        public string kind;
        public string label;
        public int year;
        public int quadrum;
        public int startTick;
        public int durationTicks;

        public void ExposeData()
        {
            Scribe_Values.Look(ref kind, "kind");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref year, "year");
            Scribe_Values.Look(ref quadrum, "quadrum");
            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Values.Look(ref durationTicks, "durationTicks");
        }
    }
}

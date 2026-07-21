using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    /// <summary>
    /// VGE-inspired life support for gravship-linked Strata decks.
    /// Underdecks do not free-replenish O₂; open shafts act as an umbilical to the host;
    /// launch dumps heat into heatsinks / ship rooms. No VEF PipeSystem dependency.
    /// </summary>
    public static class StrataGravshipLifeSupport
    {
        public static bool Enabled =>
            StrataMod.Settings == null || StrataMod.Settings.gravshipLifeSupportEnabled;

        /// <summary>Underdeck (or gravship A+) that must run on pumps / tanks / umbilical.</summary>
        public static bool IsLifeSupportDeck(Map map)
        {
            if (!Enabled || map == null || !StrataGravshipUtility.OdysseyActive)
            {
                return false;
            }
            if (!StrataGravshipUtility.IsGravshipLinkedLevel(map))
            {
                return false;
            }
            // Underdecks always; tower decks on gravship also (no free sky air in flight).
            return StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map);
        }

        public static bool HostIsVacuum(Map host)
        {
            return host?.Biome != null && host.Biome.inVacuum;
        }

        public static bool ShouldSkipGeothermalOutdoor(Map map)
        {
            return IsLifeSupportDeck(map);
        }

        /// <summary>Outdoor temp for gravship underdecks: host outdoor, or deep cold in vacuum.</summary>
        public static float OutdoorTempForLifeSupportDeck(Map map, float fallback)
        {
            Map host = StrataGravshipUtility.FindGravshipStackRoot(map) ?? map;
            if (host != null && HostIsVacuum(host))
            {
                return -75f;
            }
            if (host != null && host != map && host.mapTemperature != null)
            {
                return host.mapTemperature.OutdoorTemp;
            }
            return fallback;
        }

        /// <summary>
        /// Transfer breathable mix across an open gravship shaft (umbilical).
        /// Host → underdeck when underdeck is poorer; vacuum host drains underdeck.
        /// </summary>
        public static void ExchangeAtmosphereAcrossShaft(Map hostMap, IntVec3 hostCell, Map otherMap, IntVec3 otherCell)
        {
            if (!Enabled || hostMap == null || otherMap == null)
            {
                return;
            }
            if (!StrataMod.Settings.NaturalGasesActive)
            {
                return;
            }

            AtmosphereMapComponent hostAtmo = hostMap.GetComponent<AtmosphereMapComponent>();
            AtmosphereMapComponent otherAtmo = otherMap.GetComponent<AtmosphereMapComponent>();
            if (hostAtmo == null || otherAtmo == null)
            {
                return;
            }

            Room hostRoom = hostCell.GetRoom(hostMap);
            Room otherRoom = otherCell.GetRoom(otherMap);
            if (hostRoom == null || otherRoom == null)
            {
                return;
            }

            StrataGasDef o2 = StrataGasDefOf.Strata_Oxygen;
            StrataGasDef co2 = StrataGasDefOf.Strata_CarbonDioxide;
            if (o2 == null)
            {
                return;
            }

            float hostO2 = hostAtmo.DensityInRoom(hostRoom, o2);
            float otherO2 = otherAtmo.DensityInRoom(otherRoom, o2);
            float rate = 0.04f; // per exchange tick (~every 250t from stair tick)

            if (HostIsVacuum(hostMap))
            {
                // Open hatch to vacuum: bleed underdeck O₂ away.
                if (otherO2 > 0.01f)
                {
                    float drain = Mathf.Min(otherO2, rate * 1.5f);
                    otherAtmo.ConsumeGasFromRoomPublic(otherRoom, o2, drain);
                }
                return;
            }

            // Equalize O₂ toward host (surface ambient ~0.21).
            float delta = hostO2 - otherO2;
            if (Mathf.Abs(delta) < 0.002f)
            {
                return;
            }
            float move = Mathf.Clamp(delta * rate, -rate, rate);
            if (move > 0f)
            {
                otherAtmo.AddGasToRoomPublic(otherRoom, o2, move, otherCell);
                if (!AtmosphericMix.ForcesAmbientInEnclosedRooms(hostMap))
                {
                    hostAtmo.ConsumeGasFromRoomPublic(hostRoom, o2, move * 0.25f);
                }
            }
            else
            {
                otherAtmo.ConsumeGasFromRoomPublic(otherRoom, o2, -move);
                hostAtmo.AddGasToRoomPublic(hostRoom, o2, -move * 0.25f, hostCell);
            }

            if (co2 != null)
            {
                float hostCo2 = hostAtmo.DensityInRoom(hostRoom, co2);
                float otherCo2 = otherAtmo.DensityInRoom(otherRoom, co2);
                float co2Delta = hostCo2 - otherCo2;
                if (co2Delta > 0.002f)
                {
                    otherAtmo.AddGasToRoomPublic(otherRoom, co2, Mathf.Min(co2Delta * rate * 0.5f, rate * 0.5f), otherCell);
                }
                else if (co2Delta < -0.002f)
                {
                    otherAtmo.ConsumeGasFromRoomPublic(otherRoom, co2, Mathf.Min(-co2Delta * rate * 0.5f, rate * 0.5f));
                }
            }
        }
    }

    /// <summary>Launch heat pool keyed by grav engine, distributed to Strata heatsinks.</summary>
    public static class StrataGravshipHeat
    {
        private static readonly Dictionary<int, float> heatByEngineId = new Dictionary<int, float>();

        public static bool VgeHeatManagerPresent(Building_GravEngine engine)
        {
            if (engine == null || !StrataVgeCompat.Active)
            {
                return false;
            }
            // VGE attaches CompHeatManager — avoid double-dumping launch heat.
            List<ThingComp> comps = engine.AllComps;
            if (comps == null)
            {
                return false;
            }
            for (int i = 0; i < comps.Count; i++)
            {
                ThingComp c = comps[i];
                if (c != null && c.GetType().Name == "CompHeatManager")
                {
                    return true;
                }
            }
            return false;
        }

        public static void AddLaunchHeat(Building_GravEngine engine, float fuelCost)
        {
            if (engine == null || !StrataGravshipLifeSupport.Enabled)
            {
                return;
            }
            if (VgeHeatManagerPresent(engine))
            {
                return;
            }
            float amount = Mathf.Max(20f, fuelCost) * 8f;
            int id = engine.thingIDNumber;
            heatByEngineId.TryGetValue(id, out float cur);
            heatByEngineId[id] = cur + amount;
            Distribute(engine);
        }

        public static float PeekHeat(Building_GravEngine engine)
        {
            if (engine == null)
            {
                return 0f;
            }
            heatByEngineId.TryGetValue(engine.thingIDNumber, out float h);
            return h;
        }

        public static void Distribute(Building_GravEngine engine)
        {
            if (engine?.Map == null)
            {
                return;
            }
            int id = engine.thingIDNumber;
            if (!heatByEngineId.TryGetValue(id, out float remaining) || remaining <= 0f)
            {
                return;
            }

            List<CompGravshipHeatsink> sinks = CollectHeatsinks(engine);
            for (int i = 0; i < sinks.Count && remaining > 0f; i++)
            {
                float taken = sinks[i].AcceptHeat(remaining);
                remaining -= taken;
            }

            if (remaining > 0f)
            {
                PushOverflowToShipRooms(engine, remaining);
                remaining = 0f;
            }
            heatByEngineId[id] = remaining;
        }

        private static List<CompGravshipHeatsink> CollectHeatsinks(Building_GravEngine engine)
        {
            var list = new List<CompGravshipHeatsink>();
            void Scan(Map map)
            {
                if (map?.listerBuildings?.allBuildingsColonist == null)
                {
                    return;
                }
                List<Building> buildings = map.listerBuildings.allBuildingsColonist;
                for (int i = 0; i < buildings.Count; i++)
                {
                    CompGravshipHeatsink sink = buildings[i]?.TryGetComp<CompGravshipHeatsink>();
                    if (sink != null && sink.parent.Spawned)
                    {
                        list.Add(sink);
                    }
                }
            }
            Scan(engine.Map);
            List<Map> levels = StrataGravshipStackUtility.CollectTravellingLevels(engine);
            for (int i = 0; i < levels.Count; i++)
            {
                Scan(levels[i]);
            }
            // Also scan currently linked pockets even when not travelling.
            if (engine.Map != null)
            {
                foreach (Thing t in engine.Map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (t is MapPortal portal && portal.PocketMapExists
                        && StrataGravshipUtility.IsGravshipPortal(t))
                    {
                        Scan(portal.PocketMap);
                    }
                }
            }
            return list;
        }

        private static void PushOverflowToShipRooms(Building_GravEngine engine, float heat)
        {
            Map map = engine.Map;
            if (map == null || heat <= 0f)
            {
                return;
            }
            HashSet<IntVec3> cells = engine.ValidSubstructure;
            if (cells == null || cells.Count == 0)
            {
                cells = engine.AllConnectedSubstructure;
            }
            if (cells == null || cells.Count == 0)
            {
                Room engineRoom = engine.Position.GetRoom(map);
                engineRoom?.PushHeat(heat);
                return;
            }
            var rooms = new HashSet<Room>();
            foreach (IntVec3 cell in cells)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                Room r = cell.GetRoom(map);
                if (r != null && !r.UsesOutdoorTemperature)
                {
                    rooms.Add(r);
                }
            }
            if (rooms.Count == 0)
            {
                return;
            }
            float per = heat / rooms.Count;
            foreach (Room r in rooms)
            {
                r.PushHeat(per);
            }
        }

        public static void Clear()
        {
            heatByEngineId.Clear();
        }
    }
}

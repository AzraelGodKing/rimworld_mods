using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    internal static class StrataAlertScanCache
    {
        private const int ScanInterval = 250;

        public static bool ShouldRescan(ref int lastScanTick)
        {
            int tick = Find.TickManager.TicksGame;
            if (tick - lastScanTick < ScanInterval)
            {
                return false;
            }
            lastScanTick = tick;
            return true;
        }
    }

    // Harmful gas piling up on a level with nobody on it - a generator left
    // running in a sealed room downstairs, or a breached gas pocket, kills the
    // next colonist who walks in.
    public class Alert_SmokeOnVacantLevel : Alert
    {
        private const float WorryThreshold = 0.35f;

        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();
        private AlertReport cachedReport = false;
        private int lastScanTick = -9999;

        public Alert_SmokeOnVacantLevel()
        {
            defaultLabel = "Strata_Alert_SmokeVacant_Label".Translate();
            defaultExplanation = "Strata_Alert_SmokeVacant_Explanation".Translate();
        }

        public override AlertReport GetReport()
        {
            if (!StrataAlertScanCache.ShouldRescan(ref lastScanTick))
            {
                return cachedReport;
            }
            targets.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (!StrataLevelPerfUtility.IsStrataPocketLevel(map)
                    || map.mapPawns.FreeColonistsSpawnedCount > 0)
                {
                    continue;
                }
                AtmosphereMapComponent atmosphere = map.GetComponent<AtmosphereMapComponent>();
                if (atmosphere != null && atmosphere.TryGetWorstHarmfulCloud(out IntVec3 cell, out float density)
                    && density >= WorryThreshold)
                {
                    targets.Add(new GlobalTargetInfo(cell, map));
                }
            }
            cachedReport = targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
            return cachedReport;
        }
    }

    // Flammable gas pooling in a room that holds an open flame. The room
    // explodes the moment the gas reaches ignition density - this is the
    // window to douse the fire or get out.
    public class Alert_FlammableGasNearFlame : Alert_Critical
    {
        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();
        private AlertReport cachedReport = false;
        private int lastScanTick = -9999;

        public Alert_FlammableGasNearFlame()
        {
            defaultLabel = "Strata_Alert_FlammableGas_Label".Translate();
            defaultExplanation = "Strata_Alert_FlammableGas_Explanation".Translate();
        }

        public override AlertReport GetReport()
        {
            if (!StrataAlertScanCache.ShouldRescan(ref lastScanTick))
            {
                return cachedReport;
            }
            targets.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                AtmosphereMapComponent atmosphere = maps[i].GetComponent<AtmosphereMapComponent>();
                if (atmosphere == null)
                {
                    continue;
                }
                for (int j = 0; j < atmosphere.FlammableRiskCells.Count; j++)
                {
                    targets.Add(new GlobalTargetInfo(atmosphere.FlammableRiskCells[j], maps[i]));
                }
            }
            cachedReport = targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
            return cachedReport;
        }
    }

    // High-value goods still on linked underground levels while a surface
    // caravan is being packed.
    public class Alert_CaravanGoodsBelow : Alert
    {
        public Alert_CaravanGoodsBelow()
        {
            defaultLabel = "Strata_Alert_CaravanGoods_Label".Translate();
            defaultExplanation = "Strata_Alert_CaravanGoods_Explanation".Translate();
            defaultPriority = AlertPriority.Medium;
        }

        public override AlertReport GetReport()
        {
            if (StrataMod.Settings?.caravanPullEnabled != true || !StrataCaravanUtility.CaravanDialogOpen)
            {
                return false;
            }
            Map surface = StrataCaravanUtility.CaravanFormingMap;
            if (surface == null)
            {
                return false;
            }
            return StrataCaravanUtility.CountValuableBelow(surface) > 0
                ? AlertReport.CulpritIs(new GlobalTargetInfo(surface.Center, surface))
                : false;
        }
    }

    // Colonists on a linked level whose every way back to the surface is sealed.
    // Deliberate bunkering is fine; forgetting them on B1 or A1 is not.
    public class Alert_ColonistsBelowSealedShaft : Alert
    {
        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();
        private AlertReport cachedReport = false;
        private int lastScanTick = -9999;

        public Alert_ColonistsBelowSealedShaft()
        {
            defaultLabel = "Strata_Alert_SealedColonists_Label".Translate();
            defaultExplanation = "Strata_Alert_SealedColonists_Explanation".Translate();
            defaultPriority = AlertPriority.High;
        }

        public override AlertReport GetReport()
        {
            if (!StrataAlertScanCache.ShouldRescan(ref lastScanTick))
            {
                return cachedReport;
            }
            targets.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                bool linked = StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map);
                if (!linked || map.mapPawns.FreeColonistsSpawnedCount == 0)
                {
                    continue;
                }
                if (!AllExitsSealed(map))
                {
                    continue;
                }
                List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
                for (int j = 0; j < colonists.Count; j++)
                {
                    targets.Add(colonists[j]);
                }
            }
            cachedReport = targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
            return cachedReport;
        }

        private static bool AllExitsSealed(Map map)
        {
            bool anyExit = false;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (!(thing is PocketMapExit exit) || thing is Building_StairsDown || !exit.Spawned)
                {
                    continue;
                }
                anyExit = true;
                if (!StrataPortalUtility.IsSealedPortal(exit.entrance ?? (Thing)exit))
                {
                    return false; // at least one way home is open
                }
            }
            return anyExit;
        }
    }

    // SleepRelay scanned every linked floor and found no unowned/reachable bed.
    public class Alert_SleepBedNotFound : Alert
    {
        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();
        private AlertReport cachedReport = false;
        private int lastScanTick = -9999;

        public Alert_SleepBedNotFound()
        {
            defaultLabel = "Strata_Alert_SleepBedNotFound_Label".Translate();
            defaultExplanation = "Strata_Alert_SleepBedNotFound_Explanation".Translate();
            defaultPriority = AlertPriority.High;
        }

        public override AlertReport GetReport()
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.restRelayEnabled)
            {
                cachedReport = false;
                return cachedReport;
            }

            if (!StrataAlertScanCache.ShouldRescan(ref lastScanTick))
            {
                return cachedReport;
            }

            targets.Clear();
            SleepRelay.CollectBedNotFoundCulprits(targets);
            cachedReport = targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
            return cachedReport;
        }
    }

    // A mine canary sick or dead from bad air — warns before colonists show hediffs.
    public class Alert_CanaryWarning : Alert
    {
        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();
        private AlertReport cachedReport = false;
        private int lastScanTick = -9999;

        public Alert_CanaryWarning()
        {
            defaultLabel = "Strata_Alert_Canary_Label".Translate();
            defaultExplanation = "Strata_Alert_Canary_Explanation".Translate();
            defaultPriority = AlertPriority.High;
        }

        public override AlertReport GetReport()
        {
            if (!StrataAlertScanCache.ShouldRescan(ref lastScanTick))
            {
                return cachedReport;
            }
            targets.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                List<Building> buildings = map.listerBuildings.allBuildingsColonist;
                for (int b = 0; b < buildings.Count; b++)
                {
                    Building building = buildings[b];
                    if (building.TryGetComp<CompCanaryCage>() is CompCanaryCage cage && cage.NeedsAttention)
                    {
                        targets.Add(new GlobalTargetInfo(building));
                    }
                }
            }
            cachedReport = targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
            return cachedReport;
        }
    }
}

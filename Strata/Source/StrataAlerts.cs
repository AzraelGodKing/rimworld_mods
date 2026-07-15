using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Harmful gas piling up on a level with nobody on it - a generator left
    // running in a sealed room downstairs, or a breached gas pocket, kills the
    // next colonist who walks in.
    public class Alert_SmokeOnVacantLevel : Alert
    {
        private const float WorryThreshold = 0.35f;

        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();

        public Alert_SmokeOnVacantLevel()
        {
            defaultLabel = "Gas building underground";
            defaultExplanation = "Harmful gas is accumulating on a level with no colonists on it. "
                + "Something is burning or seeping down there with no working ventilation - the "
                + "next person to walk in will be breathing it.";
        }

        public override AlertReport GetReport()
        {
            targets.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (!StrataMapUtility.IsUnderground(map) || map.mapPawns.FreeColonistsSpawnedCount > 0)
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
            return targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
        }
    }

    // Flammable gas pooling in a room that holds an open flame. The room
    // explodes the moment the gas reaches ignition density - this is the
    // window to douse the fire or get out.
    public class Alert_FlammableGasNearFlame : Alert_Critical
    {
        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();

        public Alert_FlammableGasNearFlame()
        {
            defaultLabel = "Flammable gas near open flame";
            defaultExplanation = "Flammable gas is pooling in a room that contains an open flame - a "
                + "torch, a campfire, or a running fuel burner. When the gas thickens past ignition "
                + "density the room will explode. Extinguish the flame, vent the room, or switch to "
                + "electric light.";
        }

        public override AlertReport GetReport()
        {
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
            return targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
        }
    }

    // High-value goods still on linked underground levels while a surface
    // caravan is being packed.
    public class Alert_CaravanGoodsBelow : Alert
    {
        public Alert_CaravanGoodsBelow()
        {
            defaultLabel = "Caravan goods below";
            defaultExplanation = "Valuable items remain on linked underground levels while a caravan is being formed on the surface. "
                + "Enable caravan pull in Strata settings or haul them up manually before leaving.";
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

    // Colonists on a level whose every way up is sealed. Deliberate bunkering
    // is fine; forgetting them down there is not.
    public class Alert_ColonistsBelowSealedShaft : Alert
    {
        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();

        public Alert_ColonistsBelowSealedShaft()
        {
            defaultLabel = "Colonists sealed below";
            defaultExplanation = "Colonists are on an underground level whose every exit shaft is "
                + "sealed. They cannot come up until a stairwell or elevator is unsealed.";
            defaultPriority = AlertPriority.High;
        }

        public override AlertReport GetReport()
        {
            targets.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (!StrataMapUtility.IsUnderground(map) || map.mapPawns.FreeColonistsSpawnedCount == 0)
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
            return targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
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
                    return false; // at least one way up is open
                }
            }
            return anyExit;
        }
    }

    // A mine canary sick or dead from bad air — warns before colonists show hediffs.
    public class Alert_CanaryWarning : Alert
    {
        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();

        public Alert_CanaryWarning()
        {
            defaultLabel = "Canary warning";
            defaultExplanation = "A mine canary is distressed or dead from bad air in a cage. "
                + "Canaries succumb sooner than colonists — ventilate the room or evacuate before "
                + "people start coughing, suffocating, or worse.";
            defaultPriority = AlertPriority.High;
        }

        public override AlertReport GetReport()
        {
            targets.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                foreach (Thing thing in map.listerThings.AllThings)
                {
                    if (thing.TryGetComp<CompCanaryCage>() is CompCanaryCage cage && cage.NeedsAttention)
                    {
                        targets.Add(new GlobalTargetInfo(thing));
                    }
                }
            }
            return targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
        }
    }
}

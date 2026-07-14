using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public static class DeepPocketUtility
    {
        private static ThingDef steamGeyserDef;

        private static ThingDef gasVentDef;

        public static ThingDef SteamGeyserDef =>
            steamGeyserDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("SteamGeyser");

        public static ThingDef GasVentDef =>
            gasVentDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("Strata_GasVent");

        public static bool TrySeedPocket(Map map, DeepPocketKind kind, float radius, out DeepPocketRecord record)
        {
            record = null;
            if (map == null)
            {
                return false;
            }
            IntVec3 avoid = LandingSpot(map);
            if (!CellFinder.TryFindRandomCell(map, c => IsValidSeed(c, map, avoid, radius + 8f), out IntVec3 center))
            {
                return false;
            }

            var cells = new List<IntVec3>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (cell.InBounds(map))
                {
                    cells.Add(cell);
                }
            }
            if (cells.Count == 0)
            {
                return false;
            }

            record = new DeepPocketRecord
            {
                id = map.uniqueID * 1000 + Rand.Int,
                kind = kind,
                center = center,
                radius = radius,
                cells = cells,
            };
            return true;
        }

        public static void Breach(Map map, DeepPocketRecord pocket, bool sendLetter, string extraLetterText = null)
        {
            if (pocket == null || pocket.breached || map == null)
            {
                return;
            }
            pocket.breached = true;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pocket.center, pocket.radius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
            }

            if (pocket.kind == DeepPocketKind.Geothermal && SteamGeyserDef != null
                && pocket.center.GetFirstBuilding(map) == null)
            {
                GenSpawn.Spawn(ThingMaker.MakeThing(SteamGeyserDef), pocket.center, map);
            }
            else if (pocket.kind != DeepPocketKind.Geothermal && GasVentDef != null
                && pocket.center.GetFirstBuilding(map) == null)
            {
                GenSpawn.Spawn(ThingMaker.MakeThing(GasVentDef), pocket.center, map);
            }

            SmokeMapComponent atmosphere = map.GetComponent<SmokeMapComponent>();
            if (pocket.kind != DeepPocketKind.Geothermal)
            {
                AtmosphereChannel channel = pocket.kind == DeepPocketKind.NaturalGas
                    ? AtmosphereChannel.NaturalGas
                    : AtmosphereChannel.Toxic;
                float release = pocket.kind == DeepPocketKind.NaturalGas ? 0.95f : 0.85f;
                Room room = pocket.center.GetRoom(map);
                atmosphere?.AddGasToRoom(channel, room, release, pocket.center);
            }

            if (sendLetter)
            {
                string label;
                string text;
                switch (pocket.kind)
                {
                    case DeepPocketKind.Geothermal:
                        label = "Geothermal chamber";
                        text = "Mining has broken into a hidden steam chamber sealed in the rock. "
                            + "A geothermal vent sits at its heart — hook it up with a vanilla geothermal generator.";
                        break;
                    case DeepPocketKind.NaturalGas:
                        label = "Natural gas pocket";
                        text = "A pressurized pocket of natural gas has been breached. "
                            + "Vent the room or seal it off — open flames in thick gas can ignite it. "
                            + "Build a gas well on the vent to harvest what remains.";
                        break;
                    default:
                        label = "Toxic gas pocket";
                        text = "Mining has punctured a pocket of foul underground gas. "
                            + "Toxic vapour is flooding the chamber — get colonists clear until it vents.";
                        break;
                }
                if (!extraLetterText.NullOrEmpty())
                {
                    text = extraLetterText + "\n\n" + text;
                }
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent,
                    new TargetInfo(pocket.center, map));
            }
        }

        public static bool TryGetUnbreached(Map map, out DeepPocketRecord pocket)
        {
            pocket = null;
            MapComponent_DeepPockets comp = map?.GetComponent<MapComponent_DeepPockets>();
            if (comp == null || comp.Pockets.Count == 0)
            {
                return false;
            }
            List<DeepPocketRecord> candidates = new List<DeepPocketRecord>();
            foreach (DeepPocketRecord p in comp.Pockets)
            {
                if (!p.breached && p.kind != DeepPocketKind.Geothermal)
                {
                    candidates.Add(p);
                }
            }
            if (candidates.Count == 0)
            {
                return false;
            }
            pocket = candidates.RandomElement();
            return true;
        }

        public static bool ContainsCell(DeepPocketRecord pocket, IntVec3 cell)
        {
            if (pocket == null)
            {
                return false;
            }
            for (int i = 0; i < pocket.cells.Count; i++)
            {
                if (pocket.cells[i] == cell)
                {
                    return true;
                }
            }
            return false;
        }

        private static IntVec3 LandingSpot(Map map)
        {
            MapPortal entrance = PocketMapUtility.currentlyGeneratingPortal;
            if (entrance?.Map != null)
            {
                return StrataMapUtility.VerticalAlign(entrance.Position, entrance.Map, map);
            }
            return map.Center;
        }

        private static bool IsValidSeed(IntVec3 cell, Map map, IntVec3 avoid, float avoidRadius)
        {
            if (!cell.InBounds(map) || cell.DistanceTo(avoid) < avoidRadius)
            {
                return false;
            }
            if (cell.DistanceToEdge(map) < 6)
            {
                return false;
            }
            Mineable rock = cell.GetFirstMineable(map);
            return rock != null && rock.def.building != null && rock.def.building.isNaturalRock;
        }
    }
}

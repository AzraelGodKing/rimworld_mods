using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace LivingWorld
{
    // Season and biome on tiles you already walked. A way-camp makes it
    // rarer. Not a raid — tired legs and a letter.
    internal static class LivingWorldRoadHazards
    {
        public static void TryOn(Caravan caravan, GameComponent_LivingWorldRoad road)
        {
            if (caravan == null || road == null || !caravan.IsPlayerControlled)
            {
                return;
            }
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (settings == null || !settings.enabled || !settings.roadHazardsEnabled)
            {
                return;
            }
            if (road.Tiles.Count < 8)
            {
                return;
            }
            int tileId = caravan.Tile;
            bool onRoad = false;
            for (int i = 0; i < road.Tiles.Count; i++)
            {
                if (road.Tiles[i] == tileId)
                {
                    onRoad = true;
                    break;
                }
            }
            if (!onRoad)
            {
                return;
            }
            if (caravan.NightResting)
            {
                return;
            }
            float mtbDays = LivingWorldWayCamps.OnCampTile(caravan.Tile) ? 24f : 10f;
            if (!Rand.MTBEventOccurs(mtbDays, 60000f, 2500f))
            {
                return;
            }
            if (!road.TryConsumeHazardCooldown(60000))
            {
                return;
            }

            string flavorKey = PickFlavor(caravan);
            DrainMarch(caravan);
            Find.LetterStack.ReceiveLetter(
                "LivingWorld_LetterLabel_RouteHazard".Translate(),
                "LivingWorld_LetterText_RouteHazard".Translate(
                    caravan.LabelCap,
                    flavorKey.Translate()),
                LetterDefOf.NegativeEvent,
                caravan);
        }

        private static string PickFlavor(Caravan caravan)
        {
            string biome = BiomeName(caravan.Tile);
            Season season = Season.Undefined;
            try
            {
                Vector2 longLat = Find.WorldGrid.LongLatOf(caravan.Tile);
                season = GenDate.Season(Find.TickManager.TicksAbs, longLat);
            }
            catch (Exception)
            {
            }

            if (season == Season.Winter || ContainsAny(biome, "Tundra", "Ice", "Boreal", "Cold"))
            {
                return "LivingWorld_Hazard_Cold";
            }
            if (ContainsAny(biome, "Desert", "Arid", "Extreme"))
            {
                return "LivingWorld_Hazard_Heat";
            }
            if (ContainsAny(biome, "Swamp", "Marsh", "Tropical", "Rainforest", "Wet"))
            {
                return "LivingWorld_Hazard_Mud";
            }
            return "LivingWorld_Hazard_Wind";
        }

        private static string BiomeName(PlanetTile tile)
        {
            try
            {
                if (Find.WorldGrid == null || tile.tileId < 0
                    || tile.tileId >= Find.WorldGrid.TilesCount)
                {
                    return string.Empty;
                }
                BiomeDef biome = Find.WorldGrid[tile].PrimaryBiome;
                return biome?.defName ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static bool ContainsAny(string hay, params string[] needles)
        {
            if (string.IsNullOrEmpty(hay))
            {
                return false;
            }
            for (int i = 0; i < needles.Length; i++)
            {
                if (hay.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static void DrainMarch(Caravan caravan)
        {
            HediffDef veteran = DefDatabase<HediffDef>.GetNamedSilentFail("LW_Hediff_RoadVeteran");
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn?.needs == null || pawn.Dead)
                {
                    continue;
                }
                float restHit = 0.12f;
                float foodHit = 0.05f;
                if (veteran != null
                    && pawn.health?.hediffSet?.GetFirstHediffOfDef(veteran) != null)
                {
                    restHit *= 0.45f;
                    foodHit *= 0.45f;
                }
                if (pawn.needs.rest != null)
                {
                    pawn.needs.rest.CurLevel = Math.Max(0.05f, pawn.needs.rest.CurLevel - restHit);
                }
                if (pawn.needs.food != null)
                {
                    pawn.needs.food.CurLevel = Math.Max(0.08f, pawn.needs.food.CurLevel - foodHit);
                }
            }
        }
    }
}

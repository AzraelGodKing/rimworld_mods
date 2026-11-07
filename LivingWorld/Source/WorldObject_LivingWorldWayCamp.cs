using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    public class WorldObject_LivingWorldWayCamp : WorldObject
    {
        public override void PostAdd()
        {
            base.PostAdd();
            LivingWorldWorldObjectIndex.MarkDirty();
        }

        public override void PostRemove()
        {
            base.PostRemove();
            LivingWorldWorldObjectIndex.MarkDirty();
        }

        public override string GetInspectString()
        {
            string baseStr = base.GetInspectString();
            string line = "LivingWorld_WayCamp_Inspect".Translate();
            return string.IsNullOrEmpty(baseStr) ? line : baseStr + "\n" + line;
        }
    }

    public static class LivingWorldWayCamps
    {
        private const int MaxCamps = 10;

        public static bool OnCampTile(PlanetTile tile) => LivingWorldWorldObjectIndex.IsCampTile(tile);

        public static void TryFoundOn(PlanetTile tile)
        {
            if (OnCampTile(tile))
            {
                return;
            }
            if (LivingWorldWorldObjectIndex.CampCount >= MaxCamps)
            {
                return;
            }
            var camp = (WorldObject_LivingWorldWayCamp)WorldObjectMaker.MakeWorldObject(LivingWorldDefOf.LivingWorld_WayCamp);
            camp.Tile = tile;
            camp.SetFaction(Faction.OfPlayer);
            Find.WorldObjects.Add(camp);
        }
    }

    [HarmonyPatch(typeof(Need_Rest), nameof(Need_Rest.NeedInterval))]
    public static class Patch_WayCampRest
    {
        public static void Postfix(Need_Rest __instance, Pawn ___pawn)
        {
            Pawn pawn = ___pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (settings == null || !settings.enabled)
            {
                return;
            }
            // Cheapest checks first: this runs for every pawn's rest need.
            Caravan caravan = pawn.GetCaravan();
            if (caravan == null || !caravan.NightResting || !caravan.IsPlayerControlled)
            {
                return;
            }
            if (!LivingWorldWayCamps.OnCampTile(caravan.Tile))
            {
                return;
            }
            __instance.CurLevel = System.Math.Min(__instance.CurLevel + 0.001f, 1f);
        }
    }
}

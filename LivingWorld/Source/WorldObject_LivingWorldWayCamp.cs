using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    public class WorldObject_LivingWorldWayCamp : WorldObject
    {
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

        public static bool OnCampTile(PlanetTile tile)
        {
            foreach (WorldObject wo in Find.WorldObjects.ObjectsAt(tile))
            {
                if (wo is WorldObject_LivingWorldWayCamp)
                {
                    return true;
                }
            }
            return false;
        }

        public static void TryFoundOn(PlanetTile tile)
        {
            if (OnCampTile(tile))
            {
                return;
            }
            int existing = 0;
            List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is WorldObject_LivingWorldWayCamp)
                {
                    existing++;
                }
            }
            if (existing >= MaxCamps)
            {
                return;
            }
            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("LivingWorld_WayCamp");
            if (def == null)
            {
                return;
            }
            var camp = (WorldObject_LivingWorldWayCamp)WorldObjectMaker.MakeWorldObject(def);
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
            Caravan caravan = pawn.GetCaravan();
            if (caravan == null || !caravan.IsPlayerControlled)
            {
                return;
            }
            if (!LivingWorldWayCamps.OnCampTile(caravan.Tile) || !caravan.NightResting)
            {
                return;
            }
            __instance.CurLevel = System.Math.Min(__instance.CurLevel + 0.001f, 1f);
        }
    }
}

using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Saves from before Strata spawned its own landings have the vanilla rope
    // "cave exit" at the bottom of every level - no power shaft comp, no Strata
    // behavior at all. Swap each one for its entrance's real exitDef on load.
    [HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
    public static class Patch_ReplaceLegacyLandings
    {
        public static void Postfix()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                var portals = new List<Thing>(map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal));
                for (int j = 0; j < portals.Count; j++)
                {
                    if (!(portals[j] is PocketMapExit exit) || exit is Building_StairsUp || !exit.Spawned)
                    {
                        continue;
                    }
                    if (!(exit.entrance is Building_StairsDown entrance))
                    {
                        continue;
                    }
                    ThingDef properDef = entrance.def.portal?.exitDef;
                    if (properDef == null || exit.def == properDef)
                    {
                        continue;
                    }
                    IntVec3 position = exit.Position;
                    // Vanilla cave exits are flagged non-destroyable; despawned
                    // and unreferenced, it is dropped on the next save instead.
                    exit.DeSpawn();
                    PocketMapUtility.currentlyGeneratingPortal = entrance;
                    try
                    {
                        StrataPortalUtility.SpawnLanding(properDef, position, map);
                    }
                    finally
                    {
                        PocketMapUtility.currentlyGeneratingPortal = null;
                    }
                    Log.Message($"[Strata] Upgraded legacy landing under {entrance.LabelCap} to {properDef.defName}.");
                }
            }
        }
    }
}

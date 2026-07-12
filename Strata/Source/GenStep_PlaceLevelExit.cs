using RimWorld;
using Verse;

namespace Strata
{
    // Spawns the entrance portal's own exitDef (stairwell or elevator landing)
    // in the chamber GenStep_SolidRock carved. Vanilla GenStep_PlaceCaveExit
    // ignores exitDef and always spawns its 3x3 rope cave exit, which has no
    // power shaft and none of Strata's seal or safety behavior.
    public class GenStep_PlaceLevelExit : GenStep
    {
        public override int SeedPart => 847291043;

        public override void Generate(Map map, GenStepParams parms)
        {
            MapPortal entrance = PocketMapUtility.currentlyGeneratingPortal;
            ThingDef exitDef = entrance?.def.portal?.exitDef ?? ThingDefOf.CaveExit;
            IntVec3 spot = MapGenerator.PlayerStartSpot;
            if (!spot.IsValid || !spot.InBounds(map))
            {
                spot = map.Center;
            }
            StrataPortalUtility.SpawnLanding(exitDef, spot, map);
            MapGenerator.PlayerStartSpot = spot;
        }
    }
}

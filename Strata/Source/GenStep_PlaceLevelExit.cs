using RimWorld;
using Verse;

namespace Strata
{
    // Vanilla GenStep_PlaceCaveExit ignores portal exitDef (always the 3x3 rope cave exit).
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
                spot = entrance?.Map != null
                    ? StrataMapUtility.VerticalAlign(entrance.Position, entrance.Map, map)
                    : map.Center;
            }
            StrataPortalUtility.SpawnLanding(exitDef, spot, map);
            MapGenerator.PlayerStartSpot = spot;
        }
    }
}

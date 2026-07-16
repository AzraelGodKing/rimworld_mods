using RimWorld;
using Verse;

namespace Strata
{
    // Gravship B1: metal underdeck matching the host substructure, not a mined cavern.
    public class GenStep_GravshipUnderdeck : GenStep
    {
        public override int SeedPart => 918273645;

        public override void Generate(Map map, GenStepParams parms)
        {
            MapPortal entrance = PocketMapUtility.currentlyGeneratingPortal;
            IntVec3 spot = entrance?.Map != null
                ? StrataMapUtility.VerticalAlign(entrance.Position, entrance.Map, map)
                : map.Center;

            GravshipDeckUtility.PaintUnderdeck(map, spot);
            MapGenerator.PlayerStartSpot = spot;
        }
    }
}

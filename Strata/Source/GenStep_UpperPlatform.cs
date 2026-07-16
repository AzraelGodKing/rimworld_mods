using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Upper levels are roof decks: buildable only where the floor below is
    // roofed, plus a plaza around the tower shaft. Everywhere else is open sky.
    public class GenStep_UpperPlatform : GenStep
    {
        public override int SeedPart => 591837264;

        public override void Generate(Map map, GenStepParams parms)
        {
            MapPortal entrance = PocketMapUtility.currentlyGeneratingPortal;
            IntVec3 spot = entrance?.Map != null
                ? StrataMapUtility.VerticalAlign(entrance.Position, entrance.Map, map)
                : map.Center;

            UpperDeckUtility.PaintFromSourceRoofs(map, spot, UpperDeckUtility.DefaultPlazaRadius);
            if (entrance is IStrataGravshipPortal)
            {
                Map host = UpperDeckUtility.SourceMapFor(map);
                Building_GravEngine engine = host != null
                    ? StrataGravshipUtility.FindGravEngineOnMap(host)
                    : null;
                if (engine != null)
                {
                    StrataGravshipSubstructureSync.SyncMap(map, host, engine);
                }
            }
            MapGenerator.PlayerStartSpot = spot;

            if (Prefs.DevMode)
            {
                int deck = 0;
                int sky = 0;
                foreach (IntVec3 cell in map.AllCells)
                {
                    TerrainDef t = cell.GetTerrain(map);
                    if (t?.defName == UpperDeckUtility.RoofDeckDefName)
                    {
                        deck++;
                    }
                    else if (t?.defName == UpperDeckUtility.OpenSkyDefName)
                    {
                        sky++;
                    }
                }
                Log.Message($"[Strata] Upper deck gen: {deck} roof-deck cells, {sky} open-sky cells (landing {spot}).");
            }
        }
    }
}

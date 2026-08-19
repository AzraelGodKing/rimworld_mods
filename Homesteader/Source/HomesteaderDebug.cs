using LudeonTK;
using RimWorld;
using Verse;

namespace Homesteader
{
    public static class HomesteaderDebug
    {
        private const string Cat = "Homesteader";

        [DebugAction(Cat, "Force harvest festival", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceFestival()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            GameComponent_HomesteaderYard.Get()?.TryStartFestival(map, force: true);
        }

        [DebugAction(Cat, "Force fox-on-coop", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceFox()
        {
            TryIncident("Homesteader_FoxOnCoop");
        }

        [DebugAction(Cat, "Force county fair", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceFair()
        {
            TryIncident("Homesteader_CountyFair");
        }

        [DebugAction(Cat, "Add +10 farm brand", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddBrand()
        {
            GameComponent_HomesteaderYard.Get()?.AddBrand(10f);
            Messages.Message(
                "Brand now " + (GameComponent_HomesteaderYard.Get()?.brand ?? 0f).ToString("F0"),
                MessageTypeDefOf.NeutralEvent);
        }

        [DebugAction(Cat, "Rebuild larder cache", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RebuildLarder()
        {
            Map map = Find.CurrentMap;
            MapComponent_HomesteaderPantry pantry = map?.GetComponent<MapComponent_HomesteaderPantry>();
            pantry?.Rebuild();
            Messages.Message(
                "Larder kinds: " + (pantry?.DistinctPreservedKinds ?? 0),
                MessageTypeDefOf.NeutralEvent);
        }

        private static void TryIncident(string defName)
        {
            IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(defName);
            Map map = Find.CurrentMap;
            if (def?.Worker == null || map == null)
            {
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            if (!def.Worker.TryExecute(parms))
            {
                Messages.Message("Incident failed.", MessageTypeDefOf.RejectInput);
            }
        }
    }
}

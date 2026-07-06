using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    // Storm spires attract lightning that would land within their radius.
    // The bolt still strikes normally (authentic visuals, minor damage to the
    // spire) - the postfix then lets the spire harvest it.
    [HarmonyPatch(typeof(WeatherEvent_LightningStrike), nameof(WeatherEvent_LightningStrike.FireEvent))]
    public static class Patch_LightningStrike
    {
        private static readonly AccessTools.FieldRef<WeatherEvent_LightningStrike, IntVec3> StrikeLocRef =
            AccessTools.FieldRefAccess<WeatherEvent_LightningStrike, IntVec3>("strikeLoc");

        private static readonly AccessTools.FieldRef<WeatherEvent, Map> MapRef =
            AccessTools.FieldRefAccess<WeatherEvent, Map>("map");

        public static void Prefix(WeatherEvent_LightningStrike __instance)
        {
            Map map = MapRef(__instance);
            if (map == null || StormproofRegistry.Spires.Count == 0)
            {
                return;
            }
            IntVec3 loc = StrikeLocRef(__instance);
            // Random ambient strike: pick the cell ourselves (same way vanilla
            // does) so we can test it against spire radii.
            if (!loc.IsValid)
            {
                loc = CellFinderLoose.RandomCellWith(
                    sq => sq.Standable(map) && !map.roofGrid.Roofed(sq), map);
            }
            CompStormSpire catcher = StormproofRegistry
                .On(StormproofRegistry.Spires, map)
                .Where(s => s.Attracting &&
                            s.parent.Position.DistanceTo(loc) <= s.Props.attractRadius)
                .OrderBy(s => s.parent.Position.DistanceTo(loc))
                .FirstOrDefault();
            StrikeLocRef(__instance) = catcher != null ? catcher.parent.Position : loc;
        }

        public static void Postfix(WeatherEvent_LightningStrike __instance)
        {
            Map map = MapRef(__instance);
            if (map == null)
            {
                return;
            }
            IntVec3 loc = StrikeLocRef(__instance);
            CompStormSpire spire = StormproofRegistry
                .On(StormproofRegistry.Spires, map)
                .FirstOrDefault(s => s.parent.Position == loc);
            spire?.Notify_Struck();
        }
    }
}

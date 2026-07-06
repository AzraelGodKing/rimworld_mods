using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    // Storm spires attract lightning that would land within their radius.
    //
    // Both WeatherEvent_LightningStrike and WeatherEvent_LightningStrikeDelayed
    // (thunderstorms use the delayed variant - they are siblings, not
    // parent/child) funnel into the shared static
    // WeatherEvent_LightningStrike.DoStrike(strikeLoc, map, ref boltMesh),
    // so this one patch catches every bolt in the game.
    [HarmonyPatch(typeof(WeatherEvent_LightningStrike), nameof(WeatherEvent_LightningStrike.DoStrike))]
    public static class Patch_LightningStrike
    {
        public static void Prefix(ref IntVec3 strikeLoc, Map map)
        {
            if (map == null || StormproofRegistry.Spires.Count == 0)
            {
                return;
            }
            IntVec3 loc = strikeLoc;
            // Shouldn't normally happen (callers pick a cell first), but keep
            // the vanilla fallback so an invalid loc can still be redirected.
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
            strikeLoc = catcher != null ? catcher.parent.Position : loc;
        }

        public static void Postfix(IntVec3 strikeLoc, Map map)
        {
            if (map == null)
            {
                return;
            }
            CompStormSpire spire = StormproofRegistry
                .On(StormproofRegistry.Spires, map)
                .FirstOrDefault(s => s.parent.Position == strikeLoc);
            spire?.Notify_Struck();
        }
    }
}

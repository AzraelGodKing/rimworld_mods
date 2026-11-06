using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    // Storm spires attract lightning that would land within their radius.
    //
    // WeatherEvent_LightningStrike and WeatherEvent_LightningStrikeDelayed
    // (thunderstorms use the delayed one; they are siblings) both call the
    // static DoStrike(strikeLoc, map, ref boltMesh), but each draws its bolt
    // from its own private strikeLoc field. Redirecting only the DoStrike
    // argument moved the damage to the spire and left the bolt drawn at the
    // original cell, so the events' own field is redirected first.
    public static class LightningRedirect
    {
        // Set while an event's strike has already been redirected and counted,
        // so the DoStrike prefix doesn't count it a second time.
        internal static bool handledByEvent;

        // Returns the cell the bolt should land on, and records caught/missed.
        internal static IntVec3 Redirect(IntVec3 strikeLoc, Map map)
        {
            IntVec3 loc = strikeLoc;
            // Vanilla's own fallback for an unset cell, picked here so the
            // event's drawn bolt and the damage agree.
            if (!loc.IsValid)
            {
                loc = CellFinderLoose.RandomCellWith(
                    sq => sq.Standable(map) && !map.roofGrid.Roofed(sq), map);
            }
            CompStormSpire catcher = Catcher(loc, map);
            MapComponent_Stormproof.For(map)?.NoteStrike(caught: catcher != null);
            return catcher != null ? catcher.parent.Position : loc;
        }

        // Nearest attracting spire in range; ties go to the lowest thing id.
        private static CompStormSpire Catcher(IntVec3 loc, Map map)
        {
            List<CompStormSpire> spires = StormproofRegistry.Spires.On(map);
            CompStormSpire best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < spires.Count; i++)
            {
                CompStormSpire spire = spires[i];
                if (!spire.Attracting)
                {
                    continue;
                }
                float radius = spire.Props.attractRadius;
                float distSq = (spire.parent.Position - loc).LengthHorizontalSquared;
                if (distSq <= radius * radius && distSq < bestDistSq)
                {
                    best = spire;
                    bestDistSq = distSq;
                }
            }
            return best;
        }
    }

    [HarmonyPatch(typeof(WeatherEvent_LightningStrike), nameof(WeatherEvent_LightningStrike.FireEvent))]
    public static class Patch_LightningStrike_FireEvent
    {
        public static void Prefix(ref IntVec3 ___strikeLoc, Map ___map)
        {
            if (___map == null)
            {
                return;
            }
            ___strikeLoc = LightningRedirect.Redirect(___strikeLoc, ___map);
            LightningRedirect.handledByEvent = true;
        }

        public static void Finalizer()
        {
            LightningRedirect.handledByEvent = false;
        }
    }

    [HarmonyPatch(typeof(WeatherEvent_LightningStrikeDelayed), nameof(WeatherEvent_LightningStrikeDelayed.WeatherEventTick))]
    public static class Patch_LightningStrikeDelayed_Tick
    {
        // WeatherEventTick does age++ and strikes when age == delay.
        public static void Prefix(ref IntVec3 ___strikeLoc, Map ___map, int ___age, int ___delay)
        {
            if (___map == null || ___age + 1 != ___delay)
            {
                return;
            }
            ___strikeLoc = LightningRedirect.Redirect(___strikeLoc, ___map);
            LightningRedirect.handledByEvent = true;
        }

        public static void Finalizer()
        {
            LightningRedirect.handledByEvent = false;
        }
    }

    // Any other caller of DoStrike (other mods, other events) still gets the
    // redirect, then the spire that took the bolt reacts.
    [HarmonyPatch(typeof(WeatherEvent_LightningStrike), nameof(WeatherEvent_LightningStrike.DoStrike))]
    public static class Patch_LightningStrike
    {
        public static void Prefix(ref IntVec3 strikeLoc, Map map)
        {
            if (map == null)
            {
                return;
            }
            if (LightningRedirect.handledByEvent)
            {
                LightningRedirect.handledByEvent = false;
                return;
            }
            strikeLoc = LightningRedirect.Redirect(strikeLoc, map);
        }

        public static void Postfix(IntVec3 strikeLoc, Map map)
        {
            List<CompStormSpire> spires = StormproofRegistry.Spires.On(map);
            for (int i = 0; i < spires.Count; i++)
            {
                if (spires[i].parent.Position == strikeLoc)
                {
                    spires[i].StartFireSuppression();
                    spires[i].Notify_Struck();
                    return;
                }
            }
        }
    }
}

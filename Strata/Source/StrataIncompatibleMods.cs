using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // AASB / MultiFloors stack maps the same way Strata does. Pathfinding
    // Avoidance rewrites path costs and clashes with Strata's cross-floor
    // pathing. About.xml incompatibleWith warns in the mod list; this catches
    // players who click past.
    public static class StrataIncompatibleMods
    {
        public const string Aasb = "astryl.AsAboveSoBelow";
        public const string Aasb2 = "astryl.AsAboveSoBelow2";
        public const string MultiFloors = "telardo.MultiFloors";
        public const string MultiFloorsDev = "telardo.MultiFloorsDev";
        public const string PathfindingAvoidance = "llunak.PathfindingAvoidance";

        private static bool letterSent;

        public static List<string> ActiveConflicts()
        {
            var names = new List<string>();
            TryAdd(Aasb, "As Above, So Below", names);
            TryAdd(Aasb2, "As above, So below 2", names);
            TryAdd(MultiFloors, "MultiFloors", names);
            TryAdd(MultiFloorsDev, "MultiFloors (dev)", names);
            return names;
        }

        public static List<string> ActiveOtherConflicts()
        {
            var names = new List<string>();
            TryAdd(PathfindingAvoidance, "Pathfinding Avoidance", names);
            return names;
        }

        public static void LogIfNeeded()
        {
            List<string> conflicts = ActiveConflicts();
            if (conflicts.Count > 0)
            {
                Log.Error("[Strata] Incompatible multi-level mod(s) loaded: "
                    + string.Join(", ", conflicts)
                    + ". Do not run these with Strata — stacked maps will fight.");
            }
            List<string> others = ActiveOtherConflicts();
            if (others.Count > 0)
            {
                Log.Error("[Strata] Incompatible mod(s) loaded: "
                    + string.Join(", ", others)
                    + ". Do not run these with Strata.");
            }
        }

        public static void LetterIfNeeded()
        {
            if (letterSent || Find.LetterStack == null)
            {
                return;
            }
            List<string> conflicts = ActiveConflicts();
            List<string> others = ActiveOtherConflicts();
            if (conflicts.Count == 0 && others.Count == 0)
            {
                return;
            }
            letterSent = true;
            if (conflicts.Count > 0)
            {
                Find.LetterStack.ReceiveLetter(
                    "Strata_IncompatibleStackLetterLabel".Translate(),
                    "Strata_IncompatibleStackLetterText".Translate(string.Join(", ", conflicts)),
                    LetterDefOf.NeutralEvent);
            }
            if (others.Count > 0)
            {
                Find.LetterStack.ReceiveLetter(
                    "Strata_IncompatibleModLetterLabel".Translate(),
                    "Strata_IncompatibleModLetterText".Translate(string.Join(", ", others)),
                    LetterDefOf.NeutralEvent);
            }
        }

        private static void TryAdd(string packageId, string display, List<string> names)
        {
            if (ModsConfig.IsActive(packageId)
                || ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true) != null)
            {
                names.Add(display);
            }
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // AASB / MultiFloors stack maps the same way Strata does. About.xml
    // incompatibleWith warns in the mod list; this catches players who click past.
    public static class StrataIncompatibleMods
    {
        public const string Aasb = "astryl.AsAboveSoBelow";
        public const string Aasb2 = "astryl.AsAboveSoBelow2";
        public const string MultiFloors = "telardo.MultiFloors";
        public const string MultiFloorsDev = "telardo.MultiFloorsDev";

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

        public static void LogIfNeeded()
        {
            List<string> conflicts = ActiveConflicts();
            if (conflicts.Count == 0)
            {
                return;
            }
            Log.Error("[Strata] Incompatible multi-level mod(s) loaded: "
                + string.Join(", ", conflicts)
                + ". Do not run these with Strata — stacked maps will fight.");
        }

        public static void LetterIfNeeded()
        {
            if (letterSent || Find.LetterStack == null)
            {
                return;
            }
            List<string> conflicts = ActiveConflicts();
            if (conflicts.Count == 0)
            {
                return;
            }
            letterSent = true;
            Find.LetterStack.ReceiveLetter(
                "Strata_IncompatibleStackLetterLabel".Translate(),
                "Strata_IncompatibleStackLetterText".Translate(string.Join(", ", conflicts)),
                LetterDefOf.NeutralEvent);
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

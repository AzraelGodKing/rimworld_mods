using System.Collections.Generic;
using System.Text;
using Verse;

namespace Strata
{
    // Tag a named allowlist of vanilla / DLC combustion buildings. Other mods
    // are not opted in by heuristic — add them in Patches/Exhaust_Strata.xml.
    public static class ExhaustAutoPatch
    {
        // emissionPerCycle matches CompExhaust work-table / flame comments.
        private static readonly Dictionary<string, float> AllowlistedEmissions =
            new Dictionary<string, float>
            {
                { "FueledStove", 1f },
                { "FueledSmithy", 1f },
                { "Brazier", 2f },
                { "DarklightBrazier", 2f },
                { "Darktorch", 0.1f },
                { "DarktorchFloodlight", 0.1f },
            };

        public static void Apply()
        {
            StrataSettings settings = StrataMod.Settings;
            if (settings != null && !settings.autoTagExhaust)
            {
                if (settings.verboseLogging)
                {
                    StrataLog.Verbose("[Strata] Exhaust auto-tag skipped (setting off).");
                }
                return;
            }

            var tagged = new List<string>();
            foreach (KeyValuePair<string, float> kv in AllowlistedEmissions)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(kv.Key);
                if (def == null || HasExhaustComp(def))
                {
                    continue;
                }
                def.comps ??= new List<CompProperties>();
                def.comps.Add(new CompProperties_Exhaust { emissionPerCycle = kv.Value });
                tagged.Add(def.defName);
            }

            if (settings != null && settings.verboseLogging)
            {
                var sb = new StringBuilder();
                sb.Append("[Strata] Exhaust auto-tag allowlist: ");
                if (tagged.Count == 0)
                {
                    sb.Append("nothing new (XML already covered, or DLC defs missing).");
                }
                else
                {
                    sb.Append(string.Join(", ", tagged));
                }
                StrataLog.Verbose(sb.ToString());
            }
        }

        private static bool HasExhaustComp(ThingDef def)
        {
            if (def.comps == null)
            {
                return false;
            }
            for (int i = 0; i < def.comps.Count; i++)
            {
                if (typeof(CompExhaust).IsAssignableFrom(def.comps[i].compClass))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

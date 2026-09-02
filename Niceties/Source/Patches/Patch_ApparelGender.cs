using System.Collections.Generic;
using Verse;

namespace Niceties
{
    internal static class ApparelGender
    {
        private static readonly Dictionary<ThingDef, Gender> Original = new Dictionary<ThingDef, Gender>();

        internal static void Capture()
        {
            Original.Clear();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.IsApparel && def.apparel != null && def.apparel.gender != Gender.None)
                {
                    Original[def] = def.apparel.gender;
                }
            }
        }

        internal static void Apply(bool wearAny)
        {
            if (Original.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<ThingDef, Gender> kv in Original)
            {
                if (kv.Key?.apparel == null)
                {
                    continue;
                }

                kv.Key.apparel.gender = wearAny ? Gender.None : kv.Value;
            }
        }
    }
}

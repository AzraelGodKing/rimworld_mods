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

        /// <summary>
        /// Def-level gender tags are shared by every client, so they follow the
        /// host-baked snapshot in a loaded game and Mod Options only at the main menu.
        /// </summary>
        internal static void ApplyEffective()
        {
            NicetiesSettings settings = NicetiesSim.Settings;
            Apply(settings == null || settings.wearAnyGender);
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

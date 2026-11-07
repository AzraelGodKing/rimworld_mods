using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Lookups over every PerkDef, built once on first use (AZR-621). Perk sync used to
    /// rescan all perks for each perk's exclusivity, which is quadratic in the perk count.
    /// </summary>
    internal static class PerkIndex
    {
        private static readonly List<PerkDef> Empty = new List<PerkDef>();
        private static Dictionary<string, List<PerkDef>> exclusiveBy;
        private static List<PerkDef> autoGrantable;

        /// <summary>Perks whose exclusiveWith lists this perk.</summary>
        internal static List<PerkDef> ExcludedBy(PerkDef perk)
        {
            EnsureBuilt();
            return exclusiveBy.TryGetValue(perk.defName, out List<PerkDef> list) ? list : Empty;
        }

        /// <summary>Non-alternate perks in ascending required level: the sync grant order.</summary>
        internal static List<PerkDef> AutoGrantable
        {
            get
            {
                EnsureBuilt();
                return autoGrantable;
            }
        }

        private static void EnsureBuilt()
        {
            if (exclusiveBy != null) return;
            var map = new Dictionary<string, List<PerkDef>>();
            var grant = new List<PerkDef>();
            foreach (PerkDef perk in DefDatabase<PerkDef>.AllDefsListForReading)
            {
                if (perk == null) continue;
                if (!perk.alternateBranch) grant.Add(perk);
                if (perk.exclusiveWith == null) continue;
                for (int i = 0; i < perk.exclusiveWith.Count; i++)
                {
                    string other = perk.exclusiveWith[i];
                    if (!map.TryGetValue(other, out List<PerkDef> list))
                        map[other] = list = new List<PerkDef>();
                    if (!list.Contains(perk)) list.Add(perk);
                }
            }
            // OrderBy is stable: equal levels keep def order.
            autoGrantable = grant.OrderBy(p => p.requiredLevel).ToList();
            exclusiveBy = map;
        }
    }
}

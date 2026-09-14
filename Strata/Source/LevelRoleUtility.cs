using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    public enum LevelRole
    {
        None,
        Freezer,
        Barracks,
        Workshop,
        Hospital,
        Storage,
        Farm,
        Quarry,
    }

    // Player-assigned roles for colony levels. Soft bias only — relays and
    // hauling prefer matching levels but never block vanilla behavior.
    public class WorldComponent_StrataLevelRoles : WorldComponent
    {
        public Dictionary<int, LevelRole> roles = new Dictionary<int, LevelRole>();

        public WorldComponent_StrataLevelRoles(World world) : base(world)
        {
        }

        public static WorldComponent_StrataLevelRoles Get => Find.World?.GetComponent<WorldComponent_StrataLevelRoles>();

        public LevelRole GetRole(Map map)
        {
            if (map == null || !roles.TryGetValue(map.uniqueID, out LevelRole role))
            {
                return LevelRole.None;
            }
            return role;
        }

        public void SetRole(Map map, LevelRole role)
        {
            if (map == null)
            {
                return;
            }
            if (role == LevelRole.None)
            {
                roles.Remove(map.uniqueID);
            }
            else
            {
                roles[map.uniqueID] = role;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref roles, "roles", LookMode.Value, LookMode.Value);
            roles ??= new Dictionary<int, LevelRole>();
        }
    }

    public static class LevelRoleUtility
    {
        // Display / cycle order (AZR-140). Enum values append Farm/Quarry so
        // existing saves that stored Storage as 5 stay Storage.
        private static readonly LevelRole[] AllRoles =
        {
            LevelRole.None,
            LevelRole.Farm,
            LevelRole.Freezer,
            LevelRole.Workshop,
            LevelRole.Barracks,
            LevelRole.Quarry,
            LevelRole.Storage,
            LevelRole.Hospital,
        };

        public static IEnumerable<LevelRole> AllRolesInOrder()
        {
            for (int i = 0; i < AllRoles.Length; i++)
            {
                yield return AllRoles[i];
            }
        }

        public static string Label(LevelRole role)
        {
            switch (role)
            {
                case LevelRole.Farm: return "Strata_Role_Farm".Translate();
                case LevelRole.Freezer: return "Strata_Role_Freezer".Translate();
                case LevelRole.Barracks: return "Strata_Role_Barracks".Translate();
                case LevelRole.Workshop: return "Strata_Role_Workshop".Translate();
                case LevelRole.Hospital: return "Strata_Role_Hospital".Translate();
                case LevelRole.Storage: return "Strata_Role_Storage".Translate();
                case LevelRole.Quarry: return "Strata_Role_Quarry".Translate();
                default: return "Strata_Role_None".Translate();
            }
        }

        public static int RoleMatchScore(Map map, LevelRole preferred)
        {
            if (preferred == LevelRole.None || map == null)
            {
                return 0;
            }
            return GetRole(map) == preferred ? 1 : 0;
        }

        public static LevelRole GetRole(Map map) => WorldComponent_StrataLevelRoles.Get?.GetRole(map) ?? LevelRole.None;

        public static void SetRole(Map map, LevelRole role) => WorldComponent_StrataLevelRoles.Get?.SetRole(map, role);

        public static void SortLinksByRole(List<LevelGraph.LevelLink> links, LevelRole preferred)
        {
            if (preferred == LevelRole.None)
            {
                return;
            }
            SortLinksByScore(links, map => RoleMatchScore(map, preferred));
        }

        public static void SortLinksByRoles(List<LevelGraph.LevelLink> links, params LevelRole[] ranked)
        {
            if (ranked == null || ranked.Length == 0)
            {
                return;
            }
            SortLinksByScore(links, map => RoleRankScore(map, ranked));
        }

        public static void SortLinksForWork(List<LevelGraph.LevelLink> links, Pawn pawn)
        {
            SortLinksByRole(links, PreferredWorkRole(pawn));
        }

        // Matching role first; when nothing matches, leave BFS order alone
        // (List.Sort is unstable — a no-op compare would shuffle untagged floors).
        public static void SortLinksByScore(List<LevelGraph.LevelLink> links, Func<Map, int> score)
        {
            if (links == null || links.Count < 2 || score == null)
            {
                return;
            }

            int n = links.Count;
            int[] scores = new int[n];
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                scores[i] = score(links[i].map);
                if (scores[i] > 0)
                {
                    any = true;
                }
            }
            if (!any)
            {
                return;
            }

            int[] order = new int[n];
            for (int i = 0; i < n; i++)
            {
                order[i] = i;
            }
            Array.Sort(order, (a, b) =>
            {
                int cmp = scores[b].CompareTo(scores[a]);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            LevelGraph.LevelLink[] copy = new LevelGraph.LevelLink[n];
            for (int i = 0; i < n; i++)
            {
                copy[i] = links[order[i]];
            }
            for (int i = 0; i < n; i++)
            {
                links[i] = copy[i];
            }
        }

        public static int HaulMatchScore(Thing thing, Map map)
        {
            LevelRole role = GetRole(map);
            if (role == LevelRole.None || thing?.def == null)
            {
                return 0;
            }

            if (IsFoodish(thing.def))
            {
                if (role == LevelRole.Freezer)
                {
                    return 2;
                }
                if (role == LevelRole.Farm)
                {
                    return 1;
                }
                return 0;
            }

            if (IsOreish(thing.def))
            {
                if (role == LevelRole.Quarry)
                {
                    return 2;
                }
                if (role == LevelRole.Storage)
                {
                    return 1;
                }
                return 0;
            }

            return role == LevelRole.Storage ? 1 : 0;
        }

        public static string AppendInspect(string text, Map dest)
        {
            string line = InspectLine(dest);
            if (line.NullOrEmpty())
            {
                return text;
            }
            return text.NullOrEmpty() ? line : text + "\n" + line;
        }

        public static string InspectLine(Map map)
        {
            LevelRole role = GetRole(map);
            if (role == LevelRole.None)
            {
                return null;
            }
            return "Strata_StairwellPurpose".Translate(Label(role));
        }

        private static int RoleRankScore(Map map, LevelRole[] ranked)
        {
            LevelRole have = GetRole(map);
            if (have == LevelRole.None)
            {
                return 0;
            }
            for (int i = 0; i < ranked.Length; i++)
            {
                if (ranked[i] == have)
                {
                    return ranked.Length - i;
                }
            }
            return 0;
        }

        private static LevelRole PreferredWorkRole(Pawn pawn)
        {
            if (pawn?.workSettings == null || !pawn.workSettings.EverWork)
            {
                return LevelRole.Workshop;
            }

            int bestPri = 100;
            LevelRole best = LevelRole.None;
            Consider(pawn, StrataDefOf.Mining, LevelRole.Quarry, ref bestPri, ref best);
            Consider(pawn, WorkTypeDefOf.Growing, LevelRole.Farm, ref bestPri, ref best);
            Consider(pawn, StrataDefOf.PlantCutting, LevelRole.Farm, ref bestPri, ref best);
            Consider(pawn, StrataDefOf.Cooking, LevelRole.Freezer, ref bestPri, ref best);
            Consider(pawn, StrataDefOf.Crafting, LevelRole.Workshop, ref bestPri, ref best);
            Consider(pawn, WorkTypeDefOf.Construction, LevelRole.Workshop, ref bestPri, ref best);
            Consider(pawn, WorkTypeDefOf.Doctor, LevelRole.Hospital, ref bestPri, ref best);
            return best == LevelRole.None ? LevelRole.Workshop : best;
        }

        private static void Consider(
            Pawn pawn,
            WorkTypeDef work,
            LevelRole role,
            ref int bestPri,
            ref LevelRole best)
        {
            if (work == null)
            {
                return;
            }
            int pri = pawn.workSettings.GetPriority(work);
            if (pri <= 0 || pri >= bestPri)
            {
                return;
            }
            bestPri = pri;
            best = role;
        }

        private static bool IsFoodish(ThingDef def)
        {
            return def.IsNutritionGivingIngestible || def.IsIngestible;
        }

        private static bool IsOreish(ThingDef def)
        {
            if (def.mineable || def.deepCommonality > 0f)
            {
                return true;
            }
            if (def.stuffProps?.categories != null)
            {
                for (int i = 0; i < def.stuffProps.categories.Count; i++)
                {
                    StuffCategoryDef cat = def.stuffProps.categories[i];
                    if (cat == StuffCategoryDefOf.Metallic || (cat != null && cat.defName == "Stony"))
                    {
                        return true;
                    }
                }
            }
            if (def.thingCategories == null)
            {
                return false;
            }
            for (int i = 0; i < def.thingCategories.Count; i++)
            {
                ThingCategoryDef cat = def.thingCategories[i];
                if (cat == ThingCategoryDefOf.StoneBlocks)
                {
                    return true;
                }
                if (cat != null && (cat.defName == "StoneChunks" || cat.defName == "Chunks"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

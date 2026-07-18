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
        private static readonly LevelRole[] AllRoles =
        {
            LevelRole.None,
            LevelRole.Freezer,
            LevelRole.Barracks,
            LevelRole.Workshop,
            LevelRole.Hospital,
            LevelRole.Storage,
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
                case LevelRole.Freezer: return "Strata_Role_Freezer".Translate();
                case LevelRole.Barracks: return "Strata_Role_Barracks".Translate();
                case LevelRole.Workshop: return "Strata_Role_Workshop".Translate();
                case LevelRole.Hospital: return "Strata_Role_Hospital".Translate();
                case LevelRole.Storage: return "Strata_Role_Storage".Translate();
                default: return "Strata_Role_None".Translate();
            }
        }

        // Mild preference: matching role sorts before others, ties keep BFS order.
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
            if (preferred == LevelRole.None || links == null || links.Count < 2)
            {
                return;
            }
            links.Sort((a, b) => RoleMatchScore(b.map, preferred).CompareTo(RoleMatchScore(a.map, preferred)));
        }
    }
}

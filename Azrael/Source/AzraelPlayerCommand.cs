#nullable enable
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Azrael
{
    /// <summary>
    /// The one map change Azrael makes from Mod Options: deconstruct designations for
    /// safe removal. Applied at once (works while paused, nothing to lose on save), and
    /// registered as a Multiplayer sync method so it runs on every client.
    /// </summary>
    internal static class AzraelPlayerCommand
    {
        // Multiplayer sync target (AzraelMultiplayer): keep the signature MP-serialisable.
        public static void DesignateDeconstruct(int[] thingIds, string display)
        {
            if (thingIds == null || thingIds.Length == 0 || Current.Game == null)
            {
                return;
            }
            int n = SeriesRemoval.ApplyDeconstruct(FindBuildings(new HashSet<int>(thingIds)));
            Messages.Message("Azrael_Removal_Designated".Translate(n, display ?? ""),
                MessageTypeDefOf.TaskCompletion, historical: false);
        }

        private static List<Thing> FindBuildings(HashSet<int> ids)
        {
            var found = new List<Thing>();
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Thing>? buildings = maps[m]?.listerThings?.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
                if (buildings == null)
                {
                    continue;
                }
                for (int i = 0; i < buildings.Count; i++)
                {
                    Thing t = buildings[i];
                    if (t != null && ids.Contains(t.thingIDNumber))
                    {
                        found.Add(t);
                    }
                }
            }
            return found;
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Scan linked floors from the map the player is watching. GenHostility's
    // "active threat" filter often misses debug-spawned / idle hostiles; we use
    // a direct HostileTo scan and fire a threat letter + camera jump.
    public class MapComponent_CrossLevelThreatWatch : MapComponent
    {
        private readonly HashSet<int> knownHostileIds = new HashSet<int>();

        public MapComponent_CrossLevelThreatWatch(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            // Only the viewed map scans — avoids N duplicate letters from every
            // linked pocket ticking the same wave.
            if (Find.CurrentMap != map || !map.IsHashIntervalTick(30))
            {
                return;
            }
            if (!map.IsPlayerHome
                && !StrataCrossLevelThreatNotify.IsLinkedFloor(map))
            {
                return;
            }
            if (!LevelGraph.AnyLinkFrom(map))
            {
                return;
            }

            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(map);
            for (int i = 0; i < links.Count; i++)
            {
                Map other = links[i].map;
                if (!StrataCrossLevelThreatNotify.IsLinkedFloor(other)
                    || !StrataCrossLevelThreatNotify.ShouldForceAttention(other))
                {
                    continue;
                }
                Pawn firstNew = FirstNewHostile(other);
                if (firstNew == null)
                {
                    continue;
                }
                StrataCrossLevelThreatNotify.NotifyHostilesOnLinkedFloor(
                    other,
                    new LookTargets(firstNew),
                    "Strata_CrossLevelThreat_Label".Translate(),
                    "Strata_CrossLevelThreat_Text".Translate(FloorLabel(other)));
            }

            // Drop IDs for pawns that left / died so a later wave can re-alert.
            PruneKnown(links);
        }

        private Pawn FirstNewHostile(Map other)
        {
            Pawn first = null;
            foreach (Pawn pawn in other.mapPawns.AllPawnsSpawned)
            {
                if (!IsReportableHostile(pawn))
                {
                    continue;
                }
                if (knownHostileIds.Add(pawn.thingIDNumber) && first == null)
                {
                    first = pawn;
                }
            }
            return first;
        }

        private void PruneKnown(List<LevelGraph.LevelLink> links)
        {
            if (knownHostileIds.Count == 0)
            {
                return;
            }
            knownHostileIds.RemoveWhere(id =>
            {
                for (int i = 0; i < links.Count; i++)
                {
                    Map other = links[i].map;
                    if (!StrataCrossLevelThreatNotify.IsLinkedFloor(other))
                    {
                        continue;
                    }
                    foreach (Pawn pawn in other.mapPawns.AllPawnsSpawned)
                    {
                        if (pawn.thingIDNumber == id && IsReportableHostile(pawn))
                        {
                            return false;
                        }
                    }
                }
                return true;
            });
        }

        private static bool IsReportableHostile(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !pawn.Downed
                && pawn.HostileTo(Faction.OfPlayer)
                && pawn.Spawned;
        }

        private static string FloorLabel(Map map)
        {
            int alt = StrataDepth.Altitude(map);
            if (alt < 0)
            {
                return "B" + (-alt);
            }
            if (alt > 0)
            {
                return "A" + alt;
            }
            return map?.Parent?.LabelCap ?? "linked floor";
        }
    }
}

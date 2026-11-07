#nullable enable
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// One thingIDNumber → Pawn lookup for every saved id field (heir, counselor,
    /// comfort and count dictionaries, remembrance, pending leaves). Replaces six
    /// scanners that each walked every map and world pawn per lookup (AZR-619).
    /// Built on first use in a tick and reused for the rest of that tick; a miss
    /// rebuilds once more in the same tick, for pawns made after the build.
    /// Covers map pawns, world pawns alive or dead, and pawns in caravans and
    /// travelling transporters. Callers still filter (dead, spawned, faction).
    /// </summary>
    internal static class PawnIdIndex
    {
        private static readonly Dictionary<int, Pawn> byId = new Dictionary<int, Pawn>();
        private static int builtTick = int.MinValue;
        private static int missRebuildTick = int.MinValue;
        private static Game? builtFor;

        internal static Pawn? Find(int id)
        {
            if (id <= 0 || Current.Game == null) return null;
            int now = Verse.Find.TickManager?.TicksGame ?? 0;
            if (builtTick != now || builtFor != Current.Game)
                Build(now);
            if (byId.TryGetValue(id, out Pawn? pawn)) return pawn;
            if (missRebuildTick == now) return null;
            missRebuildTick = now;
            Build(now);
            return byId.TryGetValue(id, out pawn) ? pawn : null;
        }

        internal static void Reset()
        {
            byId.Clear();
            builtTick = int.MinValue;
            missRebuildTick = int.MinValue;
            builtFor = null;
        }

        private static void Build(int now)
        {
            byId.Clear();
            List<Map> maps = Verse.Find.Maps;
            if (maps != null)
            {
                for (int i = 0; i < maps.Count; i++)
                {
                    IReadOnlyList<Pawn>? pawns = maps[i]?.mapPawns?.AllPawns;
                    if (pawns == null) continue;
                    for (int j = 0; j < pawns.Count; j++)
                        Add(pawns[j]);
                }
            }
            if (Verse.Find.WorldPawns != null)
            {
                foreach (Pawn p in Verse.Find.WorldPawns.AllPawnsAliveOrDead)
                    Add(p);
            }
            List<Pawn> travelling = PawnsFinder.AllCaravansAndTravellingTransporters_AliveOrDead;
            if (travelling != null)
            {
                for (int i = 0; i < travelling.Count; i++)
                    Add(travelling[i]);
            }
            builtTick = now;
            builtFor = Current.Game;
        }

        private static void Add(Pawn p)
        {
            if (p != null) byId[p.thingIDNumber] = p;
        }
    }
}

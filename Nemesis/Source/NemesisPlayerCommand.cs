using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// AZR-300 — enqueue informant UI mutations; drain on GameComponentTick
    /// (MP-safer, same pattern as DeepColonyPlayerCommand; no Multiplayer.API).
    /// </summary>
    public static class NemesisPlayerCommand
    {
        private enum Kind : byte
        {
            BuyLead = 0,
            PostBounty = 1,
            ReplyTaunt = 2,
            ReplyTruce = 3,
            ReplySurrender = 4,
        }

        private struct Entry
        {
            public Kind kind;
            public int mapId;
            public int silver;
        }

        private static readonly List<Entry> queue = new List<Entry>();

        public static void EnqueueBuyLead(Map map)
        {
            queue.Add(new Entry { kind = Kind.BuyLead, mapId = map?.uniqueID ?? -1 });
        }

        public static void EnqueuePostBounty(Map map, int silver)
        {
            queue.Add(new Entry
            {
                kind = Kind.PostBounty,
                mapId = map?.uniqueID ?? -1,
                silver = silver,
            });
        }

        public static void EnqueueReplyTaunt()
        {
            queue.Add(new Entry { kind = Kind.ReplyTaunt });
        }

        public static void EnqueueReplyTruce()
        {
            queue.Add(new Entry { kind = Kind.ReplyTruce });
        }

        public static void EnqueueReplySurrender()
        {
            queue.Add(new Entry { kind = Kind.ReplySurrender });
        }

        public static void Drain()
        {
            if (queue.Count == 0) return;
            List<Entry> batch = new List<Entry>(queue);
            queue.Clear();
            for (int i = 0; i < batch.Count; i++)
                Apply(batch[i]);
        }

        private static void Apply(Entry e)
        {
            switch (e.kind)
            {
                case Kind.BuyLead:
                    NemesisInformants.TryBuyLead(FindMap(e.mapId), out _);
                    break;
                case Kind.PostBounty:
                    NemesisInformants.TryPostBounty(FindMap(e.mapId), e.silver, out _);
                    break;
                case Kind.ReplyTaunt:
                    NemesisCommsReplies.DoTauntBack();
                    break;
                case Kind.ReplyTruce:
                    NemesisCommsReplies.DoOfferTruce();
                    break;
                case Kind.ReplySurrender:
                    NemesisCommsReplies.DoDemandSurrender();
                    break;
            }
        }

        private static Map FindMap(int uniqueId)
        {
            if (uniqueId < 0) return Find.CurrentMap ?? Find.AnyPlayerHomeMap;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].uniqueID == uniqueId)
                    return maps[i];
            }
            return Find.CurrentMap ?? Find.AnyPlayerHomeMap;
        }
    }
}

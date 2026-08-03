using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    public class WorldEvent : IExposable
    {
        public int tick;
        public WorldEventKind kind;
        public NewsSeverity severity;
        public int factionAId = -1;
        public int factionBId = -1;
        public int tile = -1;
        public string settlementLabel;
        public string factionAName;
        public string factionBName;
        public bool seenByPlayer;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tick, "tick");
            Scribe_Values.Look(ref kind, "kind");
            Scribe_Values.Look(ref severity, "severity", NewsSeverity.Normal);
            Scribe_Values.Look(ref factionAId, "factionAId", -1);
            Scribe_Values.Look(ref factionBId, "factionBId", -1);
            Scribe_Values.Look(ref tile, "tile", -1);
            Scribe_Values.Look(ref settlementLabel, "settlementLabel");
            Scribe_Values.Look(ref factionAName, "factionAName");
            Scribe_Values.Look(ref factionBName, "factionBName");
            Scribe_Values.Look(ref seenByPlayer, "seenByPlayer");
        }

        public static WorldEvent Create(
            WorldEventKind kind,
            NewsSeverity severity,
            Faction a = null,
            Faction b = null,
            Settlement settlement = null)
        {
            return new WorldEvent
            {
                tick = Find.TickManager.TicksGame,
                kind = kind,
                severity = severity,
                factionAId = a?.loadID ?? -1,
                factionBId = b?.loadID ?? -1,
                factionAName = a?.Name ?? string.Empty,
                factionBName = b?.Name ?? string.Empty,
                tile = settlement?.Tile ?? -1,
                settlementLabel = settlement?.Label ?? string.Empty,
            };
        }

        public Faction FactionA() => FindFaction(factionAId);

        public Faction FactionB() => FindFaction(factionBId);

        private static Faction FindFaction(int id)
        {
            if (id < 0)
            {
                return null;
            }
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].loadID == id)
                {
                    return all[i];
                }
            }
            return null;
        }
    }

    public class SettlementMood : IExposable
    {
        public int settlementId = -1;
        public int tile = -1;
        public int prosperity; // -2..+2
        public int lastMorphTick;
        public string epithet;

        public void ExposeData()
        {
            Scribe_Values.Look(ref settlementId, "settlementId", -1);
            Scribe_Values.Look(ref tile, "tile", -1);
            Scribe_Values.Look(ref prosperity, "prosperity");
            Scribe_Values.Look(ref lastMorphTick, "lastMorphTick");
            Scribe_Values.Look(ref epithet, "epithet");
        }

        public string ProsperityLabelKey()
        {
            if (prosperity >= 2) return "LivingWorld_Prosperity_Thriving";
            if (prosperity == 1) return "LivingWorld_Prosperity_StablePlus";
            if (prosperity <= -2) return "LivingWorld_Prosperity_Collapsing";
            if (prosperity == -1) return "LivingWorld_Prosperity_Struggling";
            return "LivingWorld_Prosperity_Stable";
        }
    }
}

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
        /// <summary>Planet layer of <see cref="tile"/>; -1 (surface) in saves from before layers were stored.</summary>
        public int tileLayer = -1;
        public string settlementLabel;
        public string factionAName;
        public string factionBName;
        public bool seenByPlayer;
        public bool distorted;
        public bool isCorrection;
        public bool corrected;
        public int correctionTick = -1;
        public WorldEventKind trueKind;
        public string trueFactionAName;
        public string trueFactionBName;
        public HearChannel channel = HearChannel.Rumour;

        // Not scribed: IsNearby result for the tick it was computed in (channel and
        // hear rules both ask while publishing).
        internal int nearbyTick = -1;
        internal bool nearbyCached;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tick, "tick");
            Scribe_Values.Look(ref kind, "kind");
            Scribe_Values.Look(ref severity, "severity", NewsSeverity.Normal);
            Scribe_Values.Look(ref factionAId, "factionAId", -1);
            Scribe_Values.Look(ref factionBId, "factionBId", -1);
            Scribe_Values.Look(ref tile, "tile", -1);
            Scribe_Values.Look(ref tileLayer, "tileLayer", -1);
            Scribe_Values.Look(ref settlementLabel, "settlementLabel");
            Scribe_Values.Look(ref factionAName, "factionAName");
            Scribe_Values.Look(ref factionBName, "factionBName");
            Scribe_Values.Look(ref seenByPlayer, "seenByPlayer");
            Scribe_Values.Look(ref distorted, "distorted");
            Scribe_Values.Look(ref isCorrection, "isCorrection");
            Scribe_Values.Look(ref corrected, "corrected");
            Scribe_Values.Look(ref correctionTick, "correctionTick", -1);
            Scribe_Values.Look(ref trueKind, "trueKind");
            Scribe_Values.Look(ref trueFactionAName, "trueFactionAName");
            Scribe_Values.Look(ref trueFactionBName, "trueFactionBName");
            Scribe_Values.Look(ref channel, "channel", HearChannel.Rumour);
        }

        public static WorldEvent Create(
            WorldEventKind kind,
            NewsSeverity severity,
            Faction a = null,
            Faction b = null,
            Settlement settlement = null)
        {
            var ev = new WorldEvent
            {
                tick = Find.TickManager.TicksGame,
                kind = kind,
                severity = severity,
                factionAId = a?.loadID ?? -1,
                factionBId = b?.loadID ?? -1,
                factionAName = a?.Name ?? string.Empty,
                factionBName = b?.Name ?? string.Empty,
                settlementLabel = settlement?.Label ?? string.Empty,
            };
            if (settlement != null)
            {
                ev.Where = settlement.Tile;
            }
            return ev;
        }

        /// <summary>Layer-aware tile; Invalid when the event has no place.</summary>
        public PlanetTile Where
        {
            get => LivingWorldTiles.Make(tile, tileLayer);
            set
            {
                tile = LivingWorldTiles.IdOf(value);
                tileLayer = LivingWorldTiles.LayerOf(value);
            }
        }

        public Faction FactionA() => FindFaction(factionAId);

        public Faction FactionB() => FindFaction(factionBId);

        internal static Faction FindFaction(int id) => LivingWorldWorldStats.FactionById(id);
    }

    public class SettlementMood : IExposable
    {
        public int settlementId = -1;
        public int tile = -1;
        public int tileLayer = -1;
        public int prosperity; // -2..+2
        public int lastMorphTick;
        public string epithet;

        // Cached " (epithet)" suffix and full label; not scribed (rebuilt lazily).
        private string epithetLabelCacheKey;
        private string epithetLabelCache;
        private string fullLabelBase;
        private string fullLabel;

        public void ExposeData()
        {
            Scribe_Values.Look(ref settlementId, "settlementId", -1);
            Scribe_Values.Look(ref tile, "tile", -1);
            Scribe_Values.Look(ref tileLayer, "tileLayer", -1);
            Scribe_Values.Look(ref prosperity, "prosperity");
            Scribe_Values.Look(ref lastMorphTick, "lastMorphTick");
            Scribe_Values.Look(ref epithet, "epithet");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                epithetLabelCacheKey = null;
                epithetLabelCache = null;
                fullLabelBase = null;
                fullLabel = null;
            }
        }

        public string ProsperityLabelKey()
        {
            if (prosperity >= 2) return "LivingWorld_Prosperity_Thriving";
            if (prosperity == 1) return "LivingWorld_Prosperity_StablePlus";
            if (prosperity <= -2) return "LivingWorld_Prosperity_Collapsing";
            if (prosperity == -1) return "LivingWorld_Prosperity_Struggling";
            return "LivingWorld_Prosperity_Stable";
        }

        /// <summary>
        /// Settlement.Label with the epithet appended, cached so the label getter
        /// (called many times per frame on the world map) does not allocate.
        /// </summary>
        public string LabelWithEpithet(string baseLabel)
        {
            string suffix = EpithetLabelSuffix();
            if (suffix == null)
            {
                return baseLabel;
            }
            if (fullLabel == null || baseLabel != fullLabelBase)
            {
                fullLabelBase = baseLabel;
                fullLabel = baseLabel + suffix;
            }
            return fullLabel;
        }

        /// <summary>Cached translated epithet suffix for Settlement.Label.</summary>
        public string EpithetLabelSuffix()
        {
            if (string.IsNullOrEmpty(epithet))
            {
                return null;
            }
            if (epithetLabelCacheKey != epithet || epithetLabelCache == null)
            {
                epithetLabelCacheKey = epithet;
                epithetLabelCache = " (" + epithet.Translate() + ")";
                fullLabel = null;
            }
            return epithetLabelCache;
        }
    }
}

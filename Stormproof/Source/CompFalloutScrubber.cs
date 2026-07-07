using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_FalloutScrubber : CompProperties
    {
        // Severity removed per rare tick (250 ticks). 0.003 is roughly
        // 0.72 severity per day - a bit faster than serious buildup accrues,
        // so sheltering indoors with a scrubber actually recovers pawns.
        public float severityPerRareTick = 0.003f;

        public CompProperties_FalloutScrubber()
        {
            compClass = typeof(CompFalloutScrubber);
        }
    }

    // Filters airborne toxins out of an enclosed room, slowly reducing the
    // toxic buildup of every pawn and animal inside. Useless outdoors - it
    // can't scrub a whole open sky.
    public class CompFalloutScrubber : ThingComp
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;

        public CompProperties_FalloutScrubber Props => (CompProperties_FalloutScrubber)props;

        public bool PoweredOn =>
            parent.Spawned &&
            !parent.Destroyed &&
            (flickComp == null || flickComp.SwitchIsOn) &&
            powerComp != null &&
            powerComp.PowerOn;

        private Room ScrubbableRoom
        {
            get
            {
                Room room = parent.GetRoom();
                return room != null && !room.PsychologicallyOutdoors ? room : null;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            flickComp = parent.GetComp<CompFlickable>();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(250) || !PoweredOn)
            {
                return;
            }
            Room room = ScrubbableRoom;
            if (room == null)
            {
                return;
            }
            foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Dead || pawn.GetRoom() != room)
                {
                    continue;
                }
                Hediff buildup = pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
                if (buildup != null)
                {
                    buildup.Severity -= Props.severityPerRareTick;
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!PoweredOn)
            {
                return "Offline: needs power.";
            }
            return ScrubbableRoom != null
                ? "Scrubbing: toxic buildup of pawns in this room slowly decreases."
                : "Idle: must be placed in an enclosed room to scrub the air.";
        }
    }
}

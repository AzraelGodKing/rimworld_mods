using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Long-lasting trauma memory. Stage advances with age (Raw → Processing → Fading).
    /// Therapy increases age toward DurationTicks so the thought expires sooner.
    /// </summary>
    public class Thought_Trauma : Thought_Memory
    {
        public TraumaDef traumaDef;
        public int rememberedFactionId = -1;

        public override int CurStageIndex
        {
            get
            {
                if (DurationTicks <= 0) return 0;
                float progress = (float)age / DurationTicks;
                if (progress < 0.33f) return 0;
                if (progress < 0.66f) return 1;
                return 2;
            }
        }

        public void RememberFaction(Faction faction)
        {
            if (faction == null || faction.IsPlayer) return;
            rememberedFactionId = faction.loadID;
        }

        public Faction GetRememberedFaction()
        {
            if (rememberedFactionId < 0) return null;
            return Find.FactionManager?.AllFactionsListForReading
                ?.Find(f => f.loadID == rememberedFactionId);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref traumaDef, "traumaDef");
            Scribe_Values.Look(ref rememberedFactionId, "rememberedFactionId", -1);
        }
    }
}

using RimWorld;
using Verse;

namespace WaveSurvivalReforged
{
    // What a wave will be: its type, the faction for raids, threat points, and whether it
    // splits into two half-size raids. Saved, so the announced wave survives a reload.
    public class WavePlan : IExposable
    {
        public WaveTypeDef type;
        public Faction faction;
        public float points;
        public bool split;

        public WavePlan()
        {
        }

        public WavePlan(WaveTypeDef type, Faction faction, float points, bool split)
        {
            this.type = type;
            this.faction = faction;
            this.points = points;
            this.split = split;
        }

        // Points of each raid: half when split.
        public float SinglePoints => split ? points * 0.5f : points;

        // False after a load whose wave type was removed, or a raid whose faction is gone.
        public bool IsUsable => type != null && type.Available && (type.kind == WaveKind.Incident || faction != null);

        public string Describe()
        {
            string label = type.label;
            if (split)
                label = "WSR_WaveSplit".Translate(label);
            return faction == null ? label : (string)"WSR_WaveWithFaction".Translate(label, faction.Name);
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref type, "type");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref points, "points");
            Scribe_Values.Look(ref split, "split");
        }
    }
}

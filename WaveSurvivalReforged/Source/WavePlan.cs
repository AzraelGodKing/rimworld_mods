using RimWorld;

namespace WaveSurvivalReforged
{
    // What a wave will be: its type, the faction for raids, threat points, and whether it splits in two.
    public class WavePlan
    {
        public WaveTypeDef type;
        public Faction faction;
        public float points;
        public bool split;

        public WavePlan(WaveTypeDef type, Faction faction, float points, bool split)
        {
            this.type = type;
            this.faction = faction;
            this.points = points;
            this.split = split;
        }
    }
}

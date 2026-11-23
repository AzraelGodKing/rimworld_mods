using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace LLMStoryteller
{
    // Cassandra's random comps for home maps, switched on only while the model cannot decide
    // (offline, too slow, bad answers twice, or "Use the model" turned off). Everywhere else
    // (caravans, temporary maps, the world) they behave exactly like Cassandra's.

    public class StorytellerCompProperties_FallbackOnOffCycle : StorytellerCompProperties_OnOffCycle
    {
        public StorytellerCompProperties_FallbackOnOffCycle()
        {
            compClass = typeof(StorytellerComp_FallbackOnOffCycle);
        }
    }

    public class StorytellerComp_FallbackOnOffCycle : StorytellerComp_OnOffCycle
    {
        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target) =>
            FallbackGate.Allows(target) ? base.MakeIntervalIncidents(target) : Enumerable.Empty<FiringIncident>();
    }

    public class StorytellerCompProperties_FallbackCategoryMTB : StorytellerCompProperties_CategoryMTB
    {
        public StorytellerCompProperties_FallbackCategoryMTB()
        {
            compClass = typeof(StorytellerComp_FallbackCategoryMTB);
        }
    }

    public class StorytellerComp_FallbackCategoryMTB : StorytellerComp_CategoryMTB
    {
        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target) =>
            FallbackGate.Allows(target) ? base.MakeIntervalIncidents(target) : Enumerable.Empty<FiringIncident>();
    }

    static class FallbackGate
    {
        public static bool Allows(IIncidentTarget target)
        {
            GameComponent_LLMStoryteller state = GameComponent_LLMStoryteller.Get;
            return state == null || state.FallbackActive(target);
        }
    }
}

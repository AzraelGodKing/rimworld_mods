using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>C09 — Grand Chef extra mood only on Homesteader pantry foods. Fail-open.</summary>
    public static class HomesteaderMealUtility
    {
        public static void NotifyIngested(Pawn eater, Thing food)
        {
            if (!DeepColonySettings.Get.enablePerks) return;
            if (!SoftCompat.HomesteaderLoaded) return;
            if (eater?.needs?.mood?.thoughts == null || food == null) return;
            if (!SoftCompat.IsHomesteaderFood(food)) return;
            if (!SoftCompat.HasGrandChef(eater)) return;
            if (DC_DefOf.DC_Thought_GrandChefHomestead == null) return;

            var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_GrandChefHomestead);
            eater.needs.mood.thoughts.memories.TryGainMemory(thought);
        }
    }
}

using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>D01 — eating a meal in the same room as living kin.</summary>
    public static class FamilyMealUtility
    {
        private const int CooldownTicks = 120000; // 2 days

        public static void NotifyIngested(Pawn ingester, Thing food)
        {
            if (!DeepColonySettings.Get.enableInheritance) return;
            if (ingester == null || !ingester.IsColonistPlayerControlled) return;
            if (!ingester.RaceProps.Humanlike) return;
            if (food?.def?.IsNutritionGivingIngestible != true) return;
            if (food.def.IsDrug) return;
            if (ingester.Map == null || !ingester.Spawned) return;

            var comp = ingester.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            int now = Find.TickManager.TicksGame;
            if (comp.lastFamilyMealTick >= 0 && now - comp.lastFamilyMealTick < CooldownTicks)
                return;

            Room room = ingester.GetRoom();
            if (room == null || room.PsychologicallyOutdoors) return;

            Pawn kin = FindKinInRoom(ingester, room);
            if (kin == null) return;

            comp.lastFamilyMealTick = now;
            if (DC_DefOf.DC_Thought_FamilyMeal == null || ingester.needs?.mood?.thoughts == null)
                return;

            var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_FamilyMeal);
            ingester.needs.mood.thoughts.memories.TryGainMemory(thought, kin);
        }

        private static Pawn FindKinInRoom(Pawn pawn, Room room)
        {
            foreach (Pawn other in pawn.Map.mapPawns.FreeColonistsSpawned)
            {
                if (other == pawn || other.Dead) continue;
                if (other.GetRoom() != room) continue;
                if (!MentorshipUtility.IsLineagePair(pawn, other)
                    && !LovePartnerRelationUtility.LovePartnerRelationExists(pawn, other))
                    continue;
                return other;
            }
            return null;
        }
    }
}

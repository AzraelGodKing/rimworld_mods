using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>C07 — child witnesses get a lighter, shorter thought (not full combat shock).</summary>
    public static class ChildRaidUtility
    {
        public static void NotifyDowned(Pawn victim, DamageInfo info)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (DeepColonySettings.Get.childRaidWitnessChance <= 0f) return;
            if (victim == null || victim.MapHeld == null) return;

            Thing instigator = info.Instigator;
            if (instigator == null || !instigator.HostileTo(victim)) return;
            if (victim.Faction != Faction.OfPlayer && !victim.IsColonist) return;

            Map map = victim.MapHeld;
            foreach (Pawn child in map.mapPawns.FreeColonistsSpawned)
            {
                if (child == victim || child.Dead) continue;
                if (!IsChildWitness(child)) continue;
                if (child.Downed) continue;
                if (!Rand.Chance(DeepColonySettings.Get.childRaidWitnessChance)) continue;
                if (DC_DefOf.DC_Thought_ChildRaidWitness == null) continue;
                if (child.needs?.mood?.thoughts == null) continue;
                if (child.needs.mood.thoughts.memories.GetFirstMemoryOfDef(DC_DefOf.DC_Thought_ChildRaidWitness) != null)
                    continue;

                var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_ChildRaidWitness);
                child.needs.mood.thoughts.memories.TryGainMemory(thought);
                Messages.Message(
                    "DC_ChildRaidWitness".Translate(child.LabelShort.Named("PAWN")),
                    child, MessageTypeDefOf.NegativeEvent, false);
            }
        }

        public static void NotifyRaidStarted(Map map)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (DeepColonySettings.Get.childRaidWitnessChance <= 0f) return;
            if (map == null) return;

            foreach (Pawn child in map.mapPawns.FreeColonistsSpawned)
            {
                if (!IsChildWitness(child) || child.Downed) continue;
                if (!Rand.Chance(DeepColonySettings.Get.childRaidWitnessChance * 0.5f)) continue;
                if (DC_DefOf.DC_Thought_ChildRaidWitness == null) continue;
                if (child.needs?.mood?.thoughts == null) continue;
                if (child.needs.mood.thoughts.memories.GetFirstMemoryOfDef(DC_DefOf.DC_Thought_ChildRaidWitness) != null)
                    continue;

                var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_ChildRaidWitness);
                child.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
        }

        private static bool IsChildWitness(Pawn pawn)
        {
            if (pawn == null || !pawn.RaceProps.Humanlike) return false;
            if (!pawn.IsColonistPlayerControlled) return false;
            if (ModsConfig.BiotechActive)
                return pawn.DevelopmentalStage < DevelopmentalStage.Adult;
            return pawn.ageTracker != null && pawn.ageTracker.AgeBiologicalYears < 13;
        }
    }
}

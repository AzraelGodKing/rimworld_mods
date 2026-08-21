using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>D02 — adult child returning to a map where a parent still lives.</summary>
    public static class ParentReunionUtility
    {
        public static void NotifySpawned(Pawn pawn)
        {
            if (!DeepColonySettings.Get.enableInheritance) return;
            if (pawn == null || !pawn.IsColonistPlayerControlled) return;
            if (!pawn.RaceProps.Humanlike || pawn.Dead) return;
            if (pawn.Map == null) return;
            if (pawn.DevelopmentalStage < DevelopmentalStage.Adult) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null || comp.parentReunionGranted) return;

            Pawn parent = FindLivingParentOnMap(pawn);
            if (parent == null) return;

            comp.parentReunionGranted = true;
            if (DC_DefOf.DC_Thought_ParentReunion == null || pawn.needs?.mood?.thoughts == null)
                return;

            var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_ParentReunion);
            pawn.needs.mood.thoughts.memories.TryGainMemory(thought, parent);
            Messages.Message(
                "DC_ParentReunion".Translate(
                    pawn.LabelShort.Named("CHILD"),
                    parent.LabelShort.Named("PARENT")),
                new LookTargets(pawn, parent),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private static Pawn FindLivingParentOnMap(Pawn pawn)
        {
            if (pawn.relations == null || pawn.Map == null) return null;
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def != PawnRelationDefOf.Parent) continue;
                Pawn parent = rel.otherPawn;
                if (parent == null || parent.Dead) continue;
                if (parent.MapHeld != pawn.Map) continue;
                if (!parent.IsColonistPlayerControlled) continue;
                return parent;
            }
            return null;
        }
    }
}
